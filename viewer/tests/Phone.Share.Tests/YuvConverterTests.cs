using Phone.Share.Render;
using Phone.Share.Rtc;
using Xunit;

namespace Phone.Share.Tests;

/// <summary>
/// 票据 04：I420→RGB 转换测试（BT.601 有限范围，Y∈[16,235] 映射全色阶）。
/// 期望值来自标准公式（独立于实现的真理之源），非重算实现逻辑。
/// </summary>
public class YuvConverterTests
{
    [Fact]
    public void 黑帧转换为零RGB()
    {
        // Y=16,U=V=128 是 BT.601 有限范围的黑
        var frame = I420Frame.CreateFilled(4, 4, y: 16, u: 128, v: 128);
        var rgb = YuvConverter.ToBgra32(frame);
        for (var i = 0; i < rgb.Length; i += 4)
        {
            Assert.Equal(0, rgb[i]);     // B
            Assert.Equal(0, rgb[i + 1]); // G
            Assert.Equal(0, rgb[i + 2]); // R
            Assert.Equal(255, rgb[i + 3]); // A
        }
    }

    [Fact]
    public void 白帧转换为满RGB()
    {
        // Y=235,U=V=128 是有限范围的白
        var frame = I420Frame.CreateFilled(4, 4, y: 235, u: 128, v: 128);
        var rgb = YuvConverter.ToBgra32(frame);
        for (var i = 0; i < rgb.Length; i += 4)
        {
            Assert.Equal(255, rgb[i]);
            Assert.Equal(255, rgb[i + 1]);
            Assert.Equal(255, rgb[i + 2]);
        }
    }

    [Fact]
    public void 红色帧转换R高于GB()
    {
        // V 高 U 低 → 偏红
        var frame = I420Frame.CreateFilled(4, 4, y: 128, u: 80, v: 180);
        var rgb = YuvConverter.ToBgra32(frame);
        // 第一个像素：BGRA
        var r = rgb[2];
        var g = rgb[1];
        var b = rgb[0];
        Assert.True(r > g && r > b, $"R({r}) 应显著大于 G({g})、B({b})");
    }

    [Fact]
    public void 输出尺寸为宽高乘4字节()
    {
        var frame = I420Frame.CreateFilled(32, 16, 100, 128, 128);
        var rgb = YuvConverter.ToBgra32(frame);
        Assert.Equal(32 * 16 * 4, rgb.Length);
    }

    [Fact]
    public void 非偶数宽高用4对齐平面()
    {
        // U/V 平面为 (w+1)/2 x (h+1)/2；工厂方法按此分配
        var frame = I420Frame.CreateFilled(31, 17, 100, 128, 128);
        Assert.Equal(16 * 9, frame.U.Length);
        Assert.Equal(16 * 9, frame.V.Length);
    }
}
