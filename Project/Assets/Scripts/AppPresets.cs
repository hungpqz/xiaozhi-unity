using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace XiaoZhi.Unity
{
    [Serializable]
    [CreateAssetMenu(menuName = "App Presets")]
    public class AppPresets : ScriptableObject
    {
        [Serializable]
        public class VRMModel
        {
            [SerializeField] private string _name;
            [SerializeField] private string _path;
            
            public string Name => _name;
            public string Path => _path;
        }
        
        [SerializeField] private string _webSocketUrl;
        [SerializeField] private string _webSocketAccessToken;
        [SerializeField] private int _opusFrameDurationMs;
        [SerializeField] private int _audioInputSampleRate;
        [SerializeField] private int _audioOutputSampleRate;
        [SerializeField] private int _serverInputSampleRate;
        [SerializeField] private bool _enableWakeService;
        [SerializeField] private string _otaVersionUrl;
        [SerializeField] private string _keyWordSpotterModelConfigTransducerEncoder;
        [SerializeField] private string _keyWordSpotterModelConfigTransducerDecoder;
        [SerializeField] private string _keyWordSpotterModelConfigTransducerJoiner;
        [SerializeField] private string _keyWordSpotterModelConfigToken;
        [SerializeField] private int _keyWordSpotterModelConfigNumThreads;
        [SerializeField] private string _keyWordSpotterKeyWordsFile;
        [SerializeField] private string _vadModelConfig;
        [SerializeField] private string _activationURL;
        [SerializeField] private VRMModel[] _vrmCharacterModels;

        public string WebSocketUrl => _webSocketUrl;
        public string WebSocketAccessToken => _webSocketAccessToken;
        public int OpusFrameDurationMs => _opusFrameDurationMs;
        public int AudioInputSampleRate => _audioInputSampleRate;
        public int AudioOutputSampleRate => _audioOutputSampleRate;
        public int ServerInputSampleRate => _serverInputSampleRate;
        public bool EnableWakeService => _enableWakeService;
        public string OtaVersionUrl => _otaVersionUrl;
        public string KeyWordSpotterModelConfigTransducerEncoder => _keyWordSpotterModelConfigTransducerEncoder;
        public string KeyWordSpotterModelConfigTransducerDecoder => _keyWordSpotterModelConfigTransducerDecoder;
        public string KeyWordSpotterModelConfigTransducerJoiner => _keyWordSpotterModelConfigTransducerJoiner;
        public string KeyWordSpotterModelConfigToken => _keyWordSpotterModelConfigToken;
        public int KeyWordSpotterModelConfigNumThreads => _keyWordSpotterModelConfigNumThreads;
        public string KeyWordSpotterKeyWordsFile => _keyWordSpotterKeyWordsFile;
        public string VadModelConfig => _vadModelConfig;
        public string ActivationURL => _activationURL;
        public VRMModel[] VRMCharacterModels => _vrmCharacterModels;
        
        public static AppPresets Instance { get; private set; }
        
        public static async UniTask Load()
        {
            const string path = "Assets/Settings/AppPresets.asset";
            Instance = await Addressables.LoadAssetAsync<AppPresets>(path);
        }
    }
}