using UnityEngine;
using System;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine.SceneManagement;

namespace Manager
{
    public class AppLifeCycleManager : MonoBehaviour
    {
        // 最大允许后台时间（单位：秒）
        private int _maxBackgroundTime; // 10分钟

        private DateTime _pauseTime;
        private AndroidJavaClass helper;
        void Awake()
        {
            _maxBackgroundTime = DataConfigManager.Tables.TbGameConst.GameLifeBackgroundTime;
            // _maxBackgroundTime = 60;
            DontDestroyOnLoad(this.gameObject);
            helper = new AndroidJavaClass("game.taptap.kpzz.cardsWar.RestartHelper");
        }

        void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                // 记录进入后台的时间
                _pauseTime = DateTime.Now;
                PlayerPrefs.SetString("PauseTime", _pauseTime.ToBinary().ToString());
                PlayerPrefs.Save();
                helper?.CallStatic("startKillTimer", _maxBackgroundTime);
            }
            else
            {
                helper?.CallStatic("stopKillTimer");
                // 回到前台，检查时间差
                if (PlayerPrefs.HasKey("PauseTime"))
                {
                    long binary = Convert.ToInt64(PlayerPrefs.GetString("PauseTime"));
                    DateTime lastPause = DateTime.FromBinary(binary);
                    double elapsedSeconds = (DateTime.Now - lastPause).TotalSeconds;

                    if (elapsedSeconds > _maxBackgroundTime)
                    {
                        HandleTimeout();
                    }
                }
            }
        }

        private void HandleTimeout()
        {
            LogTool.LogError("后台超时，执行重启逻辑");

        }
        private void ScheduleKill(int seconds)
        {
            if (Application.platform != RuntimePlatform.Android) return;

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                
                    // 1. 创建一个 Intent，目标指向系统的一个 Receiver（或者你自定义的）
                    // 为了省事，我们可以发送一个自定义广播，但这依然需要 Manifest 注册。
                    // 如果你完全不想写 XML，可以尝试启动一个延迟的“重启 Intent”。
                
                    var intent = new AndroidJavaObject("android.content.Intent", "com.unity.ACTION_KILL_ME");
                    var pendingIntent = new AndroidJavaClass("android.app.PendingIntent")
                        .CallStatic<AndroidJavaObject>("getBroadcast", context, 0, intent, 0x08000000); // FLAG_IMMUTABLE

                    var alarmManager = context.Call<AndroidJavaObject>("getSystemService", "alarm");
                    long triggerAt = new AndroidJavaClass("java.lang.System").CallStatic<long>("currentTimeMillis") + (seconds * 1000);

                    // 设置精准闹钟
                    alarmManager.Call("setExact", 0, triggerAt, pendingIntent);
                }
            }
            catch (Exception e) { LogTool.LogError("设置闹钟失败: " + e.Message); }
        }

        private void CancelKill()
        {
            // 逻辑同上，调用 alarmManager.Call("cancel", pendingIntent);
        }
        /// <summary>
        /// 通用方案：重载场景
        /// </summary>
        private void RestartByLoginScene()
        {
            // var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            // UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
            // 切换主界面场景
            if (Global.User != null && Global.User.UserId > 0)
            {
                UserModel.LogOut();
            }
            
            SceneManager.LoadScene("GameStart", LoadSceneMode.Single);
        }

        /// <summary>
        /// 安卓：彻底退出再重启
        /// </summary>
        private void RestartAndroid()
        {
            // 方法一：重新加载场景（最安全）
            RestartByLoginScene();

            // 方法二（可选）：退出进程 + 启动器重启（需要插件支持）
            /*
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("finish"); // 关闭当前 Activity
            }
            Application.Quit();
            */
        }

        /// <summary>
        /// iOS：不能强制退出，只能模拟“重启”
        /// </summary>
        private void RestartIOS()
        {
            // 方法一：重新加载场景（最安全）
            RestartByLoginScene();
            
            // 推荐：回到登录场景，而不是退出
            // UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");

            // ⚠️ 注意：不要调用 Application.Quit()，苹果审核不允许
        }
    }

}