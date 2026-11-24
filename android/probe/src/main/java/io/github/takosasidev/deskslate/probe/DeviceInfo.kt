// 起動時に出す端末の情報を行の一覧にする。Android の版・API・機種、Camera2 のカメラの一覧、振動の可否。
// API 24 で使える物だけを呼ぶ。
package io.github.takosasidev.deskslate.probe

import android.content.Context
import android.graphics.ImageFormat
import android.graphics.SurfaceTexture
import android.hardware.camera2.CameraCharacteristics
import android.hardware.camera2.CameraManager
import android.os.Build
import android.os.Vibrator

object DeviceInfo {
    fun lines(context: Context): List<String> {
        val out = mutableListOf(
            "Android: ${Build.VERSION.RELEASE}",
            "API: ${Build.VERSION.SDK_INT}",
            "機種: ${Build.MANUFACTURER} ${Build.MODEL}(device=${Build.DEVICE}、product=${Build.PRODUCT})",
        )
        out += cameraLines(context)
        @Suppress("DEPRECATION") // 代わりの VIBRATOR_MANAGER_SERVICE は API 31 から
        val vibrator = context.getSystemService(Context.VIBRATOR_SERVICE) as Vibrator?
        out += "振動: hasVibrator=${vibrator?.hasVibrator() ?: "Vibrator が取れない"}"
        return out
    }

    private fun cameraLines(context: Context): List<String> {
        val out = mutableListOf<String>()
        val manager = context.getSystemService(Context.CAMERA_SERVICE) as CameraManager
        val ids = try {
            manager.cameraIdList
        } catch (e: Exception) {
            return listOf("カメラの一覧が取れない: ${e.javaClass.simpleName}: ${e.message}")
        }
        out += "カメラの数: ${ids.size}"
        for (id in ids) {
            try {
                val c = manager.getCameraCharacteristics(id)
                out += "カメラ $id: 向き=${facing(c.get(CameraCharacteristics.LENS_FACING))}"
                out += "カメラ $id: センサーの角度=${c.get(CameraCharacteristics.SENSOR_ORIENTATION)}"
                out += "カメラ $id: ライト=${c.get(CameraCharacteristics.FLASH_INFO_AVAILABLE)}"
                out += "カメラ $id: ハードウェアのレベル=${level(c.get(CameraCharacteristics.INFO_SUPPORTED_HARDWARE_LEVEL))}"
                val map = c.get(CameraCharacteristics.SCALER_STREAM_CONFIGURATION_MAP)
                val preview = map?.getOutputSizes(SurfaceTexture::class.java).orEmpty()
                    .sortedByDescending { it.width.toLong() * it.height }.take(8)
                out += "カメラ $id: SurfaceTexture の大きさ(上位 8)=${preview.joinToString(" ")}"
                val yuv = map?.getOutputSizes(ImageFormat.YUV_420_888).orEmpty()
                    .sortedByDescending { it.width.toLong() * it.height }
                out += "カメラ $id: YUV_420_888 の大きさ=${yuv.joinToString(" ")}"
            } catch (e: Exception) {
                out += "カメラ $id: 特性が取れない: ${e.javaClass.simpleName}: ${e.message}"
            }
        }
        return out
    }

    private fun facing(v: Int?) = when (v) {
        CameraCharacteristics.LENS_FACING_BACK -> "背面"
        CameraCharacteristics.LENS_FACING_FRONT -> "前面"
        CameraCharacteristics.LENS_FACING_EXTERNAL -> "外付け"
        else -> "不明($v)"
    }

    // 4 = EXTERNAL は API 28 の定数なので数で比べる
    private fun level(v: Int?) = when (v) {
        CameraCharacteristics.INFO_SUPPORTED_HARDWARE_LEVEL_LEGACY -> "LEGACY"
        CameraCharacteristics.INFO_SUPPORTED_HARDWARE_LEVEL_LIMITED -> "LIMITED"
        CameraCharacteristics.INFO_SUPPORTED_HARDWARE_LEVEL_FULL -> "FULL"
        CameraCharacteristics.INFO_SUPPORTED_HARDWARE_LEVEL_3 -> "LEVEL_3"
        4 -> "EXTERNAL"
        else -> "不明($v)"
    }
}
