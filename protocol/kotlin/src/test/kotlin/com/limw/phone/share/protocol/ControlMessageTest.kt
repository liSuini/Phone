package com.limw.phone.share.protocol

import java.io.File
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertNull

/**
 * 票据 01：控制消息契约测试（红→绿循环）。
 * 缝：protocol 契约缝（技术规格 Testing Decisions 第 4 缝）。
 * 样例文件是共同真理之源，对拍用例必须读 protocol/samples/ 而非手写副本。
 */
class ControlMessageTest {

    private fun sample(name: String): String =
        File(System.getProperty("samples.dir") ?: "../samples", name).readText().trim()

    /** 可空 double 字段对拍：同为 null 视为一致，非空则须相等（容差 1e-9） */
    private fun assertEqNullable(expected: Double?, actual: Double?, name: String) {
        if (expected == null && actual == null) return
        assertNotNull(expected, "$name 期望非空")
        assertNotNull(actual, "$name 实际非空")
        assertEquals(expected, actual, 1e-9, "$name 不一致")
    }

    // ---- 基础解析 ----

    @Test
    fun `tap 消息解析`() {
        val m = ControlMessage.parse("""{"t":"tap","x":0.5,"y":0.3}""")
        assertNotNull(m)
        assertEquals("tap", m.t)
        assertEquals(0.5, m.x!!)
        assertEquals(0.3, m.y!!)
    }

    @Test
    fun `swipe 消息解析`() {
        val m = ControlMessage.parse("""{"t":"swipe","x0":0.1,"y0":0.2,"x1":0.8,"y1":0.9,"dur":300}""")
        assertNotNull(m)
        assertEquals("swipe", m.t)
        assertEquals(0.1, m.x0!!)
        assertEquals(0.9, m.y1!!)
        assertEquals(300, m.dur!!)
    }

    @Test
    fun `key 消息解析`() {
        val m = ControlMessage.parse("""{"t":"key","k":"back"}""")
        assertNotNull(m)
        assertEquals("back", m.k)
    }

    @Test
    fun `非法输入返回 null`() {
        assertNull(ControlMessage.parse("not-json"))
    }

    @Test
    fun `未知类型返回 null`() {
        assertNull(ControlMessage.parse("""{"t":"volume"}"""))
    }

    @Test
    fun `tap 缺坐标返回 null`() {
        assertNull(ControlMessage.parse("""{"t":"tap"}"""))
    }

    // ---- 对拍：样例全集 ----

    @Test
    fun `合法样例全部可解析并往返一致`() {
        for (name in listOf("01-tap.json", "02-swipe.json", "03-key-back.json")) {
            val raw = sample(name)
            val m = ControlMessage.parse(raw) ?: error("样例 $name 解析失败")
            val round = ControlMessage.parse(m.toJson()) ?: error("样例 $name 往返失败")
            assertEquals(m.t, round.t, "样例 $name 类型不一致")
            assertEqNullable(m.x, round.x, "样例 $name x")
            assertEqNullable(m.y, round.y, "样例 $name y")
            assertEqNullable(m.x0, round.x0, "样例 $name x0")
            assertEqNullable(m.y0, round.y0, "样例 $name y0")
            assertEqNullable(m.x1, round.x1, "样例 $name x1")
            assertEqNullable(m.y1, round.y1, "样例 $name y1")
            assertEquals(m.dur, round.dur, "样例 $name dur 不一致")
            assertEquals(m.k, round.k, "样例 $name k 不一致")
        }
    }

    @Test
    fun `非法样例全部返回 null`() {
        for (name in listOf(
            "04-invalid-not-json.json",
            "05-invalid-unknown-type.json",
            "06-invalid-missing-coords.json",
        )) {
            assertNull(ControlMessage.parse(sample(name)), "样例 $name 应返回 null")
        }
    }

    // ---- 序列化规则 ----

    @Test
    fun `toJson 只输出非null字段且顺序固定`() {
        val m = ControlMessage(t = "tap", x = 0.5, y = 0.3)
        assertEquals("""{"t":"tap","x":0.5,"y":0.3}""", m.toJson())
    }

    @Test
    fun `未知字段解析时被忽略`() {
        val m = ControlMessage.parse("""{"t":"tap","x":0.25,"y":0.75,"future":"x"}""")
        assertNotNull(m)
        assertEquals(0.25, m.x!!)
        assertEquals(0.75, m.y!!)
    }
}
