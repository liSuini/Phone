using System.Net;
using System.Text;

namespace Phone.Share.Tests;

/// <summary>
/// 测试用 HTTP stub：实现 protocol/schema/signaling.md 的共享端三个端点。
/// Route 函数按路径返回 (statusCode, jsonBody)。
/// </summary>
public sealed class SignalingStub : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    public int Port { get; }

    /// <summary>路由函数：路径 → (状态码, JSON body)。默认 404。</summary>
    public Func<string, (int Status, string Body)> Route { get; set; } = _ => (404, "{}");

    public SignalingStub()
    {
        // HttpListener 不支持自动端口，先借 TcpListener 找一个空闲端口
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        Port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Start();
        _loop = Task.Run(AcceptLoop);
    }

    private async Task AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return; // listener stopped
            }

            var (status, body) = Route(ctx.Request.Url!.AbsolutePath);
            ctx.Response.StatusCode = status;
            var bytes = Encoding.UTF8.GetBytes(body);
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        try { _loop.Wait(1000); } catch { /* ignore */ }
    }
}
