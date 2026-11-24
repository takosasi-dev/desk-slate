// probe の画面。起動時に端末の情報を出し、EAN-13 を 10 回・QR を 3 回読む計測と振動の確認をする。
// adb の `--es action <info|vibrate|ean|qr>` でも動かせる。出す行はすべて Log.i("DeskSlateProbe") にも出す。
package io.github.takosasidev.deskslate.probe

import android.Manifest
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.SystemClock
import android.os.Vibrator
import android.util.Log
import android.util.Size
import android.view.View
import android.view.ViewGroup.LayoutParams.MATCH_PARENT
import android.view.ViewGroup.LayoutParams.WRAP_CONTENT
import android.widget.Button
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import androidx.activity.ComponentActivity
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.core.resolutionselector.ResolutionSelector
import androidx.camera.core.resolutionselector.ResolutionStrategy
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import com.google.zxing.BarcodeFormat
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors

class MainActivity : ComponentActivity() {
    companion object {
        private const val TAG = "DeskSlateProbe"
        private const val TIMEOUT_MS = 10_000L
        private const val VIBRATE_MS = 200L
        private const val END = "--- 終わり ---"
    }

    /** 1 つの計測の流れ(EAN-13 を 10 回、QR を 3 回)。 */
    private class Session(val label: String, val format: BarcodeFormat, val total: Int) {
        val decoder = BarcodeDecoder(format)
        val times = mutableListOf<Long?>()
    }

    private val main = Handler(Looper.getMainLooper())
    private lateinit var analysisExecutor: ExecutorService
    private lateinit var logView: TextView
    private lateinit var logScroll: ScrollView
    private lateinit var previewView: PreviewView
    private lateinit var readButton: Button

    private var provider: ProcessCameraProvider? = null
    private var session: Session? = null
    private var pendingSession: Session? = null
    private var autoStartFirst = false

    // 解析のスレッドからも読む物
    @Volatile private var measuring = false
    @Volatile private var attemptId = 0
    @Volatile private var attemptStart = 0L
    @Volatile private var activeDecoder: BarcodeDecoder? = null
    @Volatile private var frameInfoLogged = false

    private val timeout = Runnable { finishAttempt(attemptId, null, null) }

    // adb の ean・qr で、カメラから解析の画像が 1 枚も来ないまま黙って止まらないように
    private val noFrame = Runnable {
        if (session != null && !frameInfoLogged) {
            log("カメラ: ${TIMEOUT_MS / 1000} 秒たっても解析の画像が来ない")
            log(END)
            endSession()
        }
    }

