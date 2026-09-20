using System.Windows.Media;
using System.Windows.Media.Imaging;
using Phone.Share.Render;
using Phone.Share.Rtc;
using Xunit;

namespace Phone.Share.Tests;

/// <summary>
/// 票据 04：WpfFrameSink 测试。
/// Write 非阻塞（存最新帧），渲染动作显式执行（RenderNow），测试无需 UI Dispatcher。
/// 保新弃旧：多帧写入只保留最新。
/// </summary>
public class WpfFrameSinkTests
{
    private static WriteableBitmap NewBitmap(int w, int h) => new(w, h, 96, 96, PixelFormats.Bgra32, null);

    [Fact]
    public void 渲染黑帧后位图像素为零()
    {
        var bmp = NewBitmap(4, 4);
        var sink = new WpfFrameSink(bmp);

        sink.Write(I420Frame.CreateFilled(4, 4, 16, 128, 128));
        sink.RenderNow();

        byte[] pixels = new byte[4 * 4 * 4];
        bmp.CopyPixels(pixels, 4 * 4, 0);
        for (var i = 0; i < pixels.Length; i += 4)
        {
            Assert.Equal(0, pixels[i]);     // B
            Assert.Equal(0, pixels[i + 1]); // G
            Assert.Equal(0, pixels[i + 2]); // R
            Assert.Equal(255, pixels[i + 3]); // A（不透明）
        }
    }

    [Fact]
    public void 渲染白帧后位图像素为满()
    {
        var bmp = NewBitmap(4, 4);
        var sink = new WpfFrameSink(bmp);

        sink.Write(I420Frame.CreateFilled(4, 4, 235, 128, 128));
        sink.RenderNow();

        byte[] pixels = new byte[4 * 4 * 4];
        bmp.CopyPixels(pixels, 4 * 4, 0);
        for (var i = 0; i < pixels.Length; i += 4)
        {
            Assert.Equal(255, pixels[i]);
            Assert.Equal(255, pixels[i + 1]);
            Assert.Equal(255, pixels[i + 2]);
        }
    }

    [Fact]
    public void 多帧写入只保留最新一帧()
    {
        var bmp = NewBitmap(4, 4);
        var sink = new WpfFrameSink(bmp);

        sink.Write(I420Frame.CreateFilled(4, 4, 16, 128, 128));   // 黑
        sink.Write(I420Frame.CreateFilled(4, 4, 235, 128, 128));  // 白
        sink.RenderNow();

        byte[] pixels = new byte[4 * 4 * 4];
        bmp.CopyPixels(pixels, 4 * 4, 0);
        Assert.Equal(255, pixels[0]); // 最新帧（白）生效
    }

    [Fact]
    public void 渲染大尺寸帧按位图区域裁剪()
    {
        // 手机竖屏 1080x2400 帧渲染进小位图：帧按位图尺寸采样左上区域，不越界
        var bmp = NewBitmap(8, 8);
        var sink = new WpfFrameSink(bmp);

        sink.Write(I420Frame.CreateFilled(1080, 2400, 200, 128, 128));
        sink.RenderNow();

        byte[] pixels = new byte[8 * 8 * 4];
        bmp.CopyPixels(pixels, 8 * 4, 0);
        // 均匀亮度 200 的帧，任意采样点亮度应显著高于黑
        Assert.True(pixels[1] > 150, $"亮度异常: {pixels[1]}");
    }
}
