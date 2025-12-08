using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using InputField = UnityEngine.UI.InputField;

namespace XiaoZhi.Unity
{
    public class EmojiMainUI : BaseUI
    {
        private static readonly Dictionary<string, string> Emojis = new()
        {
            { "sleep", "😴" },
            { "neutral", "🙂" },
            { "standby", "😐" },
            { "happy", "😄" },
            { "funny", "😜" },
            { "sad", "🙁" },
            { "thinking", "🤔" },
        };

        private const int SpectrumUpdateInterval = 50;
        private const float OutputScaleFactor = 0.5f;
        private const string EmotionSpriteRoot = "UI/Emoji/";
        private readonly Dictionary<string, Sprite> _emotionSpriteCache = new(StringComparer.OrdinalIgnoreCase);
        
        private LocalizeStringEvent _localizeStatus;
        private LocalizeStringEvent _localizeInfo;
        private TextMeshProUGUI _textInfo;
        private TextMeshProUGUI _textChat;
        private Transform _trEmotion;
        private TextMeshProUGUI _textEmotion;
        private Image _imgEmotion;
        private Button _btnEmotion;
        private GameObject _textInputRoot;
        private InputField _inputText;
        private Button _btnSend;
        private Button _btnMic;
        private RectTransform _trSet;
        private Button _btnSet;
        private XInputWave _xInputWave;
        private GameObject _goLoading;

        private CancellationTokenSource _loopCts;
        private CancellationTokenSource _autoHideCts;
        private Sequence _breatheSequence;
        private Sequence _wakeUpSequence;
        private Talk.State _lastTalkState;
        private float _normalizedOutputDb;

        public override string GetResourcePath()
        {
            return "Assets/Res/UI/MainUI/EmojiMainUI.prefab";
        }

        protected override void OnInit()
        {
            Tr.GetComponent<XButton>().onClick.AddListener(() =>
            {
                if (Context.App.Talk.IsReady() && AppSettings.Instance.IsAutoHideUI())
                {
                    ClearAutoHideCts();
                    UpdateCompVisible(true);
                    AutoHideComp();
                }
            });
            _localizeStatus = Tr.Find("Status/Stat").GetComponent<LocalizeStringEvent>();
            _localizeStatus.StringReference = null;
            _localizeInfo = Tr.Find("Status/Info").GetComponent<LocalizeStringEvent>();
            _localizeInfo.StringReference = null;
            _textInfo = GetComponent<TextMeshProUGUI>(_localizeInfo, "");
            _textChat = Tr.Find("Chat").GetComponent<TextMeshProUGUI>();
            _textChat.text = "";
            GetComponent<HyperlinkText>(_textChat, "").OnClickLink
                .AddListener(_ => Application.OpenURL(AppPresets.Instance.ActivationURL));
            _trEmotion = Tr.Find("Emotion");
            _textEmotion = _trEmotion.GetComponent<TextMeshProUGUI>();
            _textEmotion.text = "";
            _btnEmotion = _trEmotion.GetComponent<Button>() ?? _trEmotion.gameObject.AddComponent<Button>();
            _btnEmotion.transition = Selectable.Transition.None;
            var btnGraphic = _trEmotion.GetComponent<Graphic>();
            if (btnGraphic == null)
            {
                var bg = _trEmotion.gameObject.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0); // invisible click catcher
                bg.raycastTarget = true;
                btnGraphic = bg;
                var bgRect = bg.rectTransform;
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
            }
            else
            {
                btnGraphic.raycastTarget = true;
            }
            _btnEmotion.targetGraphic = btnGraphic;
            _btnEmotion.interactable = true;
            _btnEmotion.onClick.AddListener(() => Context.App.ToggleChatState().Forget());
            var iconTr = _trEmotion.Find("Icon");
            if (iconTr != null) _imgEmotion = iconTr.GetComponent<Image>();
            if (_imgEmotion == null)
            {
                var goIcon = iconTr != null ? iconTr.gameObject : new GameObject("Icon");
                if (iconTr == null)
                {
                    goIcon.transform.SetParent(_trEmotion, false);
                }

                _imgEmotion = goIcon.GetComponent<Image>() ?? goIcon.AddComponent<Image>();
            }

            var iconRect = _imgEmotion.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.localScale = Vector3.one;
            _imgEmotion.raycastTarget = true;
            var le = _imgEmotion.GetComponent<LayoutElement>() ?? _imgEmotion.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            _imgEmotion.gameObject.SetActive(false);
            BuildTextInput();
            _goLoading = Tr.Find("Loading").gameObject;
            _trSet = GetComponent<RectTransform>(Tr, "BtnSet");
            _trSet.GetComponent<XButton>().onClick.AddListener(() => { ShowModuleUI<SettingsUI>().Forget(); });
            _xInputWave = Tr.Find("Spectrum").GetComponent<XInputWave>();
        }

