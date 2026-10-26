# 音频管理器使用指南（Unity 原生版本）

## 概述

新的音频管理器 `AudioManager` 使用 **Unity 原生 AudioSource**，完全替换了旧的 `SoundManager` 和 QFramework 的 `AudioPlayer`。

**特点**：
- ✅ **不依赖 QFramework** - 使用 Unity 原生 AudioSource
- ✅ **不依赖 UIKit** - 独立单例模式
- ✅ **BGM过渡效果** - 可配置的淡入淡出
- ✅ **动态音效混合** - 快照系统支持多种场景切换
- ✅ **音效异步加载** - 使用 YooAssets 异步加载
- ✅ **音效池管理** - 对象池避免频繁创建销毁
- ✅ **设置持久化** - PlayerPrefs 保存用户设置

## 文件结构

```
Assets/Scripts/Game/Audio/
├── AudioManager.cs              # 主管理器（单例）
├── Managers/
│   ├── BGMAudioManager.cs       # BGM管理器
│   ├── SFXAudioManager.cs       # 音效管理器
│   ├── VoiceAudioManager.cs     # 语音管理器
│   └── AmbientAudioManager.cs   # 环境音管理器
├── Core/
│   ├── AudioPoolManager.cs      # 对象池（使用 AudioSource）
│   └── AudioMixer.cs            # 动态混合器
├── Data/
│   └── AudioSettings.cs         # 设置数据
└── Extensions/
    └── AudioExtensions.cs       # 扩展方法
```

## 核心组件

### PooledAudioSource

使用 Unity 原生 `AudioSource` 的池化组件：

```csharp
public class PooledAudioSource : MonoBehaviour
{
    public void SetClip(AudioClip clip);
    public void SetVolume(float volume);
    public void SetPitch(float pitch);
    public void SetLoop(bool loop);
    public void Play();
    public void Stop();
    public void Pause();
    public void Resume();
    
    public bool IsPlaying { get; }
    public float Volume { get; }
}
```

## 基本使用

### 1. 获取音频管理器

```csharp
// 通过单例访问（自动创建）
var audio = AudioManager.Instance;
```

### 2. 播放BGM

```csharp
// 通过 SoundType 播放（带过渡效果）
AudioManager.Instance.BGM.Play(SoundType.SoundMainBGM);

// 通过名称播放
AudioManager.Instance.BGM.Play("BGM_MainTheme", volume: 0.8f, speed: 1f, useTransition: true);

// 停止BGM（带过渡）
AudioManager.Instance.BGM.Stop(useTransition: true);

// 暂停/恢复
AudioManager.Instance.BGM.Pause();
AudioManager.Instance.BGM.Resume();
```

### 3. 播放音效

```csharp
// 通过 SoundType 播放
AudioManager.Instance.SFX.Play(SoundType.SoundButtonClick);

// 通过名称播放
AudioManager.Instance.SFX.Play("SFX_ButtonClick", volume: 1f, pitch: 1f);

// 停止所有音效
AudioManager.Instance.SFX.StopAll();
```

### 4. 播放语音

```csharp
// 通过 SoundType 播放
AudioManager.Instance.Voice.Play(SoundType.SoundBattleVictory);

// 在游戏中播放（自动处理延迟和速度）
AudioManager.Instance.Voice.PlayInGame(11001, speed: 1f);

// 播放语音（不可打断）
AudioManager.Instance.Voice.Play(SoundType.SoundDialogue, canInterrupt: false);
```

### 5. 环境音

```csharp
// 添加环境音层
AudioManager.Instance.Ambient.AddLayer("Ambient_Wind", volume: 0.5f, loop: true);
AudioManager.Instance.Ambient.AddLayer("Ambient_Rain", volume: 0.3f, loop: true);

// 移除环境音层
AudioManager.Instance.Ambient.RemoveLayer("Ambient_Wind");

// 停止所有环境音
AudioManager.Instance.Ambient.StopAll();
```

### 6. 动态音效混合

```csharp
// 切换到战斗快照（自动降低BGM音量）
AudioManager.Instance.Mixer.TransitionTo(AudioMixer.SnapshotType.Battle);

// 切换到对话快照（静音SFX）
AudioManager.Instance.Mixer.TransitionTo(AudioMixer.SnapshotType.Dialogue, customTransitionTime: 0.5f);

// 立即切换（无过渡）
AudioManager.Instance.Mixer.SetSnapshotImmediate(AudioMixer.SnapshotType.Normal);
```

### 7. 音量控制

```csharp
// 设置主音量
AudioManager.Instance.Settings.MasterVolume.Value = 0.8f;

// 设置分类音量
AudioManager.Instance.Settings.BGMVolume.Value = 0.6f;
AudioManager.Instance.Settings.SFXVolume.Value = 1f;
AudioManager.Instance.Settings.VoiceVolume.Value = 0.8f;
AudioManager.Instance.Settings.AmbientVolume.Value = 0.5f;

// 静音
AudioManager.Instance.Settings.MasterMute.Value = true;

// 保存设置
AudioManager.Instance.SaveSettings();
```

### 8. 预加载

```csharp
// 预加载音效列表
await AudioManager.Instance.SFX.PreloadAsync(new List<string>
{
    "SFX_Attack_01",
    "SFX_Attack_02",
    "SFX_Hit_01"
});

// 预加载 SoundType 列表
await AudioManager.Instance.SFX.PreloadAsync(new List<SoundType>
{
    SoundType.SoundButtonClick,
    SoundType.SoundAttack
});
```

