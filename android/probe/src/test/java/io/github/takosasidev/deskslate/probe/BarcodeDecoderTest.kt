// BarcodeDecoder と Luma の単体テスト。ZXing の Writer で作った EAN-13 と QR を輝度の配列にして、
// rowStride = 幅、rowStride > 幅(行の後ろに詰め物、最後の行は幅だけ)、90 度回した EAN-13 で読めるかを見る。
package io.github.takosasidev.deskslate.probe

import com.google.zxing.BarcodeFormat
import com.google.zxing.common.BitMatrix
import com.google.zxing.oned.EAN13Writer
import com.google.zxing.qrcode.QRCodeWriter
import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import java.nio.ByteBuffer

class BarcodeDecoderTest {
    private val ean = "4901234567894"
    private val qr = "DeskSlate probe QR 1234"

    private class Img(val y: ByteArray, val w: Int, val h: Int)

    private fun toLuma(m: BitMatrix): Img {
        val y = ByteArray(m.width * m.height) { i -> if (m.get(i % m.width, i / m.width)) 0 else 255.toByte() }
        return Img(y, m.width, m.height)
    }

    private fun eanImage() = toLuma(EAN13Writer().encode(ean, BarcodeFormat.EAN_13, 400, 120))
    private fun qrImage() = toLuma(QRCodeWriter().encode(qr, BarcodeFormat.QR_CODE, 240, 240))

    /** 行の後ろに pad バイトの詰め物を置いた Y 面。最後の行には詰め物を付けない(実機の Y 面と同じ形)。 */
    private fun padded(img: Img, pad: Int): Pair<ByteBuffer, Int> {
        val stride = img.w + pad
        val buf = ByteArray(stride * (img.h - 1) + img.w) { 0x55 }
        for (r in 0 until img.h) System.arraycopy(img.y, r * img.w, buf, r * stride, img.w)
        return ByteBuffer.wrap(buf) to stride
    }

    @Test
    fun ean13_rowStrideEqualsWidth() {
        val img = eanImage()
        val y = Luma.packY(ByteBuffer.wrap(img.y), img.w, img.h, img.w)
        assertEquals(ean, BarcodeDecoder(BarcodeFormat.EAN_13).decode(y, img.w, img.h))
    }

    @Test
    fun ean13_rowStrideLargerThanWidth() {
        val img = eanImage()
        val (buf, stride) = padded(img, 64)
        val y = Luma.packY(buf, img.w, img.h, stride)
        assertArrayEquals(img.y, y)
        assertEquals(ean, BarcodeDecoder(BarcodeFormat.EAN_13).decode(y, img.w, img.h))
    }

    @Test
    fun ean13_rotated90() {
        val img = eanImage()
        // テスト側で独立に回す(反時計回り 90 度。縦向きのバーコード)
        val rot = ByteArray(img.y.size)
        for (yy in 0 until img.h) for (x in 0 until img.w) rot[(img.w - 1 - x) * img.h + yy] = img.y[yy * img.w + x]
        val decoder = BarcodeDecoder(BarcodeFormat.EAN_13)
        assertEquals(ean, decoder.decode(rot, img.h, img.w))
    }

    @Test
    fun qr_rowStrideLargerThanWidth() {
        val img = qrImage()
        val (buf, stride) = padded(img, 32)
        val y = Luma.packY(buf, img.w, img.h, stride)
        assertEquals(qr, BarcodeDecoder(BarcodeFormat.QR_CODE).decode(y, img.w, img.h))
    }

    @Test
    fun onlyTheRequestedFormatIsRead() {
        val img = qrImage()
        assertNull(BarcodeDecoder(BarcodeFormat.EAN_13).decode(img.y, img.w, img.h))
    }

    @Test
    fun rotate90_movesTopLeftToTopRight() {
        // 2x3 の 0..5 を時計回りに回すと 3x2 の [3,0 / 4,1 / 5,2]
        val src = byteArrayOf(0, 1, 2, 3, 4, 5)
        assertArrayEquals(byteArrayOf(3, 0, 4, 1, 5, 2), Luma.rotate90(src, 3, 2))
    }
}
