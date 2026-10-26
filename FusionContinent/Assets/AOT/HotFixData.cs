using System.Collections.Generic;

public class HotFixData
{
    public string ChannelKey { get; set; }
    public string ChannelName { get; set; }
    public int Status { get; set; }
    public string HotfixCdn { get; set; }
    public string HotfixFallbackCdn { get; set; }
    public string UpdateNotice { get; set; }
    public List<ServerInfo> Servers { get; set; }
}

public class ServerInfo
{
    public int ServerId { get; set; }
    public string ServerName { get; set; }
    
    public string Host {get;set;}
    public int Port { get; set; }
    public int Status { get; set; }
}