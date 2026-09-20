package com.limw.phone.share.signaling

/**
 * 配对码管理器（票据 03）。
 *
 * 行为契约（见 docs/.scratch 票据 03 与 protocol/schema/signaling.md）：
 * - 签发 4 位数字码（1000~9999），签发即开始计时
 * - 15 分钟内且未使用过的码验证通过；验证通过立即失效（一次性）
 * - 错误码验证不消耗当前签发码（防暴力试探时自我失效）
 *
 * clock 由构造注入：生产传系统时钟，测试传可控时钟。
 */
class PairCodeManager(
    private val clock: () -> Long = System::currentTimeMillis,
) {
    private var code: String? = null
    private var issuedAt: Long = Long.MIN_VALUE
    private var consumed: Boolean = true

    fun issue(): String {
        val next = (1000..9999).random().toString()
        code = next
        issuedAt = clock()
        consumed = false
        return next
    }

    fun verify(candidate: String): Boolean {
        val current = code ?: return false
        if (consumed) return false
        if (clock() - issuedAt > VALIDITY_MS) return false
        if (candidate != current) return false
        consumed = true
        return true
    }

    companion object {
        const val CODE_LENGTH = 4
        const val VALIDITY_MS: Long = 15 * 60 * 1000
    }
}
