package game.taptap.kpzz.cardsWar;

import android.os.Handler;
import android.os.Looper;
import android.os.Process;
import android.util.Log;

public class RestartHelper {
    private static Handler handler = new Handler(Looper.getMainLooper());
    private static Runnable killRunnable = new Runnable() {
        @Override
        public void run() {
            Log.d("Unity", "后台超过时长，正在强制杀死进程以释放内存");
            Process.killProcess(Process.myPid()); // 彻底杀死当前进程
        }
    };

    public static void startKillTimer(int delaySeconds) {
        // 移除之前的计时，开始新的计时
        handler.removeCallbacks(killRunnable);
        handler.postDelayed(killRunnable, delaySeconds * 1000);
    }

    public static void stopKillTimer() {
        // 用户回来了，取消计时
        handler.removeCallbacks(killRunnable);
    }
}