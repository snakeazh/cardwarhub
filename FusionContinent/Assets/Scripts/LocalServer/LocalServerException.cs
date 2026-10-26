namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 业务校验失败异常，等价于 Go 服务端的 panic（消息原文复刻 Go 文案）。
    /// LocalApi 捕获后转为 GameResponse{Code=500, Msg=Message}，客户端按原逻辑 Toast。
    /// </summary>
    public class LocalServerException : System.Exception
    {
        public LocalServerException(string message) : base(message)
        {
        }
    }
}
