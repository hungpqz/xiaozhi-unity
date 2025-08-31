using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UniVRM10;
using Object = UnityEngine.Object;

namespace XiaoZhi.Unity
{
    public class VRMDisplay : IDisplay
    {
        private static float ZoomMode2Gap(ZoomMode d) => d switch
        {
            ZoomMode.MediumShot => 0,
            ZoomMode.LongShot => 0.5f,
            ZoomMode.CloseShot => -0.5f,
            ZoomMode.CloseUp => -0.7f,
            _ => 0
        };

        private readonly Context _context;
        private WallpaperUI _wallpaperUI;
        private VRMMainUI _mainUI;
        private Camera _mainCamera;
        private GameObject _character;
        private uLipSync.uLipSync _lipSync;
        private ULipSyncAudioProxy _lipSyncAudioProxy;
        private Vrm10Instance _vrmInstance;
        private FaceAnimation _faceAnim;
        private AnimationCtrl _animCtrl;
        private CancellationTokenSource _loopCts;
        private TransformFollower _follower;
        private bool _visible = true;

        public VRMDisplay(Context context)
        {
            _context = context;
            ThemeManager.OnThemeChanged.AddListener(OnThemeChanged);
        }

        public void Dispose()
        {
            _context.App.Talk.OnEmotionUpdate -= OnEmotionUpdate;
            AppSettings.Instance.OnZoomModeUpdate -= OnZoomModeUpdate;
            ThemeManager.OnThemeChanged.RemoveListener(OnThemeChanged);
            if (_loopCts != null)
            {
                _loopCts.Cancel();
                _loopCts.Dispose();
                _loopCts = null;
            }

            _mainUI?.Dispose();
            _mainUI = null;
            _wallpaperUI?.Dispose();
            _wallpaperUI = null;
            if (_character)
            {
                Object.Destroy(_character);
                _character = null;
            }
        }

        public async UniTask<bool> Load()
        {
            _wallpaperUI = await _context.UIManager.ShowBgUI<WallpaperUI>();
            _mainUI = await _context.UIManager.ShowSceneUI<VRMMainUI>();
            _mainCamera = Camera.main;
            UpdateCameraColor();
            var models = AppPresets.Instance.VRMCharacterModels;
            var modelIndex = Mathf.Clamp(AppSettings.Instance.GetVRMModel(), 0, models.Length - 1);
            var modelPath = models[modelIndex].Path;
            _character = await Addressables.InstantiateAsync(modelPath);
            if (!_character)
            {
                _context.UIManager.ShowNotificationUI($"Load character failed: {modelPath}").Forget();
                return false;
            }

            _follower = _character.GetComponent<TransformFollower>();
            _follower.SetFollower(_mainCamera);
            OnZoomModeUpdate(AppSettings.Instance.GetZoomMode());
            _lipSync = _character.GetComponent<uLipSync.uLipSync>();
            _vrmInstance = _character.GetComponent<Vrm10Instance>();
            _faceAnim = new FaceAnimation(_vrmInstance, "sleep");
            _animCtrl = new AnimationCtrl(_character.GetComponent<Animator>(), AppPresets.Instance.GetAnimationLib(),
                _context.App.Talk);
            return true;
        }

        public void Start()
        {
            _lipSyncAudioProxy = new ULipSyncAudioProxy(_lipSync, _context.App.GetCodec());
            _loopCts = new CancellationTokenSource();
            UniTask.Void(LoopUpdate, _loopCts.Token);
            _context.App.Talk.OnEmotionUpdate -= OnEmotionUpdate;
            _context.App.Talk.OnEmotionUpdate += OnEmotionUpdate;
            AppSettings.Instance.OnZoomModeUpdate -= OnZoomModeUpdate;
            AppSettings.Instance.OnZoomModeUpdate += OnZoomModeUpdate;
        }

        public async UniTask Show()
        {
            _visible = true;
            _character.SetActive(true);
            await _wallpaperUI.Show();
            await _mainUI.Show();
        }

        public async UniTask Hide()
        {
            _visible = false;
            _character.SetActive(false);
            await _mainUI.Hide();
            await _wallpaperUI.Hide();
        }

