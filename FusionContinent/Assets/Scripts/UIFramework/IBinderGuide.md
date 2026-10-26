# IBinder 使用指南

`IBinder` 接口用于实现**自定义绑定逻辑**，将任意逻辑（音效、特效、动画等）与 ViewModel 绑定。

---

## 接口定义

```csharp
namespace UIFramework
{
    /// <summary>
    /// 绑定器接口 - 用于自定义绑定逻辑
    /// </summary>
    public interface IBinder
    {
        /// <summary>
        /// 绑定到 ViewModel
        /// </summary>
        void Bind(ViewModel viewModel);
        
        /// <summary>
        /// 解绑
        /// </summary>
        void Unbind();
    }
}
```

---

## 使用场景 1：音效绑定器

当数据变化时自动播放音效。

### 实现

```csharp
using UnityEngine;
using UIFramework;

/// <summary>
/// 音效绑定器
/// 当 ViewModel 数据变化时自动播放对应音效
/// </summary>
public class SoundBinder : IBinder
{
    private AudioSource mAudioSource;
    private ViewModel mViewModel;
    
    // 音效配置
    private AudioClip mCoinSound;
    private AudioClip mDamageSound;
    private AudioClip mLevelUpSound;
    
    public SoundBinder(AudioSource audioSource)
    {
        mAudioSource = audioSource;
        
        // 加载音效资源
        mCoinSound = Resources.Load<AudioClip>("Audio/Coin");
        mDamageSound = Resources.Load<AudioClip>("Audio/Damage");
        mLevelUpSound = Resources.Load<AudioClip>("Audio/LevelUp");
    }
    
    public void Bind(ViewModel viewModel)
    {
        mViewModel = viewModel;
        
        if (viewModel is PlayerViewModel vm)
        {
            // 金币变化时播放金币音效
            vm.Gold.OnValueChanged += OnGoldChanged;
            
            // 血量减少时播放受伤音效
            vm.HP.OnValueChanged += OnHPChanged;
            
            // 升级时播放升级音效
            vm.Level.OnValueChanged += OnLevelChanged;
        }
    }
    
    public void Unbind()
    {
        if (mViewModel is PlayerViewModel vm)
        {
            vm.Gold.OnValueChanged -= OnGoldChanged;
            vm.HP.OnValueChanged -= OnHPChanged;
            vm.Level.OnValueChanged -= OnLevelChanged;
        }
        
        mViewModel = null;
    }
    
    private void OnGoldChanged(int oldVal, int newVal)
    {
        if (newVal > oldVal && mCoinSound != null)
        {
            mAudioSource.PlayOneShot(mCoinSound);
            Debug.Log("[SoundBinder] 播放金币音效");
        }
    }
    
    private void OnHPChanged(int oldVal, int newVal)
    {
        if (newVal < oldVal && mDamageSound != null)
        {
            mAudioSource.PlayOneShot(mDamageSound);
            Debug.Log("[SoundBinder] 播放受伤音效");
        }
    }
    
    private void OnLevelChanged(int oldVal, int newVal)
    {
        if (newVal > oldVal && mLevelUpSound != null)
        {
            mAudioSource.PlayOneShot(mLevelUpSound);
            Debug.Log("[SoundBinder] 播放升级音效");
        }
    }
}
```

### 使用

```csharp
public class PlayerPanel : UIPanel<PlayerViewModel>
{
    public Text NameText;
    public Text GoldText;
    public AudioSource AudioSource;  // 音效播放器
    
    private SoundBinder mSoundBinder;
    
    protected override void OnBindViewModel(PlayerViewModel vm)
    {
        // 基本 UI 绑定
        UIBind.Text(NameText, vm.PlayerName);
        UIBind.Text(GoldText, vm.Gold);
        
        // 添加音效绑定器
        mSoundBinder = new SoundBinder(AudioSource);
        mSoundBinder.Bind(vm);
    }
    
    protected override void OnUnbindViewModel(PlayerViewModel vm)
    {
        // 必须解绑音效
        mSoundBinder?.Unbind();
    }
}
```

---

## 使用场景 2：动画绑定器

数据变化时自动播放动画（DOTween、Animator 等）。

### 实现

