using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.User;
using UIFramework;
using UIFramework.Services;

namespace Game.Setting
{
    /// <summary>
    /// 更换头像/头像框面板 ViewModel（UIFramework MVVM）
    /// </summary>
    public class ChangeHeadPanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>当前选中的 Tab：true=头像，false=头像框</summary>
        public BindableProperty<bool> IsHeadTab { get; } = new BindableProperty<bool>(true);

        /// <summary>当前选中的头像 Id（HeadConfig.Id，0 表示未选）</summary>
        public BindableProperty<int> SelectedHeadId { get; } = new BindableProperty<int>(0);

        /// <summary>当前选中的头像框 Id（HeadConfig.Id，0 表示未选）</summary>
        public BindableProperty<int> SelectedHeadFrameId { get; } = new BindableProperty<int>(0);

        /// <summary>佩戴按钮文案</summary>
        public BindableProperty<string> WearBtnText { get; } = new BindableProperty<string>("佩戴");

        /// <summary>头像列表数据（HeadType.Avatar）</summary>
        public BindableProperty<List<HeadItemData>> HeadItems { get; } = new BindableProperty<List<HeadItemData>>(new List<HeadItemData>());

        /// <summary>头像框列表数据（HeadType.AvatarFrame）</summary>
        public BindableProperty<List<HeadItemData>> HeadFrameItems { get; } = new BindableProperty<List<HeadItemData>>(new List<HeadItemData>());
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command<bool> OnSwitchTabCommand { get; private set; }
        public Command<int> OnSelectHeadCommand { get; private set; }
        public Command<int> OnSelectHeadFrameCommand { get; private set; }
        public Command OnWearCommand { get; private set; }
        #endregion

        private UserService mUserService;
        private int mHeadItemsFirstId;
        private int mHeadFrameItemsFirstId;
        private bool mIsWearing;

        public override void OnCreate()
        {
            base.OnCreate();

            mUserService = GameServiceManager.Get<UserService>();

            OnCloseCommand = new Command(OnClose);
            OnSwitchTabCommand = new Command<bool>(OnSwitchTab);
            OnSelectHeadCommand = new Command<int>(OnSelectHead);
            OnSelectHeadFrameCommand = new Command<int>(OnSelectHeadFrame);
            OnWearCommand = new Command(OnWear);

            BuildHeadItems();
            InitSelection();

            IsHeadTab.OnValueChanged += (_, __) => UpdateWearBtnText();
            SelectedHeadId.OnValueChanged += (_, __) => UpdateWearBtnText();
            SelectedHeadFrameId.OnValueChanged += (_, __) => UpdateWearBtnText();
        }

        private void BuildHeadItems()
        {
            var headItems = new List<HeadItemData>();
            var frameItems = new List<HeadItemData>();
            var wearAvatarId = Global.User.GetAvatarHeadConfigId();
            var wearFrameId = Global.User.AvatarFrame;

            foreach (var cfg in DataConfigManager.Tables.TbHeadConfig.DataList)
            {
                if (cfg.Type == HeadType.Avatar)
                {
                    var displayIndex = cfg.Id - 10000;
                    headItems.Add(new HeadItemData
                    {
                        Id = cfg.Id,
                        HeadType = HeadType.Avatar,
                        Icon = "Head_" + displayIndex,
                        Name = cfg.Name,
                        IsWear = cfg.Id == wearAvatarId,
                        IsUnlocked = PlayerAvatarData.HasAvatar(cfg.Id),
                    });
                }
                else if (cfg.Type == HeadType.AvatarFrame)
                {
                    var frameIndex = cfg.Id - 11000;
                    frameItems.Add(new HeadItemData
                    {
                        Id = cfg.Id,
                        HeadType = HeadType.AvatarFrame,
                        Icon = "AvatarFrame" + frameIndex,
                        Name = cfg.Name,
                        IsWear = cfg.Id == wearFrameId,
                        IsUnlocked = PlayerAvatarData.HasAvatarFrame(cfg.Id),
                    });
                }
            }

            SortHeadItems(headItems);
            SortHeadItems(frameItems);

            if (headItems.Count > 0)
            {
                mHeadItemsFirstId = headItems[0].Id;
            }
            if (frameItems.Count > 0)
            {
                mHeadFrameItemsFirstId = frameItems[0].Id;
            }

            HeadItems.Value = headItems;
            HeadFrameItems.Value = frameItems;
        }

        /// <summary>已解锁在前，同组内按配置 Id 升序。</summary>
        private static void SortHeadItems(List<HeadItemData> items)
        {
            items.Sort((a, b) =>
            {
                if (a.IsUnlocked != b.IsUnlocked)
                {
                    return b.IsUnlocked.CompareTo(a.IsUnlocked);
                }

                return a.Id.CompareTo(b.Id);
            });
        }

        private void InitSelection()
        {
            SelectedHeadId.Value = Global.User.Avatar > 0
                ? Global.User.Avatar
                : mHeadItemsFirstId;

            if (Global.User.AvatarFrame > 0)
            {
                SelectedHeadFrameId.Value = Global.User.AvatarFrame;
            }
            else
            {
                SelectedHeadFrameId.Value = mHeadFrameItemsFirstId;
            }

            UpdateWearBtnText();
        }

