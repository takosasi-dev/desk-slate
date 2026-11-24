// カメラの Y 面(輝度)を ZXing で読む。Android に依らないので単体テストから同じ物を呼ぶ。
// rowStride が幅と違う Y 面を詰め直す処理と、縦向きの 1D コードのための 90 度回しを持つ。
package io.github.takosasidev.deskslate.probe

import com.google.zxing.BarcodeFormat
import com.google.zxing.BinaryBitmap
import com.google.zxing.DecodeHintType
import com.google.zxing.MultiFormatReader
import com.google.zxing.PlanarYUVLuminanceSource
import com.google.zxing.ReaderException
import com.google.zxing.common.HybridBinarizer
import java.nio.ByteBuffer

object Luma {
    /**
     * Y 面を幅 x 高さの詰めた配列にする。rowStride > 幅なら行の後ろの詰め物を飛ばす。
     * 最後の行は幅の分しか無いことがあるので、まとめて読まずに 1 行ずつ読む。
     */
    fun packY(plane: ByteBuffer, width: Int, height: Int, rowStride: Int): ByteArray {
        val out = ByteArray(width * height)
        val buf = plane.duplicate()
        val base = buf.position()
        if (rowStride == width) {
            buf.get(out)
        } else {
            for (row in 0 until height) {
                buf.position(base + row * rowStride)
                buf.get(out, row * width, width)
            }
        }
        return out
    }

    /** 時計回りに 90 度回す。戻り値は 幅 = height、高さ = width。 */
    fun rotate90(src: ByteArray, width: Int, height: Int): ByteArray {
        val out = ByteArray(src.size)
        for (y in 0 until height) {
            for (x in 0 until width) {
                out[x * height + (height - 1 - y)] = src[y * width + x]
            }
        }
        return out
    }
}

/** 1 つの形式だけを読む。PlanarYUVLuminanceSource は回転に対応しない(isRotateSupported = false)ので自分で回す。 */
class BarcodeDecoder(format: BarcodeFormat) {
    private val reader = MultiFormatReader().apply {
        setHints(mapOf(DecodeHintType.POSSIBLE_FORMATS to listOf(format)))
    }

    /** 読めたら中身の文字、読めなければ null。そのまま → 90 度回して の順に試す。 */
    fun decode(y: ByteArray, width: Int, height: Int): String? =
        tryDecode(y, width, height) ?: tryDecode(Luma.rotate90(y, width, height), height, width)

    private fun tryDecode(y: ByteArray, width: Int, height: Int): String? {
        val source = PlanarYUVLuminanceSource(y, width, height, 0, 0, width, height, false)
        return try {
            reader.decodeWithState(BinaryBitmap(HybridBinarizer(source))).text
        } catch (e: ReaderException) {
            null
        } finally {
            reader.reset()
        }
    }
}