        protected override async UniTask OnShow(BaseUIData data = null)
        {
            _loopCts = new CancellationTokenSource();
            UniTask.Void(LoopUpdate, _loopCts.Token);
            Context.App.Talk.OnStateUpdate -= OnTalkStateUpdate;
            Context.App.Talk.OnStateUpdate += OnTalkStateUpdate;
            Context.App.Talk.OnEmotionUpdate -= OnTalkEmotionUpdate;
            Context.App.Talk.OnEmotionUpdate += OnTalkEmotionUpdate;
            Context.App.Talk.OnInfoUpdate -= OnTalkInfoUpdate;
            Context.App.Talk.OnInfoUpdate += OnTalkInfoUpdate;
            Context.App.Talk.OnChatUpdate -= OnTalkChatUpdate;
            Context.App.Talk.OnChatUpdate += OnTalkChatUpdate;
            AppSettings.Instance.OnAutoHideUIUpdate -= OnAutoHideUIUpdate;
            AppSettings.Instance.OnAutoHideUIUpdate += OnAutoHideUIUpdate;
            AppSettings.Instance.OnTextInputEnableUpdate -= OnTextInputToggle;
            AppSettings.Instance.OnTextInputEnableUpdate += OnTextInputToggle;
            OnTextInputToggle(AppSettings.Instance.IsTextInputEnabled());
            DetectCompVisible(true);
            await UniTask.CompletedTask;
        }

        protected override async UniTask OnHide()
        {
            Context.App.Talk.OnStateUpdate -= OnTalkStateUpdate;
            Context.App.Talk.OnEmotionUpdate -= OnTalkEmotionUpdate;
            Context.App.Talk.OnInfoUpdate -= OnTalkInfoUpdate;
            Context.App.Talk.OnChatUpdate -= OnTalkChatUpdate;
            AppSettings.Instance.OnAutoHideUIUpdate -= OnAutoHideUIUpdate;
            AppSettings.Instance.OnTextInputEnableUpdate -= OnTextInputToggle;
            if (_loopCts != null)
            {
                _loopCts.Cancel();
                _loopCts.Dispose();
                _loopCts = null;
            }

            ClearAutoHideCts();
            KillCompVisibleAnim();
            StopBreathingAnimation();
            StopWakeUpAnimation();
            await UniTask.CompletedTask;
        }

