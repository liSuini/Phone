package com.limw.phone.share.signaling

import java.util.concurrent.CountDownLatch

/**
 * 票据 03 对拍入口：固定端口与配对码，供 C# SignalingClient（票据 02）本机对跑。
 * 由 protocol/parity/run-parity.ps1 启停，验收标准：
 *  GET /info、POST /pair（1234 放行 / 其他 403）、POST /offer（answer 回显）
 */
fun main() {
    val server = SignalingServer(
        port = 18099,
        infoProvider = { DeviceInfo("对拍机", 1080, 2400, 88, "1.0-test") },
    )
    server.onPair = { it == "1234" }
    server.onOffer = { sdp -> "answer::$sdp" }
    server.start()
    println("parity-server-ready port=18099 code=1234")
    CountDownLatch(1).await() // 挂住主线程，由脚本终止进程
}
