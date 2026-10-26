#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using WeChatWASM;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 微信小游戏存档实现：整个存档（User + PlayerArchive）序列化为单个 JSON 文件，
    /// 直写 wx.env.USER_DATA_PATH，不经过 MEMFS（System.IO 在 WebGL 下先写内存文件系统再
    /// 异步回写，开发者工具重编译杀进程会丢未回写数据）。
    /// 单文件无目录（当前 SDK 的 MkdirSync 递归创建有坑）；写坏/读坏由上层清档重建。
    /// </summary>
    public class WxArchiveStorage : IWholeArchiveStorage
    {
        private readonly string _filePath = WX.env.USER_DATA_PATH + "/save_archive.json";
        private readonly WXFileSystemManager _fs = WX.GetFileSystemManager();

        public bool HasArchive
        {
            get { return _fs.AccessSync(_filePath) == "access:ok"; }
        }

        public byte[] ReadArchive()
        {
            return _fs.ReadFileSync(_filePath, null, null);
        }

        public void WriteArchive(byte[] data)
        {
            string result = _fs.WriteFileSync(_filePath, data, null);
            if (result != "ok")
            {
                throw new Exception($"[WxArchiveStorage] 写入失败: {_filePath}; {result}");
            }
        }

        public void DeleteAll()
        {
            if (HasArchive)
            {
                _fs.UnlinkSync(_filePath);
            }

            // 清掉分块存储时代遗留的 save_slot 目录
            string legacyDir = WX.env.USER_DATA_PATH + "/save_slot";
            if (_fs.AccessSync(legacyDir) == "access:ok")
            {
                _fs.RmdirSync(legacyDir, true);
            }
        }
    }
}
#endif