        private async UniTaskVoid LoopUpdate(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(SpectrumUpdateInterval / 2, DelayType.Realtime, PlayerLoopTiming.Update, token);
                await UpdateInputWave(token);
                await UniTask.Delay(SpectrumUpdateInterval / 2, DelayType.Realtime, PlayerLoopTiming.Update, token);
                await UpdateOutputWave(token);
            }
        }

        private async UniTask UpdateInputWave(CancellationToken token)
        {
            if (!_xInputWave.gameObject.activeInHierarchy) return;
            await UniTask.SwitchToThreadPool();
            var codec = Context.App.GetCodec();
            var inputDirty = codec != null && _xInputWave.UpdateSpectrumData(codec);
            await UniTask.SwitchToMainThread(token);
            if (inputDirty) _xInputWave.SetVerticesDirty();
        }

        private async UniTask UpdateOutputWave(CancellationToken token)
        {
            if (!_trEmotion.gameObject.activeInHierarchy) return;
            await UniTask.SwitchToThreadPool();
            QuantizeOutputWave();
            await UniTask.SwitchToMainThread(token);
            var scale = 1 + _normalizedOutputDb * OutputScaleFactor;
            _trEmotion.localScale = Vector3.Lerp(_trEmotion.localScale,
                new Vector3(scale, scale, 1), 0.7f);
        }

        private void QuantizeOutputWave()
        {
            var codec = Context.App.GetCodec();
            if (codec == null) return;
            if (!codec.GetOutputSpectrum(true, out var outputSpectrum)) return;
            var sums = 0.0f;
            foreach (var sample in outputSpectrum) sums += sample;
            _normalizedOutputDb = Tools.Linear2dB(Math.Max(sums, 0) / outputSpectrum.Length);
        }

        private void UpdateLoadingState()
        {
            _goLoading.SetActive(Context.App.Talk.Stat is Talk.State.Starting);
        }

        private void OnTalkStateUpdate(Talk.State state)
        {
            ClearAutoHideCts();
            DetectCompVisible();
            if (_trEmotion && !_trEmotion.gameObject.activeSelf)
            {
                _trEmotion.gameObject.SetActive(true);
                Debug.Log("[EmojiMainUI] Reactivated Emotion container on state change");
            }
            if (state is Talk.State.Idle or Talk.State.Connecting) StartBreathingAnimation();
            else StopBreathingAnimation();
            if (_lastTalkState == Talk.State.Connecting && state == Talk.State.Listening)
                PlayWakeUpAnimation();
            UpdateLoadingState();
            _localizeStatus.StringReference = Lang.GetRef(Talk.State2LocalizedKey(state));
            _lastTalkState = state;
            if (state is Talk.State.Idle or Talk.State.Connecting)
            {
                SetEmotionVisual("standby");
            }
        }

        private void OnTalkChatUpdate(string content)
        {
            _textChat.text = Context.App.Talk.Stat == Talk.State.Activating
                ? $"<u><link=\"0\">{content}</link></u>"
                : content;
        }

        private void OnTalkInfoUpdate(LocalizedString info)
        {
            _localizeInfo.StringReference = info;
            if (info == null) _textInfo.text = "";
        }

        private void OnTalkEmotionUpdate(string emotion)
        {
            Debug.Log($"[EmojiMainUI] OnEmotionUpdate: '{emotion}'");
            SetEmotionVisual(emotion);
        }

        private void SetEmotionVisual(string emotion)
        {
            if (!_trEmotion.gameObject.activeSelf) _trEmotion.gameObject.SetActive(true);
            var sprite = LoadEmotionSprite(emotion);
            if (sprite)
            {
                _imgEmotion.sprite = sprite;
                _imgEmotion.SetNativeSize();
                var size = _imgEmotion.rectTransform.sizeDelta;
                if (size == Vector2.zero) _imgEmotion.rectTransform.sizeDelta = new Vector2(128, 128);
                _imgEmotion.canvasRenderer.SetAlpha(1f);
                _imgEmotion.color = Color.white;
                _imgEmotion.enabled = true;
                _imgEmotion.transform.SetAsLastSibling();
                _imgEmotion.gameObject.SetActive(true);
                _textEmotion.enabled = false;
                Debug.Log(
                    $"[EmojiMainUI] Showing sprite for emotion '{emotion}' size={_imgEmotion.rectTransform.sizeDelta} active={_imgEmotion.gameObject.activeSelf} enabled={_imgEmotion.enabled} parent={_imgEmotion.transform.parent?.name}");
                return;
            }

            _imgEmotion.enabled = false;
            _imgEmotion.gameObject.SetActive(false);
            _textEmotion.enabled = true;
            _textEmotion.text = Emojis.GetValueOrDefault(emotion, Emojis["neutral"]);
            Debug.Log($"[EmojiMainUI] Fallback to text for emotion '{emotion}' -> '{_textEmotion.text}'");
        }

        private void OnAutoHideUIUpdate(bool autoHide)
        {
            ClearAutoHideCts();
            if (autoHide) AutoHideComp();
            else DetectCompVisible();
        }

        private Sprite LoadEmotionSprite(string emotion)
        {
            if (string.IsNullOrEmpty(emotion)) return null;
            if (_emotionSpriteCache.TryGetValue(emotion, out var cached)) return cached;
            var path = EmotionSpriteRoot + emotion;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite)
            {
                _emotionSpriteCache[emotion] = sprite;
                Debug.Log($"[EmojiMainUI] Loaded sprite for emotion '{emotion}' from Resources '{path}'");
            }
            else
            {
                Debug.Log($"[EmojiMainUI] No sprite found for emotion '{emotion}' at Resources '{path}', fallback to text");
            }
            return sprite;
        }

        private void BuildTextInput()
        {
            _textInputRoot = new GameObject("TextInput", typeof(RectTransform));
            var rt = _textInputRoot.GetComponent<RectTransform>();
            rt.SetParent(Tr, false);
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.15f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var bg = _textInputRoot.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.35f);
            var outline = _textInputRoot.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, 0.25f);

            var inputGO = new GameObject("Input", typeof(RectTransform));
            var inputRT = inputGO.GetComponent<RectTransform>();
            inputRT.SetParent(rt, false);
            inputRT.anchorMin = new Vector2(0, 0);
            inputRT.anchorMax = new Vector2(0.8f, 1);
            inputRT.offsetMin = new Vector2(8, 6);
            inputRT.offsetMax = new Vector2(-8, -6);

            _inputText = inputGO.AddComponent<InputField>();
            var textComp = inputGO.AddComponent<Text>();
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.color = Color.white;
            textComp.supportRichText = false;
            _inputText.textComponent = textComp;
            _inputText.placeholder = CreatePlaceholder(inputRT, textComp.font);
            _inputText.lineType = InputField.LineType.SingleLine;
            _inputText.onEndEdit.AddListener(OnEndEditText);

            var btnGO = new GameObject("SendButton", typeof(RectTransform));
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.SetParent(rt, false);
            btnRT.anchorMin = new Vector2(0.88f, 0);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.offsetMin = new Vector2(8, 6);
            btnRT.offsetMax = new Vector2(-8, -6);

            _btnSend = btnGO.AddComponent<Button>();
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            _btnSend.targetGraphic = btnImg;
            var btnTextGO = new GameObject("Text", typeof(RectTransform));
            var btnTextRT = btnTextGO.GetComponent<RectTransform>();
            btnTextRT.SetParent(btnRT, false);
            btnTextRT.anchorMin = btnTextRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnTextRT.sizeDelta = new Vector2(60, 30);
            var btnText = btnTextGO.AddComponent<Text>();
            btnText.font = textComp.font;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = "Send";
            _btnSend.onClick.AddListener(OnClickSendText);

            var micGO = new GameObject("MicButton", typeof(RectTransform));
            var micRT = micGO.GetComponent<RectTransform>();
            micRT.SetParent(rt, false);
            micRT.anchorMin = new Vector2(0.8f, 0);
            micRT.anchorMax = new Vector2(0.88f, 1);
            micRT.offsetMin = new Vector2(4, 6);
            micRT.offsetMax = new Vector2(-4, -6);

            _btnMic = micGO.AddComponent<Button>();
            var micImg = micGO.AddComponent<Image>();
            micImg.color = new Color(0.1f, 0.4f, 0.8f, 0.9f);
            _btnMic.targetGraphic = micImg;
            var micTextGO = new GameObject("Text", typeof(RectTransform));
            var micTextRT = micTextGO.GetComponent<RectTransform>();
            micTextRT.SetParent(micRT, false);
            micTextRT.anchorMin = micTextRT.anchorMax = new Vector2(0.5f, 0.5f);
            micTextRT.sizeDelta = new Vector2(40, 30);
            var micText = micTextGO.AddComponent<Text>();
            micText.font = textComp.font;
            micText.color = Color.white;
            micText.alignment = TextAnchor.MiddleCenter;
            micText.text = "\uD83C\uDF99"; // mic emoji
            _btnMic.onClick.AddListener(OnClickMic);
        }

        private Text CreatePlaceholder(RectTransform parent, Font font)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 0);
            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.text = "Nhập tin nhắn...";
            txt.color = new Color(1, 1, 1, 0.6f);
            txt.alignment = TextAnchor.UpperLeft;
            return txt;
        }

        private void OnClickSendText()
        {
            SendTextMessage(_inputText?.text);
        }

        private void OnClickMic()
        {
            Context.App.EnsureListening().Forget();
        }

        private void OnEndEditText(string text)
        {
            SendTextMessage(text);
        }

        private void SendTextMessage(string text)
        {
            var msg = text?.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            _inputText.text = string.Empty;
            Context.App.SendTextMessage(msg).Forget();
        }

        private void OnTextInputToggle(bool enabled)
        {
            if (_textInputRoot) _textInputRoot.SetActive(enabled);
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
            UpdateCompVisible(Context.App.Talk.IsReady() && !AppSettings.Instance.IsAutoHideUI(), instant);
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

        private void StartBreathingAnimation()
        {
            if (_breatheSequence != null) return;
            _breatheSequence = DOTween.Sequence();
            _breatheSequence.Append(_trEmotion.DOScale(1.1f, 2f).SetEase(Ease.InOutSine))
                .Append(_trEmotion.DOScale(1f, 2f).SetEase(Ease.InOutSine))
                .SetLoops(-1);
        }

        private void StopBreathingAnimation()
        {
            if (_breatheSequence == null) return;
            _breatheSequence.Kill();
            _breatheSequence = null;
            _trEmotion.localScale = Vector3.one;
        }

        private void PlayWakeUpAnimation()
        {
            StopWakeUpAnimation();
            _wakeUpSequence = DOTween.Sequence();
            _wakeUpSequence.Append(_trEmotion.DOLocalRotate(new Vector3(0, 0, 5), 0.1f).SetEase(Ease.OutSine))
                .Append(_trEmotion.DOLocalRotate(new Vector3(0, 0, -5), 0.1f).SetEase(Ease.InOutSine))
                .Append(_trEmotion.DOLocalRotate(new Vector3(0, 0, 3), 0.1f).SetEase(Ease.InOutSine))
                .Append(_trEmotion.DOLocalRotate(new Vector3(0, 0, -2), 0.1f).SetEase(Ease.InOutSine))
                .Append(_trEmotion.DOLocalRotate(Vector3.zero, 0.1f).SetEase(Ease.OutSine));
        }

        private void StopWakeUpAnimation()
        {
            if (_wakeUpSequence == null) return;
            _wakeUpSequence.Kill();
            _wakeUpSequence = null;
            _trEmotion.localRotation = Quaternion.identity;
        }
    }
}
