using UnityEngine;
using System;
using System.Linq;
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

        [Serializable]
        public class Keyword
        {
            [SerializeField] private string _localeCode;
            [SerializeField] private string _spotterModelConfigTransducerEncoder;
            [SerializeField] private string _spotterModelConfigTransducerDecoder;
            [SerializeField] private string _spotterModelConfigTransducerJoiner;
            [SerializeField] private string _spotterModelConfigToken;
            [SerializeField] private string _spotterKeyWordsFile;
            [SerializeField] private int _spotterModelConfigNumThreads;

            public string Name => _localeCode;
            public string LocaleCode => _localeCode;
            public string SpotterModelConfigTransducerEncoder => _spotterModelConfigTransducerEncoder;
            public string SpotterModelConfigTransducerDecoder => _spotterModelConfigTransducerDecoder;
            public string SpotterModelConfigTransducerJoiner => _spotterModelConfigTransducerJoiner;
            public string SpotterModelConfigToken => _spotterModelConfigToken;
            public string SpotterKeyWordsFile => _spotterKeyWordsFile;
            public int SpotterModelConfigNumThreads => _spotterModelConfigNumThreads;
        }

        [Serializable]
        public class Video
        {
            [SerializeField] private string _name;
            [SerializeField] private string _path;
            
            public string Name => _name;
            public string Path => _path;
        }
        
        [Serializable]
        public class Wallpaper
        {
            [SerializeField] private WallpaperType _type;
            [SerializeField] private string _name;
            [SerializeField] private string _path;
            
            public WallpaperType Type => _type;
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
        [SerializeField] private Keyword[] _keyWords;
        [SerializeField] private string _vadModelConfig;
        [SerializeField] private string _activationURL;
        [SerializeField] private VRMModel[] _vrmCharacterModels;
        [SerializeField] private Video[] _videos;
        [SerializeField] private Wallpaper[] _wallpapers;

        public string WebSocketUrl => _webSocketUrl;
        public string WebSocketAccessToken => _webSocketAccessToken;
        public int OpusFrameDurationMs => _opusFrameDurationMs;
        public int AudioInputSampleRate => _audioInputSampleRate;
        public int AudioOutputSampleRate => _audioOutputSampleRate;
        public int ServerInputSampleRate => _serverInputSampleRate;
        public bool EnableWakeService => _enableWakeService;
        public string OtaVersionUrl => _otaVersionUrl;
        public string VadModelConfig => _vadModelConfig;
        public string ActivationURL => _activationURL;
        public VRMModel[] VRMCharacterModels => _vrmCharacterModels;
        public Video[] Videos => _videos;
        public Video GetVideo(string videoName) => _videos.FirstOrDefault(k => k.Name == videoName);
        public Wallpaper[] Wallpapers => _wallpapers;
        public Wallpaper GetWallpaper(string paperName = "Default") => _wallpapers.FirstOrDefault(k => k.Name == paperName);
        public Keyword GetKeyword(string localeCode) => _keyWords.FirstOrDefault(k => k.LocaleCode == localeCode);
        public static AppPresets Instance { get; private set; }
        
        public static async UniTask Load()
        {
            const string path = "Assets/Settings/AppPresets.asset";
            Instance = await Addressables.LoadAssetAsync<AppPresets>(path);
        }
    }
}