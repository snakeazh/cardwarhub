using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Exception = 4,
    None = 99
}

public static class LogTool
{
    private static bool sEnabled = true;
    private static LogLevel sMinLevel = LogLevel.Debug;
    private static bool sIncludeStackTrace = true;
    private static string sDefaultModule = "General";

    public static bool Enabled => sEnabled;
    public static LogLevel MinLevel => sMinLevel;
    public static bool IncludeStackTrace => sIncludeStackTrace;

    public static void SetEnabled(bool enabled) => sEnabled = enabled;
    public static void SetMinLevel(LogLevel minLevel) => sMinLevel = minLevel;
    public static void SetIncludeStackTrace(bool includeStackTrace) => sIncludeStackTrace = includeStackTrace;

    public static void SetDefaultModule(string module)
    {
        sDefaultModule = string.IsNullOrWhiteSpace(module) ? "General" : module;
    }

    public static void Configure(bool enabled, LogLevel minLevel, bool includeStackTrace, string defaultModule = "General")
    {
        sEnabled = enabled;
        sMinLevel = minLevel;
        sIncludeStackTrace = includeStackTrace;
        SetDefaultModule(defaultModule);
    }

    // 与 UnityEngine.Debug 对齐的外部接口
    public static void Log(object message, string module = null, UnityEngine.Object context = null) => Write(LogLevel.Info, message, module, context);
    public static void Log(object message, UnityEngine.Object context) => Write(LogLevel.Info, message, null, context);

    public static void LogWarning(object message, string module = null, UnityEngine.Object context = null) => Write(LogLevel.Warning, message, module, context);
    public static void LogWarning(object message, UnityEngine.Object context) => Write(LogLevel.Warning, message, null, context);

    public static void LogError(object message, string module = null, UnityEngine.Object context = null) => Write(LogLevel.Error, message, module, context);
    public static void LogError(object message, UnityEngine.Object context) => Write(LogLevel.Error, message, null, context);

    public static void LogException(Exception exception, string module = null, UnityEngine.Object context = null)
    {
        if (!CanLog(LogLevel.Exception)) return;

        var msg = BuildMessage(LogLevel.Exception, exception?.Message ?? "Unknown exception", module, 2);
        if (exception != null)
        {
            msg += $"\n[ExceptionType] {exception.GetType().FullName}\n{exception}";
        }

        if (context != null) LogTool.LogError(msg, context);
        else LogTool.LogError(msg);
    }

    public static void LogException(Exception exception, UnityEngine.Object context)
    {
        LogException(exception, null, context);
    }

    // 额外保留的快捷接口
    public static void D(object message, string module = null, UnityEngine.Object context = null) => Write(LogLevel.Debug, message, module, context);
    public static void D(object message, UnityEngine.Object context) => Write(LogLevel.Debug, message, null, context);

    public static void I(object message, string module = null, UnityEngine.Object context = null) => Log(message, module, context);
    public static void I(object message, UnityEngine.Object context) => Log(message, context);

    public static void W(object message, string module = null, UnityEngine.Object context = null) => LogWarning(message, module, context);
    public static void W(object message, UnityEngine.Object context) => LogWarning(message, context);

    public static void E(object message, string module = null, UnityEngine.Object context = null) => LogError(message, module, context);
    public static void E(object message, UnityEngine.Object context) => LogError(message, context);

    public static void Ex(Exception exception, string module = null, UnityEngine.Object context = null) => LogException(exception, module, context);
    public static void Ex(Exception exception, UnityEngine.Object context) => LogException(exception, context);

    // 兼容旧接口
    public static void L(object msg) => Log(msg);

    private static bool CanLog(LogLevel level) => sEnabled && level >= sMinLevel;

    private static void Write(LogLevel level, object message, string module, UnityEngine.Object context)
    {
        if (!CanLog(level)) return;

        var msg = BuildMessage(level, message, module, 3);

        switch (level)
        {
            case LogLevel.Warning:
                if (context != null) Debug.LogWarning(msg, context);
                else Debug.LogWarning(msg);
                break;
            case LogLevel.Error:
                if (context != null) Debug.LogError(msg, context);
                else Debug.LogError(msg);
                break;
            default:
                if (context != null) Debug.Log(msg, context);
                else Debug.Log(msg);
                break;
        }
    }

    private static string BuildMessage(LogLevel level, object message, string module, int stackSkip)
    {
        var sb = new StringBuilder(256);

        sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");
        sb.Append("[T").Append(Thread.CurrentThread.ManagedThreadId).Append("] ");
        sb.Append('[').Append(level.ToString().ToUpperInvariant()).Append("] ");
        sb.Append('[').Append(string.IsNullOrWhiteSpace(module) ? sDefaultModule : module).Append("] ");
        sb.Append(message ?? "null");

        if (sIncludeStackTrace)
        {
            var stack = new StackTrace(stackSkip, true).ToString();
            if (!string.IsNullOrEmpty(stack))
            {
                sb.Append("\n[StackTrace]\n").Append(stack);
            }
        }

        return sb.ToString();
    }
}