```csharp
using UnityEngine;
using UIFramework;
using DG.Tweening;  // DOTween

/// <summary>
/// DOTween 动画绑定器
/// </summary>
public class TweenBinder : IBinder
{
    private Transform mTarget;
    private ViewModel mViewModel;
    private Sequence mCurrentSequence;
    
    public TweenBinder(Transform target)
    {
        mTarget = target;
    }
    
    public void Bind(ViewModel viewModel)
    {
        mViewModel = viewModel;
        
        if (viewModel is PlayerViewModel vm)
        {
            // 受伤时抖动
            vm.HP.OnValueChanged += OnHPChanged;
            
            // 升级时缩放弹跳
            vm.Level.OnValueChanged += OnLevelChanged;
            
            // 获得金币时弹跳
            vm.Gold.OnValueChanged += OnGoldChanged;
        }
    }
    
    public void Unbind()
    {
        // 停止所有动画
        mCurrentSequence?.Kill();
        mTarget?.DOKill();
        
        if (mViewModel is PlayerViewModel vm)
        {
            vm.HP.OnValueChanged -= OnHPChanged;
            vm.Level.OnValueChanged -= OnLevelChanged;
            vm.Gold.OnValueChanged -= OnGoldChanged;
        }
        
        mViewModel = null;
    }
    
    private void OnHPChanged(int oldVal, int newVal)
    {
        if (newVal < oldVal)
        {
            // 受伤抖动效果
            mTarget.DOShakePosition(0.5f, new Vector3(10f, 0, 0), 20, 90);
            
            // 变红闪烁
            var image = mTarget.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.DOColor(Color.red, 0.1f)
                     .SetLoops(2, LoopType.Yoyo)
                     .OnComplete(() => image.color = Color.white);
            }
        }
    }
    
    private void OnLevelChanged(int oldVal, int newVal)
    {
        if (newVal > oldVal)
        {
            // 升级弹跳效果
            mCurrentSequence = DOTween.Sequence();
            mCurrentSequence.Append(mTarget.DOScale(1.5f, 0.2f))
                           .Append(mTarget.DOScale(1f, 0.3f).SetEase(Ease.OutBounce));
        }
    }
    
    private void OnGoldChanged(int oldVal, int newVal)
    {
        if (newVal > oldVal)
        {
            // 金币图标弹跳
            mTarget.DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 1);
        }
    }
}
```

### 使用

```csharp
public class HUDPanel : UIPanel<PlayerViewModel>
{
    public Transform AvatarTransform;      // 头像
    public Transform GoldIconTransform;    // 金币图标
    public Slider HpSlider;
    
    private TweenBinder mAvatarTweenBinder;
    private TweenBinder mGoldTweenBinder;
    
    protected override void OnBindViewModel(PlayerViewModel vm)
    {
        // 基本绑定
        UIBind.Slider(HpSlider, vm.HpPercent);
        
        // 头像动画绑定
        mAvatarTweenBinder = new TweenBinder(AvatarTransform);
        mAvatarTweenBinder.Bind(vm);
        
        // 金币图标动画绑定
        mGoldTweenBinder = new TweenBinder(GoldIconTransform);
        mGoldTweenBinder.Bind(vm);
    }
    
    protected override void OnUnbindViewModel(PlayerViewModel vm)
    {
        mAvatarTweenBinder?.Unbind();
        mGoldTweenBinder?.Unbind();
    }
}
```

---

## 使用场景 3：列表自动刷新绑定器

封装复杂的列表管理逻辑。

### 实现

```csharp
using UnityEngine;
using UIFramework;
using System.Collections.Generic;

/// <summary>
/// 列表自动刷新绑定器
/// 自动处理列表的清空、重建、更新
/// </summary>
public class ListBinder<TData> : IBinder
{
    private Transform mContainer;
    private GameObject mItemPrefab;
    private BindableProperty<List<TData>> mProperty;
    private System.Action<GameObject, TData> mOnBindItem;
    private List<GameObject> mItemObjects = new List<GameObject>();
    
    public ListBinder(
        Transform container, 
        GameObject itemPrefab,
        System.Action<GameObject, TData> onBindItem)
    {
        mContainer = container;
        mItemPrefab = itemPrefab;
        mOnBindItem = onBindItem;
    }
    
    public void Bind(ViewModel viewModel)
    {
        // 查找对应的属性
        var propertyInfo = viewModel.GetType().GetProperty("Items");
        if (propertyInfo != null)
        {
            mProperty = propertyInfo.GetValue(viewModel) as BindableProperty<List<TData>>;
            if (mProperty != null)
            {
                mProperty.OnValueChanged += OnListChanged;
                // 初始刷新
                RefreshList(mProperty.Value);
            }
        }
    }
    
    public void Unbind()
    {
        if (mProperty != null)
        {
            mProperty.OnValueChanged -= OnListChanged;
        }
        
        // 清理所有子对象
        ClearItems();
    }
    
    private void OnListChanged(List<TData> oldVal, List<TData> newVal)
    {
        RefreshList(newVal);
    }
    
    private void RefreshList(List<TData> items)
    {
        if (items == null) return;
        
        // 清空旧项
        ClearItems();
        
        // 创建新项
        foreach (var item in items)
        {
            var obj = Object.Instantiate(mItemPrefab, mContainer);
            mOnBindItem?.Invoke(obj, item);
            mItemObjects.Add(obj);
        }
        
        Debug.Log($"[ListBinder] 刷新列表，共 {items.Count} 项");
    }
    
    private void ClearItems()
    {
        foreach (var obj in mItemObjects)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
        mItemObjects.Clear();
    }
}
```

