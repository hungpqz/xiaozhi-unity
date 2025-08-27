using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.LowLevel;
using UniVRM10;
using Object = UnityEngine.Object;

namespace XiaoZhi.Unity
{
    public class VRMDisplay : IDisplay
    {
        private static readonly int AnimTalkingHash = Animator.StringToHash("talking");
        
        private readonly Context _context;
        private VRMMainUI _mainUI;
        private Camera _mainCamera;
        private GameObject _character;
        private uLipSync.uLipSync _lipSync;
        private ULipSyncAudioProxy _lipSyncAudioProxy;
        private Vrm10Instance _vrmInstance;
        private FaceAnimation _faceAnim;
        private Animator _animator;
        private CancellationTokenSource _loopCts;
        private string _emotion;

        public VRMDisplay(Context context)
        {
            _context = context;
            ThemeManager.OnThemeChanged.AddListener(OnThemeChanged);
        }

        public void Dispose()
        {
            ThemeManager.OnThemeChanged.RemoveListener(OnThemeChanged);
            if (_loopCts != null)
            {
                _loopCts.Cancel();
                _loopCts.Dispose();
                _loopCts = null;
            }

            _mainUI?.Dispose();
            _mainUI = null;
            if (_lipSyncAudioProxy != null)
            {
                _lipSyncAudioProxy.Dispose();
                _lipSyncAudioProxy = null;
            }

            if (_character)
            {
                Object.Destroy(_character);
                _character = null;
            }
        }

        public async UniTask<bool> Load()
        {
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

            _character.GetComponent<TransformFollower>().SetFollower(_mainCamera);
            _lipSync = _character.GetComponent<uLipSync.uLipSync>();
            _vrmInstance = _character.GetComponent<Vrm10Instance>();
            _faceAnim = new FaceAnimation(_vrmInstance, "loading");
            _animator = _character.GetComponent<Animator>();
            return true;
        }

        public void Start()
        {
            _lipSyncAudioProxy = new ULipSyncAudioProxy(_lipSync, _context.App.GetCodec());
            _lipSyncAudioProxy.Start();
            _loopCts = new CancellationTokenSource();
            UniTask.Void(LoopUpdate, _loopCts.Token);
            _context.App.OnDeviceStateUpdate -= OnDeviceStateUpdate;
            _context.App.OnDeviceStateUpdate += OnDeviceStateUpdate;
        }

        public void SetStatus(string status)
        {
            _mainUI.SetStatus(status);
        }

        public void SetStatus(LocalizedString status)
        {
            _mainUI.SetStatus(status);
        }

        public void SetEmotion(string emotion)
        {
            _emotion = emotion;
            _mainUI.ShowLoading(_emotion == "loading");
            _faceAnim.SetExpression(_emotion);
        }

        public void SetChatMessage(ChatRole role, string content)
        {
            if (_emotion == "activation") _mainUI.SetActivateLink(content);
        }

        public void SetChatMessage(ChatRole role, LocalizedString content)
        {
            throw new NotSupportedException();
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
                _faceAnim.Update(Time.deltaTime);
            }
        }
        
        private void OnDeviceStateUpdate(DeviceState state)
        {
            _animator.SetBool(AnimTalkingHash, state == DeviceState.Speaking);
        }

        private class FaceAnimation
        {
            private static readonly Dictionary<string, ExpressionKey> ExpressionMap = new()
            {
                { "loading", new ExpressionKey(ExpressionPreset.custom, "sleeping") },
                { "sleep", new ExpressionKey(ExpressionPreset.custom, "sleeping") },
                { "yawn", new ExpressionKey(ExpressionPreset.custom, "sleeping") },
                { "activation", new ExpressionKey(ExpressionPreset.custom, "sleeping") },
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