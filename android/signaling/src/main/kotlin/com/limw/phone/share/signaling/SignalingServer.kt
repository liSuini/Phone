package com.limw.phone.share.signaling

import fi.iki.elonen.NanoHTTPD
import org.json.JSONException
import org.json.JSONObject
import java.util.UUID

/**
 * 票据 03：共享端信令服务器（M-S4，见架构设计文档）。
 *
 * 契约：protocol/schema/signaling.md——三个端点：
 *  GET  /info   → 200 设备信息 JSON
 *  POST /pair   → {"code":"1234"}；验证逻辑由 onPair 注入 → 200 {"ok":true,"session":...} / 403 {"ok":false,"error":"配对码错误"}
 *  POST /offer  → {"sdp":"..."}；answer 由 onOffer 回调产出 → 200 {"answer":"..."}
 *
 * 深模块设计：HTTP、JSON、路由、错误处理内聚于此；
 * 配对验证与 SDP 应答是回调缝，生产由 ShareCoordinator 接 PairCodeManager 与 RtcSession。
 */
class SignalingServer(
    private val port: Int = 18080,
    private val infoProvider: () -> DeviceInfo,
) : NanoHTTPD("0.0.0.0", port) {

    /** 配对验证回调：入参为请求携带的配对码，返回是否放行。 */
    var onPair: (code: String) -> Boolean = { false }

    /** SDP 应答回调：入参为观看端 offer，返回 answer 文本。 */
    var onOffer: (sdp: String) -> String = { "" }

    override fun serve(session: IHTTPSession): Response {
        val path = session.uri
        return try {
            when {
                session.method == Method.GET && path == "/info" -> serveInfo()
                session.method == Method.POST && path == "/pair" -> servePair(session)
                session.method == Method.POST && path == "/offer" -> serveOffer(session)
                else -> notFound()
            }
        } catch (_: Exception) {
            badRequest()
        }
    }

    private fun serveInfo(): Response {
        val info = infoProvider()
        val body = JSONObject()
            .put("deviceName", info.deviceName)
            .put("width", info.width)
            .put("height", info.height)
            .put("battery", info.battery)
            .put("version", info.version)
            .toString()
        return newFixedLengthResponse(Response.Status.OK, "application/json", body)
    }

    private fun servePair(session: IHTTPSession): Response {
        val body = readBody(session) ?: return badRequest()
        val code = try {
            JSONObject(body).getString("code")
        } catch (_: JSONException) {
            return badRequest()
        }
        return if (onPair(code)) {
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                JSONObject().put("ok", true).put("session", UUID.randomUUID().toString()).toString(),
            )
        } else {
            newFixedLengthResponse(
                Response.Status.FORBIDDEN,
                "application/json",
                JSONObject().put("ok", false).put("error", "配对码错误").toString(),
            )
        }
    }

    private fun serveOffer(session: IHTTPSession): Response {
        val body = readBody(session) ?: return badRequest()
        val sdp = try {
            JSONObject(body).getString("sdp")
        } catch (_: JSONException) {
            return badRequest()
        }
        val answer = onOffer(sdp)
        return newFixedLengthResponse(
            Response.Status.OK,
            "application/json",
            JSONObject().put("answer", answer).toString(),
        )
    }

    private fun readBody(session: IHTTPSession): String? {
        val files = HashMap<String, String>()
        session.parseBody(files)
        return files["postData"]
    }

    private fun badRequest(): Response =
        newFixedLengthResponse(Response.Status.BAD_REQUEST, "application/json", """{"error":"bad request"}""")

    private fun notFound(): Response =
        newFixedLengthResponse(Response.Status.NOT_FOUND, "application/json", """{"error":"not found"}""")
}