### 使用

```csharp
public class ShopPanel : UIPanel<ShopViewModel>
{
    public Transform ItemContainer;
    public GameObject ItemPrefab;
    
    private ListBinder<ShopItem> mListBinder;
    
    protected override void OnBindViewModel(ShopViewModel vm)
    {
        // 创建列表绑定器
        mListBinder = new ListBinder<ShopItem>(
            ItemContainer,
            ItemPrefab,
            (obj, item) =>
            {
                // 绑定单个项
                var slot = obj.GetComponent<ShopItemSlot>();
                slot.Setup(item, vm.BuyCommand);
            }
        );
        
        mListBinder.Bind(vm);
    }
    
    protected override void OnUnbindViewModel(ShopViewModel vm)
    {
        mListBinder?.Unbind();
    }
}
```

---

## 使用场景 4：粒子特效绑定器

数据变化时触发粒子特效。

### 实现

```csharp
using UnityEngine;
using UIFramework;

/// <summary>
/// 粒子特效绑定器
/// </summary>
public class ParticleBinder : IBinder
{
    private Transform mEffectParent;
    private ViewModel mViewModel;
    
    // 特效资源
    private GameObject mCoinEffectPrefab;
    private GameObject mLevelUpEffectPrefab;
    private GameObject mDamageEffectPrefab;
    
    public ParticleBinder(Transform effectParent)
    {
        mEffectParent = effectParent;
        
        // 加载特效
        mCoinEffectPrefab = Resources.Load<GameObject>("Effects/CoinEffect");
        mLevelUpEffectPrefab = Resources.Load<GameObject>("Effects/LevelUpEffect");
        mDamageEffectPrefab = Resources.Load<GameObject>("Effects/DamageEffect");
    }
    
    public void Bind(ViewModel viewModel)
    {
        mViewModel = viewModel;
        
        if (viewModel is PlayerViewModel vm)
        {
            vm.Gold.OnValueChanged += OnGoldChanged;
            vm.Level.OnValueChanged += OnLevelChanged;
            vm.HP.OnValueChanged += OnHPChanged;
        }
    }
    
    public void Unbind()
    {
        if (mViewModel is PlayerViewModel vm)
        {
            vm.Gold.OnValueChanged -= OnGoldChanged;
            vm.Level.OnValueChanged -= OnLevelChanged;
            vm.HP.OnValueChanged -= OnHPChanged;
        }
        
        mViewModel = null;
    }
    
    private void OnGoldChanged(int oldVal, int newVal)
    {
        if (newVal > oldVal && mCoinEffectPrefab != null)
        {
            SpawnEffect(mCoinEffectPrefab);
        }
    }
    
    private void OnLevelChanged(int oldVal, int newVal)
    {
        if (newVal > oldVal && mLevelUpEffectPrefab != null)
        {
            SpawnEffect(mLevelUpEffectPrefab, 3f);  // 持续3秒
        }
    }
    
    private void OnHPChanged(int oldVal, int newVal)
    {
        if (newVal < oldVal && mDamageEffectPrefab != null)
        {
            SpawnEffect(mDamageEffectPrefab);
        }
    }
    
    private void SpawnEffect(GameObject prefab, float duration = 1f)
    {
        var effect = Object.Instantiate(prefab, mEffectParent);
        
        // 自动销毁
        Object.Destroy(effect, duration);
    }
}
```

---

## 使用场景 5：多 Binder 管理

在 Panel 中管理多个 Binder。

### 完整示例

