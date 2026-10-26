using System;
using System.IO;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 文件存档实现（Editor/Android）：整个存档序列化为单个 JSON 文件，
    /// 路径由构造方注入（persistentDataPath/save_archive.json）。
    /// 与微信小游戏（WxArchiveStorage）同为整档单文件方案，这里 System.IO 直写即持久化，
    /// 写入先落 .tmp 再替换，避免半截文件；写坏/读坏由上层清档重建。
    /// </summary>
    public class FileArchiveStorage : IWholeArchiveStorage
    {
        private readonly string _filePath;

        public FileArchiveStorage(string filePath)
        {
            _filePath = filePath;
        }

        public bool HasArchive
        {
            get { return File.Exists(_filePath); }
        }

        public byte[] ReadArchive()
        {
            return File.ReadAllBytes(_filePath);
        }

        public void WriteArchive(byte[] data)
        {
            string tmp = _filePath + ".tmp";
            File.WriteAllBytes(tmp, data);
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
            File.Move(tmp, _filePath);
        }

        public void DeleteAll()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            // 清掉分块存储时代遗留的 save_slot 目录
            string legacyDir = Path.Combine(Path.GetDirectoryName(_filePath), "save_slot");
            if (Directory.Exists(legacyDir))
            {
                Directory.Delete(legacyDir, true);
            }
        }
    }
}
