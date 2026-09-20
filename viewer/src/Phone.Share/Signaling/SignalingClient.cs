using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phone.Share.Signaling;

/// <summary>共享端设备信息（GET /info）。</summary>
public sealed record DeviceInfo(
    [property: JsonPropertyName("deviceName")] string DeviceName,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("battery")] int Battery,
    [property: JsonPropertyName("version")] string Version);

/// <summary>配对结果（POST /pair）。失败时 Error 携带原因，Session 为 null。</summary>
public sealed record PairResult(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("session")] string? Session,
    [property: JsonPropertyName("error")] string? Error)
{
    public static PairResult Fail(string error) => new(false, null, error);
}

/// <summary>票据 02：观看端信令客户端。见 protocol/schema/signaling.md。</summary>
/// <remarks>
/// 契约：纯 HTTP、返回结果不抛异常、每请求 5 秒超时（技术规格实现决策 3 / M-V1）。
/// HttpClient 由外部注入（可测试），不拥有其生命周期。
/// </remarks>
public sealed class SignalingClient
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;

    public SignalingClient(HttpClient http) => _http = http;

    public async Task<DeviceInfo?> GetInfoAsync(string ip, int port, CancellationToken ct = default)
        => await GetAsync<DeviceInfo>($"http://{ip}:{port}/info", ct);

    public async Task<PairResult> PairAsync(string ip, int port, string code, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new PairBody(code), WriteOptions);
        var result = await PostAsync<PairResult>($"http://{ip}:{port}/pair", body, ct);
        return result ?? PairResult.Fail("网络错误：无法连接手机，请检查是否同一 WiFi");
    }

    public async Task<string?> ExchangeOfferAsync(string ip, int port, string sdp, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new OfferBody(sdp), WriteOptions);
        var answer = await PostAsync<AnswerBody>($"http://{ip}:{port}/offer", body, ct);
        return answer?.Answer;
    }

    // ---- 内部 ----

    private sealed record PairBody([property: JsonPropertyName("code")] string Code);
    private sealed record OfferBody([property: JsonPropertyName("sdp")] string Sdp);
    private sealed record AnswerBody([property: JsonPropertyName("answer")] string Answer);

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            using var resp = await _http.GetAsync(url, cts.Token);
            if (!resp.IsSuccessStatusCode) return default;
            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or OperationCanceledException)
        {
            return default;
        }
    }

    private async Task<T?> PostAsync<T>(string url, string jsonBody, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            using var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(url, content, cts.Token);
            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or OperationCanceledException)
        {
            return default;
        }
    }
}