## 兼容旧接口

为了兼容旧代码，AudioManager 提供了与 SoundManager 相同的接口：

```csharp
// 旧代码
AudioManager.Instance.PlayBgm(SoundType.SoundMainBGM);
AudioManager.Instance.PlayVoice(SoundType.SoundBattleVictory);
AudioManager.Instance.PlayVoiceInGame(11001);

// 新代码（兼容接口）
AudioManager.Instance.PlayBgm(SoundType.SoundMainBGM);
AudioManager.Instance.PlayVoice(SoundType.SoundBattleVictory);
AudioManager.Instance.PlayVoiceInGame(11001);
```

## 扩展方法（更便捷的使用方式）

```csharp
using Game.Audio;

// 播放BGM
SoundType.SoundMainBGM.PlayBGM();

// 播放音效
SoundType.SoundButtonClick.PlaySFX();

// 播放语音
SoundType.SoundBattleVictory.PlayVoice();
```

## 事件订阅

```csharp
var audio = AudioManager.Instance;

// 音量变化事件
audio.OnMasterVolumeChanged += (volume) => Debug.Log($"主音量: {volume}");
audio.OnBGMVolumeChanged += (volume) => Debug.Log($"BGM音量: {volume}");
audio.OnSFXVolumeChanged += (volume) => Debug.Log($"音效音量: {volume}");
audio.OnVoiceVolumeChanged += (volume) => Debug.Log($"语音音量: {volume}");

// 静音变化事件
audio.OnMuteChanged += (isMute) => Debug.Log($"静音: {isMute}");
```

## 配置说明

### BGM 过渡配置

```csharp
// 过渡时间（秒）
AudioManager.Instance.BGM.Transition.Duration = 1f;

// 过渡曲线
AudioManager.Instance.BGM.Transition.Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
```

### 对象池配置

```csharp
// 默认池大小
AudioManager.Instance.Pool.DefaultPoolSize = 5;

// 最大池大小
AudioManager.Instance.Pool.MaxPoolSize = 20;

// 自动释放时间（秒）
AudioManager.Instance.Pool.AutoReleaseTime = 30f;
```

### 混合器配置

```csharp
// 默认过渡时间（秒）
AudioManager.Instance.Mixer.TransitionTime = 1f;
```

## 最佳实践

1. **BGM 播放**：在场景切换时播放对应的 BGM，使用过渡效果
2. **音效播放**：频繁播放的音效（如攻击音效）会自动使用对象池
3. **语音播放**：重要语音设置 `canInterrupt = false`，避免被其他语音打断
4. **预加载**：在场景加载时预加载该场景需要的音效
5. **快照切换**：进入战斗时切换到 Battle 快照，退出战斗时切换回 Normal
6. **设置保存**：在设置面板修改音量后调用 `SaveSettings()` 保存

## 注意事项

1. 音频管理器使用单例模式，首次访问 `AudioManager.Instance` 时自动创建
2. 所有音频资源路径通过 `TbGameSoundConfig` 配置表获取
3. 设置数据自动保存到 PlayerPrefs，键名为 `GameAudioSettings_v1`
4. 对象池会自动清理长时间未使用的音频资源
5. 音频管理器在场景切换时不会被销毁（使用了 `DontDestroyOnLoad`）
6. **不再使用 QFramework 的 AudioPlayer**，改用 Unity 原生 AudioSource

## 与旧版本的对比

| 特性 | 旧版本（QFramework） | 新版本（Unity原生） |
|------|---------------------|-------------------|
| 音频组件 | AudioPlayer (QFramework) | AudioSource (Unity) |
| 依赖 | QFramework AudioKit | 无外部依赖 |
| 对象池 | SafeObjectPool | 自定义 Pool |
| 资源加载 | AudioKit | YooAssets |
| 性能 | 中等 | 更好（原生组件） |
| 可控性 | 受限 | 完全可控 |

## 迁移指南

### 从 SoundManager 迁移

```csharp
// 旧代码
AudioManager.Instance.PlayBgm(SoundType.SoundMainBGM);
AudioManager.Instance.PlayVoice(SoundType.SoundBattleVictory);

// 新代码（直接替换）
AudioManager.Instance.PlayBgm(SoundType.SoundMainBGM);
AudioManager.Instance.PlayVoice(SoundType.SoundBattleVictory);
```

### 从 QFramework AudioKit 迁移

```csharp
// 旧代码
AudioKit.PlayMusic("BGM_MainTheme");
AudioKit.PlaySound("SFX_Click");
AudioKit.PlayVoice("Voice_01");

// 新代码
AudioManager.Instance.BGM.Play("BGM_MainTheme");
AudioManager.Instance.SFX.Play("SFX_Click");
AudioManager.Instance.Voice.Play("Voice_01");
```

## 技术细节

### 对象池工作原理

1. **获取音频源**：从对象池获取 `PooledAudioSource`（包含 AudioSource）
2. **设置参数**：设置 AudioClip、Volume、Pitch、Loop 等参数
3. **播放**：调用 AudioSource.Play()
4. **自动回收**：非循环音频播放完成后自动归还对象池

### BGM 过渡实现

1. 创建两个 AudioSource（旧BGM和新BGM）
2. 同时播放，旧BGM音量从1降到0，新BGM音量从0升到1
3. 过渡完成后停止旧BGM并归还对象池

### 快照混合实现

1. 定义不同场景的音量配置（Normal/Battle/Dialogue等）
2. 使用协程平滑过渡音量值
3. 音量变化通过事件通知各管理器更新
