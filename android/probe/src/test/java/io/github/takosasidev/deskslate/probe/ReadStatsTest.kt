// ReadStats(R-3 の集計)の単体テスト。成功の数・3 秒以内の数・中央値・判定を見る。
package io.github.takosasidev.deskslate.probe

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ReadStatsTest {
    @Test
    fun eightWithin3s_meetsR3() {
        val s = ReadStats.summarize(listOf(500L, 800L, 1200L, 3000L, 900L, 700L, 2000L, 1500L, 3001L, null))
        assertEquals(10, s.total)
        assertEquals(9, s.successes)
        assertEquals(8, s.within3s) // 3000 は以内、3001 は外
        assertEquals(1200L, s.medianMs) // 500,700,800,900,1200,1500,2000,3000,3001
        assertTrue(s.meetsR3)
    }

    @Test
    fun sevenWithin3s_doesNotMeetR3() {
        val s = ReadStats.summarize(listOf(100L, 200L, 300L, 400L, 500L, 600L, 700L, 4000L, null, null))
        assertEquals(8, s.successes)
        assertEquals(7, s.within3s)
        assertEquals(450L, s.medianMs) // 偶数個: (400 + 500) / 2
        assertFalse(s.meetsR3)
    }

    @Test
    fun allFailed() {
        val s = ReadStats.summarize(List(10) { null })
        assertEquals(0, s.successes)
        assertEquals(0, s.within3s)
        assertNull(s.medianMs)
        assertFalse(s.meetsR3)
    }
}
