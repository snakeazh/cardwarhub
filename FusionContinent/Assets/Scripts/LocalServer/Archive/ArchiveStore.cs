using Cardwar;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 内存权威存档（扮演原 MySQL）：PlayerArchive + User（等级/战力/账号在服务端是独立的 user 表）。
    /// Handler 直接读写本对象；改动后 MarkDirty 标记，由 LocalApi 在请求收尾时统一 Flush 整档落盘。
    /// 持久化为整档单 JSON 文件（IWholeArchiveStorage），不分块：Flush 全量覆盖写。
    /// </summary>
    public class ArchiveStore
    {
        public User User { get; private set; }
        public PlayerArchive Archive { get; private set; }

        /// <summary>是否已加载/新建存档。</summary>
        public bool Loaded { get; private set; }

        private readonly IWholeArchiveStorage _storage;

        /// <summary>是否有未落盘改动。整档存储下 MarkDirty 只起"有改动"开关作用，Flush 恒为全量写。</summary>
        private bool _dirty;

        public ArchiveStore(IWholeArchiveStorage storage)
        {
            _storage = storage;
        }

        /// <summary>从持久层读档；无存档返回 false，由 login Handler 走新档初始化。</summary>
        public bool Load()
        {
            if (!_storage.HasArchive)
            {
                return false;
            }

            byte[] bytes = _storage.ReadArchive();
            var jobj = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            User = JsonParser.Default.Parse<User>(jobj["user"].ToString());
            Archive = JsonParser.Default.Parse<PlayerArchive>(jobj["archive"].ToString());
            Loaded = true;
            LogTool.Log("[ArchiveStore] 读档成功");
            return true;
        }

        /// <summary>挂上新档（由 NewPlayerInitializer 填充），并整体标脏等待落盘。</summary>
        public void AttachNewArchive(User user, PlayerArchive archive)
        {
            User = user;
            Archive = archive;
            Loaded = true;
            MarkAllDirty();
        }

        /// <summary>标记存档有改动。参数为 PlayerArchive 顶层字段号，仅保留作 Handler 调用处的语义说明。</summary>
        public void MarkDirty(int archiveFieldNumber)
        {
            _dirty = true;
        }

        public void MarkUserDirty()
        {
            _dirty = true;
        }

        public void MarkAllDirty()
        {
            _dirty = true;
        }

        /// <summary>清除持久层全部存档（读档发现存档损坏时自愈用）。</summary>
        public void DeleteAll()
        {
            _storage.DeleteAll();
            _dirty = false;
        }

        /// <summary>把存档整档写入持久层。请求成功收尾时调用；无改动直接返回。</summary>
        public void Flush()
        {
            if (!_dirty)
            {
                return;
            }

            _storage.WriteArchive(SerializeWhole());
            _dirty = false;
        }

        /// <summary>
        /// 事务等价物：请求前的存档快照（Clone 内存复制，比全量序列化便宜且无字节垃圾）。
        /// Go 端靠 MySQL 事务回滚；单机版在 Handler 抛错时由 LocalApi 调 Restore 还原内存存档。
        /// </summary>
        public (User user, PlayerArchive archive) Snapshot()
        {
            return (User.Clone(), Archive.Clone());
        }

        /// <summary>恢复快照：整体替换内存存档并清脏标记（磁盘本就未写入，无需回滚文件）。</summary>
        public void Restore((User user, PlayerArchive archive) snapshot)
        {
            User = snapshot.user;
            Archive = snapshot.archive;
            _dirty = false;
        }

        private byte[] SerializeWhole()
        {
            // JsonFormatter 输出本身即合法 JSON，直接拼接，省去 JObject 中转的二次解析+序列化
            string json = "{\"user\":" + JsonFormatter.Default.Format(User)
                        + ",\"archive\":" + JsonFormatter.Default.Format(Archive) + "}";
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
