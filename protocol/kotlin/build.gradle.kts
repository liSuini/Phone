plugins {
    kotlin("jvm") version "1.9.25"
}

repositories {
    mavenCentral()
}

dependencies {
    // JVM 侧 org.json 独立库；Android 工程复用系统内置 org.json，API 兼容
    implementation("org.json:json:20240303")
    testImplementation(kotlin("test"))
}

kotlin {
    jvmToolchain(21)
}

tasks.test {
    useJUnitPlatform()
    // 样例目录作为系统属性注入，避免依赖测试 cwd
    systemProperty("samples.dir", File(projectDir, "../samples").absolutePath)
}