```csharp
using UnityEngine;
using UIFramework;
using System.Collections.Generic;

public class PlayerPanel : UIPanel<PlayerViewModel>
{
    [Header("UI组件")]
    public Text NameText;
    public Text GoldText;
    public Text LevelText;
    public Slider HpSlider;
    public Transform AvatarTransform;
    public Transform EffectParent;
    public AudioSource AudioSource;
    
    // 自定义 Binder 列表
    private List<IBinder> mCustomBinders = new List<IBinder>();
    
    protected override void OnBindViewModel(PlayerViewModel vm)
    {
        // 1. 基础 UI 绑定
        UIBind.Text(NameText, vm.PlayerName);
        UIBind.Text(GoldText, vm.Gold, "金币: {0}");
        UIBind.Text(LevelText, vm.Level, "Lv.{0}");
        UIBind.Slider(HpSlider, vm.HpPercent);
        
        // 2. 音效绑定器
        var soundBinder = new SoundBinder(AudioSource);
        soundBinder.Bind(vm);
        mCustomBinders.Add(soundBinder);
        
        // 3. 动画绑定器
        var tweenBinder = new TweenBinder(AvatarTransform);
        tweenBinder.Bind(vm);
        mCustomBinders.Add(tweenBinder);
        
        // 4. 特效绑定器
        var particleBinder = new ParticleBinder(EffectParent);
        particleBinder.Bind(vm);
        mCustomBinders.Add(particleBinder);
        
        Debug.Log($"[PlayerPanel] 已绑定 {mCustomBinders.Count} 个自定义 Binder");
    }
    
    protected override void OnUnbindViewModel(PlayerViewModel vm)
    {
        // 统一清理所有 Binder
        foreach (var binder in mCustomBinders)
        {
            binder?.Unbind();
        }
        mCustomBinders.Clear();
        
        Debug.Log("[PlayerPanel] 已清理所有 Binder");
    }
}
```

---

## 创建自定义 Binder 的步骤

```csharp
// Step 1: 实现 IBinder 接口
public class MyBinder : IBinder
{
    // 需要的依赖
    private MyDependency mDependency;
    private ViewModel mViewModel;
    
    public MyBinder(MyDependency dependency)
    {
        mDependency = dependency;
    }
    
    // Step 2: 实现 Bind 方法
    public void Bind(ViewModel viewModel)
    {
        mViewModel = viewModel;
        
        // 订阅 ViewModel 属性变化
        if (viewModel is MyViewModel vm)
        {
            vm.SomeProperty.OnValueChanged += OnPropertyChanged;
        }
    }
    
    // Step 3: 实现 Unbind 方法
    public void Unbind()
    {
        // 必须取消订阅！
        if (mViewModel is MyViewModel vm)
        {
            vm.SomeProperty.OnValueChanged -= OnPropertyChanged;
        }
        
        mViewModel = null;
    }
    
    // Step 4: 实现业务逻辑
    private void OnPropertyChanged(T oldVal, T newVal)
    {
        // 执行自定义逻辑
    }
}

// Step 5: 在 Panel 中使用
public class MyPanel : UIPanel<MyViewModel>
{
    private MyBinder mBinder;
    
    protected override void OnBindViewModel(MyViewModel vm)
    {
        mBinder = new MyBinder(dependency);
        mBinder.Bind(vm);
    }
    
    protected override void OnUnbindViewModel(MyViewModel vm)
    {
        mBinder?.Unbind();
    }
}
```

---

## 注意事项

### 1. 必须实现 Unbind

```csharp
public void Unbind()
{
    // ❌ 错误：不取消订阅会导致内存泄漏
    
    // ✅ 正确：取消所有订阅
    if (mViewModel is PlayerViewModel vm)
    {
        vm.Property.OnValueChanged -= Handler;
    }
}
```

### 2. 避免在 Binder 中持有 View 引用

```csharp
// ❌ 错误：Binder 应该独立，不依赖特定 View
public class BadBinder : IBinder
{
    private Text mText;  // 不要这样
}

// ✅ 正确：通过事件或回调与 View 通信
public class GoodBinder : IBinder
{
    private System.Action<string> mOnTextChanged;  // 使用回调
}
```

### 3. 资源释放

```csharp
public void Unbind()
{
    // 停止动画
    mTarget?.DOKill();
    
    // 停止音效
    mAudioSource?.Stop();
    
    // 销毁特效
    foreach (var effect in mActiveEffects)
    {
        Object.Destroy(effect);
    }
}
```

---

## 总结

| Binder 类型 | 用途 | 示例 |
|------------|------|------|
| **SoundBinder** | 音效播放 | 金币、受伤、升级音效 |
| **TweenBinder** | 动画效果 | DOTween 抖动、缩放、颜色变化 |
| **ListBinder** | 列表管理 | 自动刷新、清空、重建列表 |
| **ParticleBinder** | 粒子特效 | 获得物品、升级特效 |
| **CustomBinder** | 任意逻辑 | 日志记录、数据分析、网络同步 |

**核心思想**：`IBinder` 让你可以将**任何逻辑**封装成可绑定组件，实现关注点分离，代码更清晰！
