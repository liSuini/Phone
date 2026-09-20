package com.limw.phone.share.signaling

/**
 * 共享端设备信息（GET /info 响应体，见 protocol/schema/signaling.md）。
 */
data class DeviceInfo(
    val deviceName: String,
    val width: Int,
    val height: Int,
    val battery: Int,
    val version: String,
)
