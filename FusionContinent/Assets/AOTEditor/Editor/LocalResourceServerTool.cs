using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;

/// <summary>
/// 本地资源服务器：把 CDN 目录（默认 D:/CDN）通过 http 暴露出来，
/// 供 WebGL/微信小游戏开发期走 WebPlayMode 请求 bundle，免去每次上传 CDN。
///
/// 运行时请求地址 = http://{AotConfig.CdnUrl}/{平台目录}/v{版本号}/...，
/// 所以 AotConfig.txt 的 CdnUrl 配成 "127.0.0.1:8010" 后，
/// 请求路径形如 http://127.0.0.1:8010/WebGL/v1.0.0/xxx.bundle，正好映射到 D:/CDN/WebGL/v1.0.0/。
///
/// 注意：仅绑定 localhost，真机（手机微信）访问不到 PC 的 127.0.0.1，
/// 真机调试需要改用支持绑定局域网 IP 的服务器（如 nginx）或 netsh 加 URL ACL。
/// </summary>
public static class LocalResourceServerTool
{
    private const int Port = 8010;
    private const string ServeRoot = "D:/CDN";

    private static HttpListener _listener;
    private static Thread _thread;

    static LocalResourceServerTool()
    {
        // 域重载/退出编辑器时释放端口，否则下次启动会端口占用
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
    }

    private static bool IsRunning
    {
        get { return _listener != null && _listener.IsListening; }
    }

    [MenuItem("WEBGL/本地资源服务器/启动 (D:/CDN @ 8010)")]
    private static void Start()
    {
        if (!Directory.Exists(ServeRoot))
        {
            LogTool.LogError($"[本地资源服务器] 目录不存在: {ServeRoot}，请先构建 bundle（Tools/Build WebGL 重新生成Bundle）");
            return;
        }

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
        }
        catch (Exception e)
        {
            LogTool.LogException(e);
            _listener = null;
            return;
        }

        _thread = new Thread(ListenLoop) { IsBackground = true };
        _thread.Start();
        LogTool.Log($"[本地资源服务器] 已启动: http://127.0.0.1:{Port}/ -> {ServeRoot}");
        LogTool.Log($"[本地资源服务器] 请把 Assets/Resources/AotConfig.txt 的 CdnUrl 设为 \"127.0.0.1:{Port}\"");
    }

    [MenuItem("WEBGL/本地资源服务器/启动 (D:/CDN @ 8010)", true)]
    private static bool StartValidate()
    {
        return !IsRunning;
    }

    [MenuItem("WEBGL/本地资源服务器/停止")]
    private static void Stop()
    {
        if (!IsRunning)
            return;

        _listener.Stop();
        _listener.Close();
        _listener = null;
        LogTool.Log("[本地资源服务器] 已停止");
    }

    [MenuItem("WEBGL/本地资源服务器/停止", true)]
    private static bool StopValidate()
    {
        return IsRunning;
    }

    private static void ListenLoop()
    {
        while (IsRunning)
        {
            HttpListenerContext context;
            try
            {
                context = _listener.GetContext();
            }
            catch
            {
                break; // Stop 时 GetContext 抛异常，正常退出
            }
            ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
        }
    }

    private static void HandleRequest(HttpListenerContext context)
    {
        try
        {
            string urlPath = Uri.UnescapeDataString(context.Request.Url.AbsolutePath).TrimStart('/');
            string filePath = Path.GetFullPath(Path.Combine(ServeRoot, urlPath));

            // 防目录穿越
            if (!filePath.StartsWith(Path.GetFullPath(ServeRoot), StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                return;
            }

            // 浏览器调试时 UnityWebRequest 需要 CORS 头，微信开发者工具走 wx 网络栈不受影响
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");

            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                context.Response.ContentType = "application/octet-stream";
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
                LogTool.LogWarning($"[本地资源服务器] 404: {urlPath}");
            }
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 500;
            LogTool.LogException(e);
        }
        finally
        {
            context.Response.OutputStream.Close();
        }
    }
}
