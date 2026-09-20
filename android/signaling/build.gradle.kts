plugins {
    kotlin("jvm") version "1.9.25"
    application
}

repositories {
    mavenCentral()
}

dependencies {
    implementation("org.nanohttpd:nanohttpd:2.3.1")
    // JVM 侧 org.json 独立库；Android 模块复用系统内置 org.json，API 兼容
    implementation("org.json:json:20240303")
    testImplementation(kotlin("test"))
}

kotlin {
    jvmToolchain(17)
}

tasks.test {
    useJUnitPlatform()
}

application {
    // 票据 03 对拍入口：固定端口与配对码，供 C# 客户端本机对跑
    mainClass.set("com.limw.phone.share.signaling.ParityServerMainKt")
}
