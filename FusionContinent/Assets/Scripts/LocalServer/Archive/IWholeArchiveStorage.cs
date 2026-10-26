namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 整档存储抽象：存档（User + PlayerArchive）整体序列化为单个 JSON 文件，读写全量，不分块。
    /// 实现：WxArchiveStorage（微信小游戏，WXFileSystemManager 直写 wx.env.USER_DATA_PATH，绕过 MEMFS）；
    /// FileArchiveStorage（Editor/Android，System.IO 写 persistentDataPath/save_archive.json，.tmp 原子替换）。
    /// </summary>
    public interface IWholeArchiveStorage
    {
        /// <summary>是否已存在存档。</summary>
        bool HasArchive { get; }

        /// <summary>读取整档字节；无存档时返回 null。</summary>
        byte[] ReadArchive();

        /// <summary>写入整档字节（全量覆盖）。失败应抛异常。</summary>
        void WriteArchive(byte[] data);

        /// <summary>删除存档（读档损坏自愈清档用）。</summary>
        void DeleteAll();
    }
}
