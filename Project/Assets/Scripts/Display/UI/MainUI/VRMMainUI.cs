using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace XiaoZhi.Unity
{
    public class VRMMainUI : BaseUI
    {
        private RectTransform _trSet;
        private Button _btnSet;
        private GameObject _goLoading;
        private RectTransform _trStatus;
        private TextMeshProUGUI _textStatus;
        private LocalizeStringEvent _localizeStatus;
        private TextMeshProUGUI _textChat;
        private LocalizeStringEvent _localizeChat;
        private CancellationTokenSource _autoHideCts;
        
        public override string GetResourcePath()
        {
            return "Assets/Res/UI/MainUI/VRMMainUI.prefab";
        }

        protected override void OnInit()
        {
            Tr.GetComponent<XButton>().onClick.AddListener(() =>
            {
                if (Context.App.IsDeviceReady() && AppSettings.Instance.IsAutoHideUI())
                {
                    ClearAutoHideCts();
                    UpdateCompVisible(true);
                    AutoHideComp();
                }
            });
            _goLoading = Tr.Find("Loading").gameObject;
            _trSet = GetComponent<RectTransform>(Tr, "BtnSet");
            _trSet.GetComponent<XButton>().onClick.AddListener(() => { ShowModuleUI<SettingsUI>().Forget(); });
            GetComponent<XButton>(Tr, "ClickRole").onClick.AddListener(() => Context.App.ToggleChatState().Forget());
            _trStatus = GetComponent<RectTransform>(Tr, "Status");
            _textStatus = GetComponent<TextMeshProUGUI>(_trStatus, "Text");
            _textStatus.text = "";
            _localizeStatus = GetComponent<LocalizeStringEvent>(_textStatus, "");
            _localizeStatus.StringReference = null;
            _textChat = Tr.Find("Chat").GetComponent<TextMeshProUGUI>();
            _textChat.text = "";
            _localizeChat = GetComponent<LocalizeStringEvent>(_textChat, "");
            _localizeChat.StringReference = null;
            GetComponent<HyperlinkText>(_textChat, "").OnClickLink
                .AddListener(_ => Application.OpenURL(AppPresets.Instance.ActivationURL));
            GetComponent<HyperlinkText>(_textStatus, "").OnClickLink.AddListener(_ => Application.OpenURL(AppPresets.Instance.ActivationURL));
        }

        protected override async UniTask OnShow(BaseUIData data = null)
        {
            Context.App.OnDeviceStateUpdate -= OnDeviceStateUpdate;
            Context.App.OnDeviceStateUpdate += OnDeviceStateUpdate;
            AppSettings.Instance.OnAutoHideUIUpdate -= OnAutoHideUIUpdate;
            AppSettings.Instance.OnAutoHideUIUpdate += OnAutoHideUIUpdate;
            DetectCompVisible(true);
            await UniTask.CompletedTask;
        }

        protected override async UniTask OnHide()
        {
            ClearAutoHideCts();
            KillCompVisibleAnim();
            Context.App.OnDeviceStateUpdate -= OnDeviceStateUpdate;
            AppSettings.Instance.OnAutoHideUIUpdate -= OnAutoHideUIUpdate;
            await UniTask.CompletedTask;
        }

        public void ShowLoading(bool show)
        {
            _goLoading.SetActive(show);
        }

        public void SetStatus(string status)
        {
            _localizeStatus.StringReference = null;
            _textStatus.text = status;
        }

        public void SetStatus(LocalizedString status)
        {
            _localizeStatus.StringReference = status;
        }
        
        public void SetChatMessage(ChatRole role, string content)
        {
            _localizeChat.StringReference = null;
            _textChat.text = content;
        }

        public void SetChatMessage(ChatRole role, LocalizedString content)
        {
            _localizeChat.StringReference = content;
        }
        
        public void SetActivateLink(string content)
        {
            _localizeChat.StringReference = null;
            _textChat.text = $"<u><link=\"0\">{content}</link></u>";
        }

        private void OnDeviceStateUpdate(DeviceState state)
        {
            ClearAutoHideCts();
            DetectCompVisible();
        }

        private void OnAutoHideUIUpdate(bool autoHide)
        {
            ClearAutoHideCts();
            if (autoHide) AutoHideComp();
            else DetectCompVisible();
        }

        private void ClearAutoHideCts()
        {
            if (_autoHideCts != null)
            {
                _autoHideCts.Cancel();
                _autoHideCts.Dispose();
                _autoHideCts = null;
            }
        }

        private void AutoHideComp()
        {
            _autoHideCts = new CancellationTokenSource();
            UniTask.Void(async token =>
            {
                await UniTask.Delay(3000, cancellationToken: token);
                DetectCompVisible();
            }, _autoHideCts.Token);
        }

        private void DetectCompVisible(bool instant = false)
        {
            UpdateCompVisible(Context.App.IsDeviceReady() && !AppSettings.Instance.IsAutoHideUI(), instant);
        }

        private void UpdateCompVisible(bool visible, bool instant = false)
        {
            var trSetPosY = visible ? -100 : 100;
            if (instant)
            {
                _trSet.SetAnchorPosY(trSetPosY);
            }
            else
            {
                _trSet.DOAnchorPosY(trSetPosY, AnimationDuration).SetEase(Ease.InOutSine);
            }
        }

        private void KillCompVisibleAnim()
        {
            _trSet.DOKill();
        }
    }
}