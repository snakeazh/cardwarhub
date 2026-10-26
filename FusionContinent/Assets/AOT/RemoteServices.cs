using YooAsset;

// 在 RemoteServices 中修改，将 Referer 作为 URL 参数传递
// 虽然不如 Header 正规，但七牛云日志会记录，能增加盗链难度
public class RemoteServices : IRemoteServices
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;
    private readonly string _referer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer, string referer = null)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
        _referer = referer;
    }

    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_defaultHostServer}/{fileName}";
    }
    
    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackHostServer}/{fileName}";
    }
}