        public void Animate(params string[] labels)
        {
            _animCtrl.Animate(labels);
        }

        private void OnThemeChanged(ThemeSettings.Theme theme)
        {
            UpdateCameraColor();
        }

        private void UpdateCameraColor()
        {
            if (!_mainCamera) return;
            var color = ThemeManager.FetchColor(ThemeManager.Theme);
            _mainCamera.backgroundColor = color;
        }

        private async UniTaskVoid LoopUpdate(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                if (!_visible) continue;
                _faceAnim.Update(Time.deltaTime);
                _lipSyncAudioProxy.Update();
            }
        }
        
        private void OnEmotionUpdate(string emotion)
        {
            _faceAnim.SetExpression(emotion);
        }

        private void OnZoomModeUpdate(ZoomMode mode)
        {
            _follower.SetZoomGap(ZoomMode2Gap(mode));
        }

        private class FaceAnimation
        {
            private static readonly Dictionary<string, ExpressionKey> ExpressionMap = new()
            {
                { "sleep", new ExpressionKey(ExpressionPreset.custom, "sleeping") },
                { "neutral", new ExpressionKey(ExpressionPreset.neutral) },
                { "happy", new ExpressionKey(ExpressionPreset.relaxed) },
                { "funny", new ExpressionKey(ExpressionPreset.relaxed) },
                { "sad", new ExpressionKey(ExpressionPreset.sad) },
                { "thinking", new ExpressionKey(ExpressionPreset.custom, "thinking") },
            };

            private const float CrossFadeDuration = 0.3f;

            private readonly Vrm10Instance _instance;

            private ExpressionKey _current;

            private readonly List<CrossFader> _faders = new();

            public FaceAnimation(Vrm10Instance instance, string initialExpression)
            {
                _instance = instance;
                _current = ExpressionMap.GetValueOrDefault(initialExpression, ExpressionMap["neutral"]);
                _instance.Runtime.Expression.SetWeight(_current, 1.0f);
            }

            public void SetExpression(string expression)
            {
                Debug.Log($"SetExpression: {expression}");
                var newKey = ExpressionMap.GetValueOrDefault(expression, ExpressionMap["neutral"]);
                if (_current.Equals(newKey)) return;
                for (var i = _faders.Count - 1; i >= 0; i--)
                    if (_faders[i].From < _faders[i].To)
                        _faders[i] = _faders[i].Cancel();
                _faders.Add(new CrossFader(_current, 1, 0, CrossFadeDuration));
                _current = newKey;
                _faders.Add(new CrossFader(_current, 0, 1, CrossFadeDuration));
            }

            public void Update(float deltaTime)
            {
                foreach (var fade in _faders)
                {
                    fade.Update(deltaTime);
                    _instance.Runtime.Expression.SetWeight(fade.Key, fade.Weight);
                }

                for (var i = _faders.Count - 1; i >= 0; i--)
                    if (_faders[i].IsEnd())
                        _faders.RemoveAt(i);
            }

            private class CrossFader
            {
                private readonly ExpressionKey _key;
                public ExpressionKey Key => _key;
                private readonly float _from;
                public float From => _from;
                private readonly float _to;
                public float To => _to;
                private float _crossTime;
                private float _crossDuration;
                public bool IsEnd() => _crossDuration == 0;
                private float _weight;
                public float Weight => _weight;

                public CrossFader(ExpressionKey key, float from, float to, float crossDuration)
                {
                    _key = key;
                    _from = from;
                    _to = to;
                    _crossDuration = crossDuration;
                    _crossTime = 0;
                    _weight = _from;
                }

                public void Update(float deltaTime)
                {
                    if (IsEnd()) return;
                    _crossTime += deltaTime;
                    if (_crossTime >= _crossDuration)
                    {
                        _weight = _to;
                        _crossDuration = 0;
                    }
                    else
                    {
                        _weight = Mathf.Lerp(_from, _to, _crossTime / _crossDuration);
                    }
                }

                public CrossFader Cancel()
                {
                    return new CrossFader(_key, _weight, 0, (_weight - _from) / (_to - _from) * _crossDuration);
                }
            }
        }
    }
}