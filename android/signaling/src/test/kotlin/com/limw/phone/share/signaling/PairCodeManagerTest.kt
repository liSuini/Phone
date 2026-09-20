package com.limw.phone.share.signaling

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

/**
 * 票据 03：配对码管理器测试。
 * 验收：4 位数字、15 分钟失效、验证一次后失效（票据 01 期约定的信令契约）。
 * clock 注入使时间可控（codebase-design：接受依赖，不创建依赖）。
 */
class PairCodeManagerTest {

    private fun managerWith(now: () -> Long) = PairCodeManager(clock = now)

    @Test
    fun `签发4位数字码`() {
        var now = 1_000_000L
        val m = managerWith { now }
        repeat(50) {
            assertTrue(Regex("^\\d{4}$").matches(m.issue()), "签发码必须是 4 位数字")
        }
    }

    @Test
    fun `刚签发的码验证通过`() {
        var now = 1_000_000L
        val m = managerWith { now }
        val code = m.issue()
        now += 1_000
        assertTrue(m.verify(code), "15 分钟内正确码应通过")
    }

    @Test
    fun `验证一次后立即失效`() {
        val m = managerWith { 1_000_000L }
        val code = m.issue()
        assertTrue(m.verify(code))
        assertFalse(m.verify(code), "一次性使用：第二次验证必须失败")
    }

    @Test
    fun `15分钟前签发的码被拒绝`() {
        var now = 1_000_000_000L
        val m = managerWith { now }
        val code = m.issue()
        now += PairCodeManager.VALIDITY_MS + 1
        assertFalse(m.verify(code), "超过 15 分钟必须失效")
    }

    @Test
    fun `错误码不消耗签发码`() {
        val m = managerWith { 1_000_000L }
        val code = m.issue()
        assertFalse(m.verify("0000"))
        assertTrue(m.verify(code), "错一次尝试不应使真码失效")
    }

    @Test
    fun `未签发时验证任意码失败`() {
        val m = managerWith { 0L }
        assertFalse(m.verify("1234"))
        assertEquals(4, PairCodeManager.CODE_LENGTH)
    }
}
