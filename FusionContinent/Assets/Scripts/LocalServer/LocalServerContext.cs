namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 本地服务端会话上下文：权威存档 + 公共服务（道具入口 / 战力 / 随机源）。
    /// 生命周期 = 一次游戏会话，由 Global.LocalServer 持有，登录时读档或建档。
    /// </summary>
    public class LocalServerContext
    {
        public ArchiveStore Store { get; private set; }
        public LocalRng Rng { get; private set; }
        public ItemEntrance ItemEntrance { get; private set; }
        public TaskEngine TaskEngine { get; private set; }
        public ArcCardUpgradeHelper ArcCardUpgrade { get; private set; }
        public CombatPowerRefresher CombatPower { get; private set; }

        /// <summary>当前玩家 Id（单机恒为存档中的 UserId）。</summary>
        public int UserId => Store.User.UserId;

        /// <summary>当前玩家等级，对照 Go model.GetUserLv。</summary>
        public int UserLv => Store.User.Level;

        public LocalServerContext()
        {
            // 各平台统一整档单 JSON 方案（IWholeArchiveStorage），仅写入途径不同：
            // WebGL（微信小游戏）下 System.IO 写 MEMFS 再异步回写，devtools 重编译杀进程会丢未落盘数据，
            // 故走 WXFileSystemManager 直写 wx.env.USER_DATA_PATH；其余平台 System.IO 直写 persistentDataPath。
#if UNITY_WEBGL && !UNITY_EDITOR
            Store = new ArchiveStore(new WxArchiveStorage());
#else
            Store = new ArchiveStore(new FileArchiveStorage(
                System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "save_archive.json")));
#endif
            Rng = new LocalRng();
            ItemEntrance = new ItemEntrance(this);
            TaskEngine = new TaskEngine(this);
            ArcCardUpgrade = new ArcCardUpgradeHelper();
            CombatPower = new CombatPowerRefresher(this);
        }

        /// <summary>读档；无存档返回 false（由 login Handler 走新档初始化）。</summary>
        public bool LoadArchive()
        {
            try
            {
                return Store.Load();
            }
            catch (System.Exception e)
            {
                // 存档损坏（缺块/CRC 失败）：本地单机存档无法修复，清档重开，避免玩家永远卡登录
                LogTool.LogError($"[LocalServerContext] 读档失败，清除损坏存档并重新建档: {e.Message}");
                Store.DeleteAll();
                return false;
            }
        }
    }
}