        private void UpdateWearBtnText()
        {
            if (IsHeadTab.Value)
            {
                var selected = FindHeadItem(SelectedHeadId.Value);
                WearBtnText.Value = GetWearBtnText(selected);
            }
            else
            {
                var selected = FindHeadFrameItem(SelectedHeadFrameId.Value);
                WearBtnText.Value = GetWearBtnText(selected);
            }
        }

        private static string GetWearBtnText(HeadItemData selected)
        {
            if (selected == null) return "佩戴";
            if (!selected.IsUnlocked) return "未解锁";
            if (selected.IsWear) return "已佩戴";
            return "佩戴";
        }

        private HeadItemData FindHeadItem(int headId)
        {
            var items = HeadItems.Value;
            if (items == null || headId == 0) return null;
            foreach (var item in items)
            {
                if (item.Id == headId) return item;
            }

            return null;
        }

        private HeadItemData FindHeadFrameItem(int frameId)
        {
            var items = HeadFrameItems.Value;
            if (items == null || frameId == 0) return null;
            foreach (var item in items)
            {
                if (item.Id == frameId) return item;
            }

            return null;
        }

        private void OnSwitchTab(bool isHeadTab)
        {
            IsHeadTab.Value = isHeadTab;
        }

        private void OnSelectHead(int headId)
        {
            SelectedHeadId.Value = headId;
        }

        private void OnSelectHeadFrame(int frameId)
        {
            SelectedHeadFrameId.Value = frameId;
        }

        private async void OnWear()
        {
            if (mIsWearing) return;

            if (IsHeadTab.Value)
            {
                await WearHeadAsync();
            }
            else
            {
                await WearHeadFrameAsync();
            }
        }

        private async UniTask WearHeadAsync()
        {
            var items = HeadItems.Value;
            if (items == null || SelectedHeadId.Value == 0)
            {
                OnShowTip?.Invoke("请先选择头像", 2f);
                return;
            }

            HeadItemData selected = null;
            foreach (var item in items)
            {
                if (item.Id == SelectedHeadId.Value)
                {
                    selected = item;
                    break;
                }
            }
            if (selected == null) return;

            if (!selected.IsUnlocked)
            {
                OnShowTip?.Invoke("尚未拥有该头像", 2f);
                return;
            }

            if (selected.Id == Global.User.GetAvatarHeadConfigId())
            {
                OnShowTip?.Invoke("当前已佩戴该头像", 2f);
                return;
            }

            mIsWearing = true;
            try
            {
                var success = await mUserService.UpdateAvatarAsync(selected.Id);
                if (!success)
                {
                    OnShowTip?.Invoke("佩戴失败，请稍后重试", 2f);
                    return;
                }

                foreach (var item in items)
                {
                    item.IsWear = item.Id == selected.Id;
                }

                UpdateWearBtnText();
                OnShowTip?.Invoke("佩戴成功", 1f);
                OnWearHeadCompleted?.Invoke(selected.Id);
            }
            catch (Exception ex)
            {
                OnShowTip?.Invoke(ex.Message, 2f);
            }
            finally
            {
                mIsWearing = false;
            }
        }

        private async UniTask WearHeadFrameAsync()
        {
            var items = HeadFrameItems.Value;
            if (items == null || SelectedHeadFrameId.Value == 0)
            {
                OnShowTip?.Invoke("请先选择头像框", 2f);
                return;
            }

            HeadItemData selected = null;
            foreach (var item in items)
            {
                if (item.Id == SelectedHeadFrameId.Value)
                {
                    selected = item;
                    break;
                }
            }
            if (selected == null) return;

            if (!selected.IsUnlocked)
            {
                OnShowTip?.Invoke("尚未拥有该头像框", 2f);
                return;
            }

            if (selected.Id == Global.User.AvatarFrame)
            {
                OnShowTip?.Invoke("当前已佩戴该头像框", 2f);
                return;
            }

            mIsWearing = true;
            try
            {
                var success = await mUserService.UpdateAvatarFrameAsync(selected.Id);
                if (!success)
                {
                    OnShowTip?.Invoke("佩戴失败，请稍后重试", 2f);
                    return;
                }

                foreach (var item in items)
                {
                    item.IsWear = item.Id == selected.Id;
                }

                UpdateWearBtnText();
                OnShowTip?.Invoke("佩戴成功", 1f);
                OnWearHeadFrameCompleted?.Invoke(selected.Id);
            }
            catch (Exception ex)
            {
                OnShowTip?.Invoke(ex.Message, 2f);
            }
            finally
            {
                mIsWearing = false;
            }
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #region 事件
        public Action OnClosePanel;
        public Action<int> OnWearHeadCompleted;
        public Action<int> OnWearHeadFrameCompleted;
        public Action<string, float> OnShowTip;
        #endregion
    }

    /// <summary>头像/头像框列表项数据</summary>
    public class HeadItemData
    {
        public int Id;
        public HeadType HeadType;
        /// <summary>图集 sprite 名（头像：Head_1；头像框：AvatarFrame1）</summary>
        public string Icon;
        public string Name;
        public bool IsWear;
        /// <summary>是否已解锁（拥有），依据 Global.PlayerAvatars / PlayerAvatarFrames</summary>
        public bool IsUnlocked;
    }
}