    private val cameraPermission = registerForActivityResult(ActivityResultContracts.RequestPermission()) { ok ->
        val s = pendingSession ?: return@registerForActivityResult
        pendingSession = null
        if (ok) {
            startSession(s)
        } else {
            log("カメラの権限がもらえなかった")
            log(END)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        analysisExecutor = Executors.newSingleThreadExecutor()
        buildViews()
        runInfo()
        val action = intent.getStringExtra("action")
        if (savedInstanceState == null && action != null && action != "info") handleAction(action)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        intent.getStringExtra("action")?.let { handleAction(it) }
    }

    override fun onPause() {
        super.onPause()
        if (session != null) {
            log("画面を離れたので読み取りを止めた")
            log(END)
            endSession()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        analysisExecutor.shutdown()
    }

    private fun handleAction(action: String) {
        when (action) {
            "info" -> runInfo()
            "vibrate" -> runVibrate()
            "ean" -> requestSession(eanSession(), autoStart = true)
            "qr" -> requestSession(qrSession(), autoStart = true)
            else -> {
                log("知らない action: $action(info / vibrate / ean / qr)")
                log(END)
            }
        }
    }

    private fun eanSession() = Session("EAN-13", BarcodeFormat.EAN_13, 10)
    private fun qrSession() = Session("QR", BarcodeFormat.QR_CODE, 3)

    // ---- 画面 ----

    private fun buildViews() {
        val root = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL }
        fun button(text: String, onClick: () -> Unit) = Button(this).apply {
            this.text = text
            setOnClickListener { onClick() }
        }
        val row = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
        fun weighted() = LinearLayout.LayoutParams(0, WRAP_CONTENT, 1f)
        row.addView(button("情報") { runInfo() }, weighted())
        row.addView(button("振動") { runVibrate() }, weighted())
        row.addView(button("EAN-13 を 10 回") { requestSession(eanSession(), autoStart = false) }, weighted())
        row.addView(button("QR を 3 回") { requestSession(qrSession(), autoStart = false) }, weighted())
        root.addView(row, LinearLayout.LayoutParams(MATCH_PARENT, WRAP_CONTENT))

        readButton = button("読む") { startAttempt() }.apply { visibility = View.GONE }
        root.addView(readButton, LinearLayout.LayoutParams(MATCH_PARENT, WRAP_CONTENT))

        previewView = PreviewView(this).apply { visibility = View.GONE }
        val previewHeight = (resources.displayMetrics.density * 280).toInt()
        root.addView(previewView, LinearLayout.LayoutParams(MATCH_PARENT, previewHeight))

        logView = TextView(this).apply {
            setTextIsSelectable(true)
            setPadding(16, 16, 16, 16)
        }
        logScroll = ScrollView(this).apply { addView(logView) }
        root.addView(logScroll, LinearLayout.LayoutParams(MATCH_PARENT, 0, 1f))
        setContentView(root)
    }

    /** 画面と logcat に 1 行出す。UI のスレッドから呼ぶ。 */
    private fun log(line: String) {
        Log.i(TAG, line)
        logView.append(line + "\n")
        logScroll.post { logScroll.fullScroll(View.FOCUS_DOWN) }
    }

    // ---- 情報と振動 ----

    private fun runInfo() {
        log("=== 情報 ===")
        DeviceInfo.lines(this).forEach { log(it) }
        log("権限: CAMERA=${granted(Manifest.permission.CAMERA)} VIBRATE=${granted(Manifest.permission.VIBRATE)}")
        log(END)
    }

    private fun granted(permission: String) =
        checkSelfPermission(permission) == PackageManager.PERMISSION_GRANTED

    private fun runVibrate() {
        log("=== 振動 ===")
        log(vibrateOnce())
        log(END)
    }

    /** 200 ms 振動させ、呼んだ結果を 1 行で返す。API 26 の VibrationEffect は使わない(相手は API 24)。 */
    private fun vibrateOnce(): String {
        @Suppress("DEPRECATION") // 代わりの VIBRATOR_MANAGER_SERVICE は API 31 から
        val vibrator = getSystemService(Context.VIBRATOR_SERVICE) as Vibrator?
            ?: return "振動: Vibrator が取れない"
        return try {
            @Suppress("DEPRECATION")
            vibrator.vibrate(VIBRATE_MS)
            "振動: vibrate($VIBRATE_MS) を呼んだ(hasVibrator=${vibrator.hasVibrator()}、例外なし)"
        } catch (e: Exception) {
            "振動: vibrate($VIBRATE_MS) で例外: ${e.javaClass.simpleName}: ${e.message}"
        }
    }

    // ---- 読み取り ----

    private fun requestSession(s: Session, autoStart: Boolean) {
        if (session != null) endSession()
        autoStartFirst = autoStart
        if (granted(Manifest.permission.CAMERA)) {
            startSession(s)
        } else {
            log("カメラの権限を求める")
            pendingSession = s
            cameraPermission.launch(Manifest.permission.CAMERA)
        }
    }

    private fun startSession(s: Session) {
        session = s
        log("=== ${s.label} を ${s.total} 回 ===")
        log("「読む」を押すと時間を測り始める。${TIMEOUT_MS / 1000} 秒で読めなければ失敗")
        previewView.visibility = View.VISIBLE
        updateReadButton()
        startCamera()
    }

    private fun endSession() {
        cancelAttempt()
        main.removeCallbacks(noFrame)
        session = null
        autoStartFirst = false
        readButton.visibility = View.GONE
        previewView.visibility = View.GONE
        provider?.unbindAll()
    }

    private fun updateReadButton() {
        val s = session ?: return
        readButton.text = "読む(${s.times.size + 1}/${s.total})"
        readButton.isEnabled = !measuring
        readButton.visibility = View.VISIBLE
    }

    private fun startCamera() {
        frameInfoLogged = false
        val future = ProcessCameraProvider.getInstance(this)
        future.addListener({
            if (session == null) return@addListener
            try {
                val p = future.get()
                provider = p
                p.unbindAll()
                val preview = Preview.Builder().build()
                preview.setSurfaceProvider(previewView.surfaceProvider)
                // 1280x720 に近い物。無ければ近い大きい物 → 近い小さい物の順
                val resolution = ResolutionSelector.Builder()
                    .setResolutionStrategy(
                        ResolutionStrategy(Size(1280, 720), ResolutionStrategy.FALLBACK_RULE_CLOSEST_HIGHER_THEN_LOWER),
                    )
                    .build()
                val analysis = ImageAnalysis.Builder()
                    .setResolutionSelector(resolution)
                    .setOutputImageFormat(ImageAnalysis.OUTPUT_IMAGE_FORMAT_YUV_420_888)
                    .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                    .build()
                analysis.setAnalyzer(analysisExecutor) { image -> analyze(image) }
                p.bindToLifecycle(this, CameraSelector.DEFAULT_BACK_CAMERA, preview, analysis)
                log("カメラ: 背面カメラにつないだ")
                main.postDelayed(noFrame, TIMEOUT_MS)
            } catch (e: Exception) {
                log("カメラ: 開けない: ${e.javaClass.simpleName}: ${e.message}")
                log(END)
                endSession()
            }
        }, { main.post(it) })
    }

    /** 解析のスレッドで呼ばれる。必ず close する。 */
    private fun analyze(image: ImageProxy) {
        try {
            val plane = image.planes[0]
            if (!frameInfoLogged) {
                frameInfoLogged = true
                val line = "解析の画像: ${image.width}x${image.height} rowStride=${plane.rowStride} " +
                    "pixelStride=${plane.pixelStride} rotationDegrees=${image.imageInfo.rotationDegrees} " +
                    "buffer=${plane.buffer.remaining()}"
                main.post {
                    log(line)
                    if (autoStartFirst) {
                        autoStartFirst = false
                        log("カメラの準備ができたので 1 回目を始める")
                        startAttempt()
                    }
                }
            }
            val id = attemptId
            val start = attemptStart
            val decoder = activeDecoder
            if (!measuring || decoder == null) return
            val y = Luma.packY(plane.buffer, image.width, image.height, plane.rowStride)
            val text = decoder.decode(y, image.width, image.height) ?: return
            val ms = SystemClock.elapsedRealtime() - start
            main.post { finishAttempt(id, text, ms) }
        } catch (e: Exception) {
            val line = "解析で例外: ${e.javaClass.simpleName}: ${e.message}"
            main.post { log(line) }
        } finally {
            image.close()
        }
    }

    private fun startAttempt() {
        val s = session ?: return
        if (measuring || s.times.size >= s.total) return
        attemptStart = SystemClock.elapsedRealtime()
        activeDecoder = s.decoder
        attemptId++
        measuring = true
        updateReadButton()
        main.postDelayed(timeout, TIMEOUT_MS)
        log("${s.label} ${s.times.size + 1}/${s.total}: 計測を始めた")
    }

    private fun cancelAttempt() {
        measuring = false
        activeDecoder = null
        main.removeCallbacks(timeout)
    }

    /** 成功(text と ms あり)か失敗(null)で 1 回を終える。UI のスレッドで、1 回につき 1 度だけ数える。 */
    private fun finishAttempt(id: Int, text: String?, ms: Long?) {
        val s = session ?: return
        if (!measuring || id != attemptId) return
        cancelAttempt()
        val n = s.times.size + 1
        s.times += ms
        if (text == null || ms == null) {
            log("${s.label} $n/${s.total}: 失敗(${TIMEOUT_MS / 1000} 秒で読めない)")
        } else {
            val vibrated = vibrateOnce()
            val shown = if (text.length > 80) text.take(80) + "…(全 ${text.length} 文字)" else text
            log("${s.label} $n/${s.total}: 成功 $ms ms「$shown」")
            log(vibrated)
        }
        if (s.times.size < s.total) {
            updateReadButton()
            return
        }
        val sum = ReadStats.summarize(s.times)
        log("${s.label} の結果: 成功 ${sum.successes}/${sum.total}")
        log("${s.label} の結果: 3 秒以内の成功 ${sum.within3s}/${sum.total}")
        log("${s.label} の結果: 時間の中央値 ${sum.medianMs?.let { "$it ms" } ?: "なし(成功 0 回)"}")
        if (s.format == BarcodeFormat.EAN_13) {
            log("R-3 の基準(3 秒以内に 8 回以上): ${if (sum.meetsR3) "満たす" else "満たさない"}")
        }
        log(END)
        endSession()
    }
}
