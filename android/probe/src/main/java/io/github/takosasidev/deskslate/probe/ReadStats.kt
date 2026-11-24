// 読み取りの回ごとの結果をまとめ、R-3 の基準(3 秒以内に 8 回以上)を判定する。
// Android に依らない。単体テストから同じ物を呼ぶ。
package io.github.takosasidev.deskslate.probe

data class ReadSummary(
    val total: Int,
    val successes: Int,
    val within3s: Int,
    /** 成功した回の時間の中央値(ミリ秒)。成功が 0 回なら null。偶数個なら真ん中 2 つの平均(切り捨て)。 */
    val medianMs: Long?,
) {
    /** R-3: 3 秒以内の成功が 8 回以上。 */
    val meetsR3: Boolean get() = within3s >= 8
}

object ReadStats {
    const val LIMIT_3S_MS = 3000L

    /** times の各要素は 1 回分。読めた回はかかったミリ秒、失敗(10 秒で読めない)は null。 */
    fun summarize(times: List<Long?>): ReadSummary {
        val ok = times.filterNotNull().sorted()
        val median = when {
            ok.isEmpty() -> null
            ok.size % 2 == 1 -> ok[ok.size / 2]
            else -> (ok[ok.size / 2 - 1] + ok[ok.size / 2]) / 2
        }
        return ReadSummary(times.size, ok.size, ok.count { it <= LIMIT_3S_MS }, median)
    }
}
