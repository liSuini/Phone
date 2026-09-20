# 票据 03 双端本机对拍：Kotlin SignalingServer ↔ C# SignalingClient
# 用法：powershell -File protocol\parity\run-parity.ps1
# 通过标准：4 个 Parity 测试全绿（GetInfo / Pair 成功 / Pair 403 / Offer 回显）

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent   # protocol/parity -> 仓库根

# ---- 工具定位（PATH 优先，回退 D:\developTools） ----
$javaHome = if ($env:JAVA_HOME) { $env:JAVA_HOME } else { "D:\developTools\JDK\jdk17.0.16" }
if (Test-Path $javaHome) { $env:JAVA_HOME = $javaHome }

# 系统 PATH 可能含 runtime-only 的 dotnet（无 SDK），SDK 路径无条件前置（幂等）
if (Test-Path "D:\developTools\dotnet\dotnet.exe") {
    $env:PATH = "D:\developTools\dotnet;$env:PATH"
}

# ---- 1. 构建 Kotlin 对拍服务器 ----
Write-Output "[1/4] 构建对拍服务器 (gradle installDist)..."
& "$repoRoot\android\gradlew.bat" -p "$repoRoot\android\signaling" --no-daemon installDist --console=plain 2>&1 | Select-Object -Last 1
if ($LASTEXITCODE -ne 0) { throw "gradle installDist 失败" }

# ---- 2. 启动服务器（后台） ----
Write-Output "[2/4] 启动对拍服务器 (127.0.0.1:18099, 配对码 1234)..."
$serverBat = "$repoRoot\android\signaling\build\install\signaling\bin\signaling.bat"
if (-not (Test-Path $serverBat)) { throw "未找到 $serverBat" }
$server = Start-Process -FilePath $serverBat -WindowStyle Hidden -PassThru -RedirectStandardOutput "$env:TEMP\parity-server.log"
try {
    $ready = $false
    foreach ($i in 1..60) {
        try {
            $r = Invoke-WebRequest -Uri "http://127.0.0.1:18099/info" -UseBasicParsing -TimeoutSec 2
            if ($r.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) {
        Get-Content "$env:TEMP\parity-server.log" -ErrorAction SilentlyContinue | Select-Object -Last 10
        throw "对拍服务器 30 秒内未就绪"
    }
    Write-Output "       服务器就绪"

    # ---- 3. 运行 C# 对拍测试 ----
    Write-Output "[3/4] 运行 C# Parity 测试..."
    dotnet test "$repoRoot\viewer\tests\Phone.Share.Tests" --filter "Category=Parity" --nologo
    if ($LASTEXITCODE -ne 0) { throw "Parity 测试失败" }
}
finally {
    # ---- 4. 清理服务器进程 ----
    Write-Output "[4/4] 停止对拍服务器 (pid $($server.Id))..."
    if (-not $server.HasExited) {
        taskkill.exe /PID $server.Id /T /F 2>&1 | Out-Null
    }
}

Write-Output ""
Write-Output "=== 对拍通过：C# 信令客户端与 Kotlin 信令服务器全链路互通 ==="
