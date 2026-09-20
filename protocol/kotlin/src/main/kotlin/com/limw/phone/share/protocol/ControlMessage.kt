package com.limw.phone.share.protocol

import org.json.JSONException
import org.json.JSONObject

/**
 * 控制消息契约（票据 01）。见 protocol/schema/control-message.md：
 * - 坐标为归一化坐标（0~1，CONTEXT.md），契约层 double，下游换算 float
 * - 非法 JSON、未知类型、字段缺失一律返回 null，不抛异常
 * - 序列化只输出非 null 字段，字段顺序 t,x,y,x0,y0,x1,y1,dur,k
 */
data class ControlMessage(
    val t: String,
    val x: Double? = null,
    val y: Double? = null,
    val x0: Double? = null,
    val y0: Double? = null,
    val x1: Double? = null,
    val y1: Double? = null,
    val dur: Int? = null,
    val k: String? = null,
) {
    companion object {
        fun parse(json: String): ControlMessage? {
            val obj = try {
                JSONObject(json)
            } catch (_: JSONException) {
                return null
            } catch (_: Exception) {
                return null
            }
            return when (obj.optString("t")) {
                "tap" -> {
                    if (!obj.has("x") || !obj.has("y")) return null
                    ControlMessage(
                        t = "tap",
                        x = obj.getDouble("x"),
                        y = obj.getDouble("y"),
                    )
                }
                "swipe" -> {
                    if (!obj.has("x0") || !obj.has("y0") || !obj.has("x1") || !obj.has("y1") || !obj.has("dur")) return null
                    ControlMessage(
                        t = "swipe",
                        x0 = obj.getDouble("x0"),
                        y0 = obj.getDouble("y0"),
                        x1 = obj.getDouble("x1"),
                        y1 = obj.getDouble("y1"),
                        dur = obj.optInt("dur"),
                    )
                }
                "key" -> {
                    if (!obj.has("k")) return null
                    ControlMessage(t = "key", k = obj.optString("k"))
                }
                else -> null
            }
        }
    }

    fun toJson(): String = buildString {
        append("{\"t\":").append(JSONObject.quote(t))
        x?.let { appendNumber("x", it) }
        y?.let { appendNumber("y", it) }
        x0?.let { appendNumber("x0", it) }
        y0?.let { appendNumber("y0", it) }
        x1?.let { appendNumber("x1", it) }
        y1?.let { appendNumber("y1", it) }
        dur?.let { appendNumber("dur", it) }
        k?.let { append(",\"k\":").append(JSONObject.quote(it)) }
        append('}')
    }

    private fun StringBuilder.appendNumber(name: String, value: Number) {
        append(",\"").append(name).append("\":").append(value)
    }
}
