package com.limw.phone.share.signaling

import java.io.DataOutputStream
import java.net.HttpURLConnection
import java.net.ServerSocket
import java.net.URL
import kotlin.test.AfterTest
import kotlin.test.BeforeTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

/**
 * 票据 03：信令服务器测试（真实 HTTP 对打，遵循 protocol/schema/signaling.md）。
 * 缝：M-S4 SignalingServer 公开接口（onPair/onOffer 回调 + HTTP 端点行为）。
 */
class SignalingServerTest {

    private lateinit var server: SignalingServer
    private var port: Int = 0
    private val fixedInfo = DeviceInfo("测试机", 1080, 2400, 88, "1.0")
    private var offeredSdp: String? = null

    @BeforeTest
    fun setUp() {
        port = ServerSocket(0).use { it.localPort }
        server = SignalingServer(
            port = port,
            infoProvider = { fixedInfo },
        )
        server.onPair = { it == "1234" }
        server.onOffer = { sdp ->
            offeredSdp = sdp
            "answer-of-$sdp"
        }
        server.start()
        // 轮询真实请求直到就绪（NanoHTTPD.start 为异步起线程）
        var ready = false
        for (i in 1..50) {
            try {
                val conn = URL("http://127.0.0.1:$port/info").openConnection() as HttpURLConnection
                conn.connectTimeout = 500
                if (conn.responseCode == 200) {
                    ready = true
                    break
                }
            } catch (_: Exception) {
                Thread.sleep(100)
            }
        }
        assertTrue(ready, "服务器端口未就绪")
    }

    @AfterTest
    fun tearDown() {
        server.stop()
    }

    // ---- HTTP 帮助函数（java.net 内置，零依赖） ----

    private fun httpGet(path: String): Pair<Int, String> {
        val conn = URL("http://127.0.0.1:$port$path").openConnection() as HttpURLConnection
        conn.connectTimeout = 3000
        val status = conn.responseCode
        val body = if (status in 200..299) conn.inputStream else conn.errorStream
        return status to body.bufferedReader().readText()
    }

    private fun httpPost(path: String, body: String): Pair<Int, String> {
        val conn = URL("http://127.0.0.1:$port$path").openConnection() as HttpURLConnection
        conn.requestMethod = "POST"
        conn.doOutput = true
        conn.setRequestProperty("Content-Type", "application/json")
        conn.connectTimeout = 3000
        DataOutputStream(conn.outputStream).use { it.writeBytes(body) }
        val status = conn.responseCode
        val respBody = if (status in 200..299) conn.inputStream else conn.errorStream
        return status to respBody.bufferedReader().readText()
    }

    // ---- GET /info ----

    @Test
    fun `info端点返回设备信息`() {
        val (status, body) = httpGet("/info")
        assertEquals(200, status)
        // 契约只约定字段名与值（JSON 对象字段顺序无意义），做字段级断言
        val json = org.json.JSONObject(body)
        assertEquals("测试机", json.getString("deviceName"))
        assertEquals(1080, json.getInt("width"))
        assertEquals(2400, json.getInt("height"))
        assertEquals(88, json.getInt("battery"))
        assertEquals("1.0", json.getString("version"))
    }

    // ---- POST /pair ----

    @Test
    fun `pair配对码正确返回会话`() {
        val (status, body) = httpPost("/pair", """{"code":"1234"}""")
        assertEquals(200, status)
        assertTrue(body.contains("\"ok\":true"), "应包含 ok:true，实际: $body")
        assertTrue(body.contains("session"), "应包含 session 字段，实际: $body")
    }

    @Test
    fun `pair配对码错误返回403与错误信息`() {
        val (status, body) = httpPost("/pair", """{"code":"9999"}""")
        assertEquals(403, status)
        assertEquals("""{"ok":false,"error":"配对码错误"}""", body)
    }

    @Test
    fun `pair畸形请求返回400`() {
        val (status, _) = httpPost("/pair", "not-json")
        assertEquals(400, status)
    }

    // ---- POST /offer ----

    @Test
    fun `offer端点回调返回answer`() {
        val (status, body) = httpPost("/offer", """{"sdp":"v=0-offer"}""")
        assertEquals(200, status)
        assertEquals("v=0-offer", offeredSdp, "onOffer 回调应收到原始 SDP")
        assertEquals("""{"answer":"answer-of-v=0-offer"}""", body)
    }

    @Test
    fun `offer畸形请求返回400`() {
        val (status, _) = httpPost("/offer", "broken")
        assertEquals(400, status)
    }

    // ---- 未知路由 ----

    @Test
    fun `未知路由返回404`() {
        val (status, _) = httpGet("/nope")
        assertEquals(404, status)
    }
}
