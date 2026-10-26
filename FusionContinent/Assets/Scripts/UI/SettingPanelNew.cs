using Cysharp.Threading.Tasks;
using FusionContinent.Animation;
using FusionContinent.Model;
using Game.Login;
using Game.Setting;
using QFramework;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("SettingPanel")]
    public class SettingPanelNew : UIPanel<SettingPanelViewModel>
    {
        private SettingPanel mOldPanel;
        private bool mIsClosing;
        private GameObject mFeedbackPanel;

        private UnityAction mQuitHandler;
        private UnityAction mCloseBtnHandler;
        private UnityAction mMusicHandler;
        private UnityAction mSoundHandler;
        private UnityAction mFeedbackHandler;
        private UnityAction mLogoutHandler;
        private UnityAction mGiftCodeHandler;
        private UnityAction mUserNameToolHandler;
        private UnityAction mChanNameHandler;
        private UnityAction mCopyUidHandler;
        private UnityAction mChangeHeadHandler;
        private UnityAction<int> mLanguageChangedHandler;

        private System.Action<string, string> mUserNameChangedHandler;
        private System.Action<string, string> mUserIdChangedHandler;
        private System.Action<string, string> mScoreChangedHandler;
        private System.Action<string, string> mStageNameChangedHandler;
        private System.Action<int, int> mAvatarChangedHandler;
        private System.Action<bool, bool> mMusicChangedHandler;
        private System.Action<bool, bool> mSoundChangedHandler;
        private System.Action<int, int> mSelectedLangChangedHandler;
        private System.Action<object> mUserNamePubHandler;
        private System.Action<object> mAvatarPubHandler;
        private System.Action<object> mAvatarFramePubHandler;

        void Awake()
        {
            mOldPanel = GetComponent<SettingPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SettingPanelNew] 找不到 SettingPanel 组件");
            }
        }

        protected override void OnBindViewModel(SettingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mQuitHandler = () => vm.OnCloseCommand.Execute();
            mCloseBtnHandler = () => vm.OnCloseCommand.Execute();
            mMusicHandler = () => vm.OnToggleMusicCommand.Execute();
            mSoundHandler = () => vm.OnToggleSoundCommand.Execute();
            mFeedbackHandler = () => vm.OnShowFeedbackCommand.Execute();
            mLogoutHandler = () => vm.OnLogoutCommand.Execute();
            mGiftCodeHandler = () => vm.OnOpenGiftCodeCommand.Execute();
            mUserNameToolHandler = () => vm.TryOpenToolPanel();

            mOldPanel.Quit.onClick.AddListener(mQuitHandler);
            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);
            mOldPanel.MusicBtn.onClick.AddListener(mMusicHandler);
            mOldPanel.SoundBtn.onClick.AddListener(mSoundHandler);
            mOldPanel.FeedbackBtn.onClick.AddListener(mFeedbackHandler);
            mOldPanel.LogOutBtn.onClick.AddListener(mLogoutHandler);
            mOldPanel.GiftCodeBtn.onClick.AddListener(mGiftCodeHandler);

            // 改名：复用 CreateNamePanel（与 SpeakWhellPanel.EditNameBtn 逻辑一致）。
            // 改名成功后 LoginService 会广播 CHANGE_USER_NAME，由 mUserNamePubHandler 刷新显示。
            mChanNameHandler = OnChanNameBtnClicked;
            if (mOldPanel.ChanNameBtn != null)
            {
                mOldPanel.ChanNameBtn.onClick.AddListener(mChanNameHandler);
            }

            // 复制 UID：写入系统剪贴板，VM 完成后回调弹 Toast 提示。
            mCopyUidHandler = () => vm.OnCopyUidCommand.Execute();
            if (mOldPanel.CopyUIDBtn != null)
            {
                mOldPanel.CopyUIDBtn.onClick.AddListener(mCopyUidHandler);
            }

            // 点击头像打开更换头像/头像框面板。PlayerHead 源 prefab 上自带 Button。
            mChangeHeadHandler = OpenChangeHeadPanel;
            var headBtn = mOldPanel.PlayerHead.GetComponent<Button>();
            if (headBtn != null)
            {
                headBtn.onClick.AddListener(mChangeHeadHandler);
            }

            if (mOldPanel.UserName != null)
            {
                var btn = mOldPanel.UserName.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(mUserNameToolHandler);
                }
            }

            mLanguageChangedHandler = idx => vm.OnSelectLanguageCommand.Execute(idx);
            mOldPanel.LanguageDropdown.onValueChanged.AddListener(mLanguageChangedHandler);

            mUserNameChangedHandler = (o, n) => { if (mOldPanel.UserName != null) mOldPanel.UserName.text = n; };
            mUserIdChangedHandler = (o, n) => { if (mOldPanel.UserId != null) mOldPanel.UserId.text = n; };
            mScoreChangedHandler = (o, n) => { if (mOldPanel.ScoreNum != null) mOldPanel.ScoreNum.text = n; };
            mStageNameChangedHandler = (o, n) => { if (mOldPanel.StageName != null) mOldPanel.StageName.text = n; };
            mAvatarChangedHandler = (o, n) => RefreshAvatar();
            mMusicChangedHandler = (o, n) => ApplyToggleVisual(mOldPanel.MusicBtn, n);
            mSoundChangedHandler = (o, n) => ApplyToggleVisual(mOldPanel.SoundBtn, n);
            mSelectedLangChangedHandler = (o, n) => SetDropdownValueWithoutNotify(mOldPanel.LanguageDropdown, n);

            vm.UserName.OnValueChanged += mUserNameChangedHandler;
            vm.UserIdText.OnValueChanged += mUserIdChangedHandler;
            vm.ScoreText.OnValueChanged += mScoreChangedHandler;
            vm.StageNameText.OnValueChanged += mStageNameChangedHandler;
            vm.AvatarId.OnValueChanged += mAvatarChangedHandler;
            vm.IsMusicOn.OnValueChanged += mMusicChangedHandler;
            vm.IsSoundOn.OnValueChanged += mSoundChangedHandler;
            vm.SelectedLanguageIndex.OnValueChanged += mSelectedLangChangedHandler;

            vm.OnClosePanel += ClosePanelWithAnimation;
            vm.OnShowFeedbackRequested += ShowFeedbackPanel;
            vm.OnOpenGiftCodeRequested += OpenGiftCodePanel;
            vm.OnSoundVolumeRequested += SetSoundVolume;
            vm.OnOpenToolPanelRequested += OpenToolPanel;
            vm.OnCopyUidCompleted += OnCopyUidCompleted;

            // 改名成功后 LoginService 广播此消息，刷新界面上显示的用户名
            mUserNamePubHandler = OnUserNamePublished;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_NAME, mUserNamePubHandler);

            mAvatarPubHandler = OnAvatarPublished;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_AVATAR, mAvatarPubHandler);
            mAvatarFramePubHandler = OnAvatarFramePublished;
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_AVATAR_FRAME, mAvatarFramePubHandler);

            InitLanguageDropdown(vm);
            ApplyToggleVisual(mOldPanel.MusicBtn, vm.IsMusicOn.Value);
            ApplyToggleVisual(mOldPanel.SoundBtn, vm.IsSoundOn.Value);
            RefreshAvatar();
        }

        protected override void OnUnbindViewModel(SettingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mQuitHandler != null)
            {
                mOldPanel.Quit.onClick.RemoveListener(mQuitHandler);
                mQuitHandler = null;
            }
            if (mCloseBtnHandler != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }
            if (mMusicHandler != null)
            {
                mOldPanel.MusicBtn.onClick.RemoveListener(mMusicHandler);
                mMusicHandler = null;
            }
            if (mSoundHandler != null)
            {
                mOldPanel.SoundBtn.onClick.RemoveListener(mSoundHandler);
                mSoundHandler = null;
            }
            if (mFeedbackHandler != null)
            {
                mOldPanel.FeedbackBtn.onClick.RemoveListener(mFeedbackHandler);
                mFeedbackHandler = null;
            }
            if (mLogoutHandler != null)
            {
                mOldPanel.LogOutBtn.onClick.RemoveListener(mLogoutHandler);
                mLogoutHandler = null;
            }
            if (mGiftCodeHandler != null)
            {
                mOldPanel.GiftCodeBtn.onClick.RemoveListener(mGiftCodeHandler);
                mGiftCodeHandler = null;
            }

            if (mChanNameHandler != null && mOldPanel.ChanNameBtn != null)
            {
                mOldPanel.ChanNameBtn.onClick.RemoveListener(mChanNameHandler);
            }
            mChanNameHandler = null;

            if (mCopyUidHandler != null && mOldPanel.CopyUIDBtn != null)
            {
                mOldPanel.CopyUIDBtn.onClick.RemoveListener(mCopyUidHandler);
            }
            mCopyUidHandler = null;

            if (mChangeHeadHandler != null)
            {
                var headBtn = mOldPanel.PlayerHead.GetComponent<Button>();
                if (headBtn != null)
                {
                    headBtn.onClick.RemoveListener(mChangeHeadHandler);
                }
            }
            mChangeHeadHandler = null;

            if (mOldPanel.UserName != null)
            {
                var btn = mOldPanel.UserName.GetComponent<Button>();
                if (btn != null && mUserNameToolHandler != null)
                {
                    btn.onClick.RemoveListener(mUserNameToolHandler);
                }
            }
            mUserNameToolHandler = null;

            if (mLanguageChangedHandler != null)
            {
                mOldPanel.LanguageDropdown.onValueChanged.RemoveListener(mLanguageChangedHandler);
                mLanguageChangedHandler = null;
            }

            if (mUserNameChangedHandler != null)
            {
                vm.UserName.OnValueChanged -= mUserNameChangedHandler;
                mUserNameChangedHandler = null;
            }
            if (mUserIdChangedHandler != null)
            {
                vm.UserIdText.OnValueChanged -= mUserIdChangedHandler;
                mUserIdChangedHandler = null;
            }
            if (mScoreChangedHandler != null)
            {
                vm.ScoreText.OnValueChanged -= mScoreChangedHandler;
                mScoreChangedHandler = null;
            }
            if (mStageNameChangedHandler != null)
            {
                vm.StageNameText.OnValueChanged -= mStageNameChangedHandler;
                mStageNameChangedHandler = null;
            }
            if (mAvatarChangedHandler != null)
            {
                vm.AvatarId.OnValueChanged -= mAvatarChangedHandler;
                mAvatarChangedHandler = null;
            }
            if (mMusicChangedHandler != null)
            {
                vm.IsMusicOn.OnValueChanged -= mMusicChangedHandler;
                mMusicChangedHandler = null;
            }
            if (mSoundChangedHandler != null)
            {
                vm.IsSoundOn.OnValueChanged -= mSoundChangedHandler;
                mSoundChangedHandler = null;
            }
            if (mSelectedLangChangedHandler != null)
            {
                vm.SelectedLanguageIndex.OnValueChanged -= mSelectedLangChangedHandler;
                mSelectedLangChangedHandler = null;
            }

            vm.OnClosePanel -= ClosePanelWithAnimation;
            vm.OnShowFeedbackRequested -= ShowFeedbackPanel;
            vm.OnOpenGiftCodeRequested -= OpenGiftCodePanel;
            vm.OnSoundVolumeRequested -= SetSoundVolume;
            vm.OnOpenToolPanelRequested -= OpenToolPanel;
            vm.OnCopyUidCompleted -= OnCopyUidCompleted;

            if (mUserNamePubHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_NAME, mUserNamePubHandler);
                mUserNamePubHandler = null;
            }
            if (mAvatarPubHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_AVATAR, mAvatarPubHandler);
                mAvatarPubHandler = null;
            }
            if (mAvatarFramePubHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_AVATAR_FRAME, mAvatarFramePubHandler);
                mAvatarFramePubHandler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;

            mIsClosing = false;
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, false);

            VM.OnOpenCommand.Execute();
            PlayOpenAnimation();
        }

        protected override void OnHide()
        {
            mIsClosing = false;
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, true);
        }

        private void InitLanguageDropdown(SettingPanelViewModel vm)
        {
            if (mOldPanel?.LanguageDropdown == null) return;

            var dropdown = mOldPanel.LanguageDropdown;
            dropdown.ClearOptions();
            dropdown.AddOptions(vm.LanguageOptions.Value ?? new System.Collections.Generic.List<string>());
            SetDropdownValueWithoutNotify(dropdown, vm.SelectedLanguageIndex.Value);
            dropdown.RefreshShownValue();
        }

        private void SetDropdownValueWithoutNotify(TMPro.TMP_Dropdown dropdown, int value)
        {
            if (dropdown == null) return;
            value = Mathf.Clamp(value, 0, Mathf.Max(0, dropdown.options.Count - 1));

            // TMP_Dropdown.SetValueWithoutNotify 存在版本差异，这里做兼容写法
            var old = dropdown.value;
            dropdown.value = value;
            if (old != value)
            {
                dropdown.RefreshShownValue();
            }
        }

        private void ApplyToggleVisual(Button btn, bool isOn)
        {
            if (btn == null) return;

            var onNode = btn.transform.Find("开");
            var onBtnNode = btn.transform.Find("开btn");
            var offNode = btn.transform.Find("关");
            var offBtnNode = btn.transform.Find("关btn");

            if (onNode != null) onNode.localScale = isOn ? Vector3.one : Vector3.zero;
            if (onBtnNode != null) onBtnNode.localScale = isOn ? Vector3.one : Vector3.zero;
            if (offNode != null) offNode.localScale = isOn ? Vector3.zero : Vector3.one;
            if (offBtnNode != null) offBtnNode.localScale = isOn ? Vector3.zero : Vector3.one;
        }

        private void RefreshAvatar()
        {
            mOldPanel.PlayerHead.SetData(Global.User);
            // if (mOldPanel?.UserAvatarBtn == null) return;

            // var img = mOldPanel.UserAvatarBtn.GetComponent<Image>();
            // if (img == null) return;
            //
            // img.sprite = Global.AvatarIconSpriteAtlas.GetSprite("Head_" + avatarKey);
        }

        private void SetSoundVolume(bool isOn)
        {
            var audioSource = UIFramework.UIRoot.Instance?.transform.Find("Audio Source")?.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = isOn ? 1 : 0;
            }
        }

        private void ShowFeedbackPanel()
        {
            if (mFeedbackPanel == null)
            {
                mFeedbackPanel = ResLoader.Allocate().LoadSync<GameObject>("FeedbackPanel").Instantiate();
                mFeedbackPanel.transform.SetParent(transform);
                mFeedbackPanel.transform.localPosition = Vector3.zero;
                mFeedbackPanel.transform.localScale = Vector3.one;
            }
            else
            {
                mFeedbackPanel.SetActive(true);
            }
        }

        private void OpenGiftCodePanel()
        {
            var vm = new Game.GiftCode.GiftCodeInterfacePanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<GiftCodeInterfacePanelNew, Game.GiftCode.GiftCodeInterfacePanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OpenToolPanel()
        {
            UIKit.OpenPanel<ToolPanel>();
        }

        private void OnChanNameBtnClicked()
        {
            var vm = new CreateNamePanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<CreateNamePanelNew, CreateNamePanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnUserNamePublished(object userName)
        {
            if (mOldPanel?.UserName != null)
            {
                mOldPanel.UserName.text = (string)userName;
            }
        }

        private void OnAvatarPublished(object avatar)
        {
            RefreshAvatar();
        }

        private void OnAvatarFramePublished(object avatarFrame)
        {
            RefreshAvatar();
        }

        private void OnCopyUidCompleted()
        {
            Toast.Show("UID 已复制");
        }

        private void OpenChangeHeadPanel()
        {
            var vm = new Game.Setting.ChangeHeadPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<ChangeHeadPanelNew, Game.Setting.ChangeHeadPanelViewModel>(
                vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void PlayOpenAnimation()
        {
            if (mOldPanel?.OpenAnimation == null) return;
            UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject);
        }

        private void ClosePanelWithAnimation()
        {
            if (mIsClosing) return;
            mIsClosing = true;

            if (mOldPanel?.CloseAnimation == null)
            {
                CloseSelf();
                return;
            }

            UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () => CloseSelf());
        }
    }
}

