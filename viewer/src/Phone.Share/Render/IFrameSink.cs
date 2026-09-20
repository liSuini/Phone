using Phone.Share.Rtc;

namespace Phone.Share.Render;

/// <summary>
/// 视频输出缝（M-V3）：第一适配器 WpfFrameSink（WriteableBitmap）；
/// 性能不达标时第二适配器换 SkiaSharp/D3D，接口与上游零改动（ADR-0004 后备路径）。
/// </summary>
public interface IFrameSink
{
    /// <summary>非阻塞写入：帧排队丢弃策略为保新弃旧（接口性能特征的一部分）。</summary>
    void Write(I420Frame frame);

    /// <summary>立即渲染当前待渲染帧（由 UI 帧驱动或测试显式调用）。</summary>
    void RenderNow();
}
