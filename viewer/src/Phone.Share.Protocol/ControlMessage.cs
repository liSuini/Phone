using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phone.Share.Protocol;

/// <summary>
/// 控制消息契约（票据 01）。见 protocol/schema/control-message.md：
/// - 坐标为归一化坐标（0~1，CONTEXT.md），契约层 double，下游换算 float
/// - 非法 JSON、未知类型、字段缺失一律返回 null，不抛异常
/// - 序列化只输出非 null 字段，字段顺序 = 构造参数顺序 t,x,y,x0,y0,x1,y1,dur,k
/// </summary>
public sealed record ControlMessage(
    [property: JsonPropertyName("t")] string T,
    [property: JsonPropertyName("x")] double? X,
    [property: JsonPropertyName("y")] double? Y,
    [property: JsonPropertyName("x0")] double? X0,
    [property: JsonPropertyName("y0")] double? Y0,
    [property: JsonPropertyName("x1")] double? X1,
    [property: JsonPropertyName("y1")] double? Y1,
    [property: JsonPropertyName("dur")] int? Dur,
    [property: JsonPropertyName("k")] string? K)
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        // 严格小写字段名，防字段名大小写漂移
        PropertyNameCaseInsensitive = false,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ControlMessage? Parse(string json)
    {
        try
        {
            var m = JsonSerializer.Deserialize<ControlMessage>(json, ReadOptions);
            if (m is null) return null;
            return m.T switch
            {
                "tap" => m.X.HasValue && m.Y.HasValue ? m : null,
                "swipe" => m.X0.HasValue && m.Y0.HasValue && m.X1.HasValue && m.Y1.HasValue && m.Dur.HasValue ? m : null,
                "key" => string.IsNullOrEmpty(m.K) ? null : m,
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, WriteOptions);
}
