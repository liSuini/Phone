using System.Windows.Media.Imaging;
using Phone.Share.Rtc;

namespace Phone.Share.Render;

/// <summary>
/// WPF WriteableBitmap 帧渲染器（票据 04，M-V3 第一适配器）。
/// Write 非阻塞：仅替换待渲染帧（保新弃旧）；RenderNow 在 UI 线程把最新帧写入位图。
/// 大帧渲染进小位图时按位图尺寸从帧左上区域采样（2 的倍数步长），不越界。
/// </summary>
public sealed class WpfFrameSink : IFrameSink
{
    private readonly WriteableBitmap _bitmap;
    private I420Frame? _pending;
    private readonly object _gate = new();

    public WpfFrameSink(WriteableBitmap bitmap)
    {
        _bitmap = bitmap;
    }

    public void Write(I420Frame frame)
    {
        lock (_gate)
        {
            _pending = frame; // 保新弃旧：只保留最新一帧
        }
    }

    public void RenderNow()
    {
        I420Frame? frame;
        lock (_gate)
        {
            frame = _pending;
            _pending = null;
        }
        if (frame is null || _bitmap.Height == 0)
        {
            return;
        }

        var bgra = YuvConverter.ToBgra32(frame);
        var bw = (int)_bitmap.PixelWidth;
        var bh = (int)_bitmap.PixelHeight;

        // 帧尺寸与位图一致：直写；不一致：整数步长采样（帧大位图小）
        if (frame.Width == bw && frame.Height == bh)
        {
            _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, bw, bh), bgra, bw * 4, 0);
            return;
        }

        var sampled = new byte[bw * bh * 4];
        var stepX = frame.Width / bw;
        var stepY = frame.Height / bh;
        if (stepX < 1) stepX = 1;
        if (stepY < 1) stepY = 1;

        for (var row = 0; row < bh; row++)
        {
            var srcRow = Math.Min(row * stepY, frame.Height - 1) * frame.Width * 4;
            for (var col = 0; col < bw; col++)
            {
                var srcIdx = srcRow + Math.Min(col * stepX, frame.Width - 1) * 4;
                var dstIdx = row * bw * 4 + col * 4;
                sampled[dstIdx] = bgra[srcIdx];
                sampled[dstIdx + 1] = bgra[srcIdx + 1];
                sampled[dstIdx + 2] = bgra[srcIdx + 2];
                sampled[dstIdx + 3] = 255;
            }
        }
        _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, bw, bh), sampled, bw * 4, 0);
    }
}
