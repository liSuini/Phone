namespace Phone.Share.Render;

/// <summary>
/// I420→BGRA32 转换（票据 04，M-V3）。
/// BT.601 有限范围（SDR 视频 Y∈[16,235]），整数近似公式——与 FFmpeg/主流播放器一致：
///   C = Y-16, D = U-128, E = V-128
///   R = clip((298C + 409E + 128) >> 8)
///   G = clip((298C - 100D - 208E + 128) >> 8)
///   B = clip((298C + 516D + 128) >> 8)
/// </summary>
public static class YuvConverter
{
    public static byte[] ToBgra32(Rtc.I420Frame f)
    {
        var w = f.Width;
        var h = f.Height;
        var bgra = new byte[w * h * 4];
        var uw = f.UWidth;
        var vh = f.UHeight;

        for (var row = 0; row < h; row++)
        {
            var yRow = row * w;
            var uvRow = (row / 2) * uw;
            var bgraRow = row * w * 4;
            for (var col = 0; col < w; col++)
            {
                var c = f.Y[yRow + col] - 16;
                var d = f.U[uvRow + col / 2] - 128;
                var e = f.V[uvRow + col / 2] - 128;

                var r = Clip((298 * c + 409 * e + 128) >> 8);
                var g = Clip((298 * c - 100 * d - 208 * e + 128) >> 8);
                var b = Clip((298 * c + 516 * d + 128) >> 8);

                var idx = bgraRow + col * 4;
                bgra[idx] = (byte)b;
                bgra[idx + 1] = (byte)g;
                bgra[idx + 2] = (byte)r;
                bgra[idx + 3] = 255;
            }
        }
        _ = vh; // U 高度已隐含在 uvRow 推导中
        return bgra;
    }

    private static int Clip(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
}
