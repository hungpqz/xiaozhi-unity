using System;
using UnityEngine;

namespace XiaoZhi.Unity
{
    public class AppSettings : Settings
    {
        private static AppSettings _instance;

        public static AppSettings Instance => _instance;

        public static void Load()
        {
            _instance = new AppSettings();
        }

        private DisplayMode _displayMode;

        private int _vrmModel;

        private ZoomMode _zoomMode;

        private BreakMode _breakMode;

        private string _keywords;

        private string _wallpaper;

        private bool _autoHideUI;

        private int _outputVolume;

        private string _webSocketUrl;

        private string _webSocketAccessToken;

        private string _customMacAddress;
        private bool _enableTextInput;

        public event Action<bool> OnAutoHideUIUpdate;
        public event Action<int> OnOutputVolumeUpdate;

        public event Action<ZoomMode> OnZoomModeUpdate;

        public event Action<string> OnWallPaperUpdate;
        public event Action<bool> OnTextInputEnableUpdate;

        private AppSettings() : base("app")
        {
            _displayMode = (DisplayMode)GetInt("display_mode");
            _breakMode = (BreakMode)GetInt("break_mode", (int)BreakMode.Keyword);
            _autoHideUI = GetInt("auto_hide_ui", 1) == 1;
            _outputVolume = GetInt("output_volume", 50);
            _vrmModel = GetInt("vrm_model");
            _zoomMode = (ZoomMode)GetInt("zoom_mode");
            _wallpaper = GetString("wallpaper", "Default");
            _enableTextInput = GetInt("enable_text_input", 0) == 1;
        }

        public DisplayMode GetDisplayMode() => _displayMode;

        public void SetDisplayMode(DisplayMode displayMode)
        {
            if (_displayMode == displayMode) return;
            _displayMode = displayMode;
            SetInt("display_mode", (int)displayMode);
            Save();
        }

        public AppPresets.VRMModel GetVRMModelPreset() =>
            AppPresets.Instance.VRMCharacterModels[
                Mathf.Clamp(_vrmModel, 0, AppPresets.Instance.VRMCharacterModels.Length - 1)];
        
        public int GetVRMModel() => _vrmModel;

        public void SetVRMModel(int vrmModel)
        {
            if (_vrmModel == vrmModel) return;
            _vrmModel = vrmModel;
            SetInt("vrm_model", _vrmModel);
            Save();
        }

        public ZoomMode GetZoomMode() => _zoomMode;

        public void SetZoomMode(ZoomMode zoomMode)
        {
            if (_zoomMode == zoomMode) return;
            _zoomMode = zoomMode;
            SetInt("zoom_mode", (int)zoomMode);
            Save();
            OnZoomModeUpdate?.Invoke(_zoomMode);
        }

        public BreakMode GetBreakMode() => _breakMode;

        public void SetBreakMode(BreakMode breakMode)
        {
            if (_breakMode == breakMode) return;
            _breakMode = breakMode;
            SetInt("break_mode", (int)breakMode);
            Save();
        }

        public string GetWallpaper() => _wallpaper;

        public void SetWallpaper(string wallpaper)
        {
            if (_wallpaper == wallpaper) return;
            _wallpaper = wallpaper;
            SetString("wallpaper", _wallpaper);
            Save();
            OnWallPaperUpdate?.Invoke(_wallpaper);
        }

        public string GetKeywords(bool forceUpdate = false)
        {
            if (forceUpdate) _keywords = null;
            _keywords ??= LoadOrRestoreKeywords();
            return _keywords;
        }

        public void SetKeywords(string keywords)
        {
            var current = _keywords ?? string.Empty;
            if (string.Equals(current, keywords, StringComparison.Ordinal)) return;

            var newValue = SanitizeKeywords(keywords);
            if (!IsValidKeywords(newValue))
            {
                Debug.LogWarning("Invalid wake word input, restoring default keywords.");
                newValue = LoadDefaultKeywords();
            }

            _keywords = newValue;
            FileUtility.WriteAllText(AppPresets.Instance.GetKeyword(Lang.Code).SpotterKeyWordsFile, _keywords);
        }

        private string LoadOrRestoreKeywords()
        {
            var preset = AppPresets.Instance.GetKeyword(Lang.Code);
            var path = preset.SpotterKeyWordsFile;
            if (FileUtility.FileExists(FileUtility.FileType.DataPath, path))
            {
                try
                {
                    var content = FileUtility.ReadAllText(FileUtility.FileType.DataPath, path);
                    if (IsValidKeywords(content)) return content;
                    Debug.LogWarning("Cached wake word file invalid, restoring default.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to read cached wake word file: {ex.Message}");
                }
            }

            var defaults = LoadDefaultKeywords();
            FileUtility.WriteAllText(path, defaults);
            return defaults;
        }

        private string LoadDefaultKeywords()
        {
            try
            {
                var preset = AppPresets.Instance.GetKeyword(Lang.Code);
                return FileUtility.ReadAllText(FileUtility.FileType.StreamingAssets, preset.SpotterKeyWordsFile);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load default wake words, using fallback. {ex.Message}");
                return "▁HI ▁BI ▁WELL";
            }
        }

        private static string SanitizeKeywords(string keywords)
        {
            return keywords?.Trim() ?? string.Empty;
        }

        private static bool IsValidKeywords(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords)) return false;
            if (keywords.Length > 256) return false;
            var lines = keywords.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || lines.Length > 10) return false;
            foreach (var line in lines)
            {
                // Allow letters, numbers, spaces, underscore token markers (▁), and basic punctuation.
                const string pattern = @"^[\p{L}\p{N}\p{Zs}_\.\-'\u2581]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(line, pattern))
                    return false;
            }

            return true;
        }

        public bool IsAutoHideUI()
        {
            return _autoHideUI;
        }

        public void SetAutoHideUI(bool autoHideUI)
        {
            if (_autoHideUI == autoHideUI) return;
            _autoHideUI = autoHideUI;
            SetInt("auto_hide_ui", _autoHideUI ? 1 : 0);
            Save();
            OnAutoHideUIUpdate?.Invoke(_autoHideUI);
        }

        public bool IsTextInputEnabled() => _enableTextInput;

        public void SetTextInputEnabled(bool enabled)
        {
            if (_enableTextInput == enabled) return;
            _enableTextInput = enabled;
            SetInt("enable_text_input", enabled ? 1 : 0);
            Save();
            OnTextInputEnableUpdate?.Invoke(_enableTextInput);
        }

        public int GetOutputVolume()
        {
            return _outputVolume;
        }

        public void SetOutputVolume(int outputVolume)
        {
            if (_outputVolume == outputVolume) return;
            _outputVolume = outputVolume;
            SetInt("output_volume", _outputVolume);
            Save();
            OnOutputVolumeUpdate?.Invoke(_outputVolume);
        }

        public string GetWebSocketUrl()
        {
            _webSocketUrl ??= GetString("web_socket_url", AppPresets.Instance.WebSocketUrl);
            return _webSocketUrl;
        }

        public void SetWebSocketUrl(string url)
        {
            if (_webSocketUrl.Equals(url)) return;
            _webSocketUrl = url;
            SetString("web_socket_url", _webSocketUrl);
            Save();
        }

        public string GetWebSocketAccessToken()
        {
            _webSocketAccessToken ??= GetString("web_socket_access_token", AppPresets.Instance.WebSocketAccessToken);
            return _webSocketAccessToken;
        }

        public void SetWebSocketAccessToken(string accessToken)
        {
            if (_webSocketAccessToken.Equals(accessToken)) return;
            _webSocketAccessToken = accessToken;
            SetString("web_socket_access_token", _webSocketAccessToken);
            Save();
        }

        public string GetMacAddress()
        {
            _customMacAddress ??= GetString("custom_mac_address", AppUtility.GetMacAddress());
            return _customMacAddress;
        }

        public void SetMacAddress(string macAddress)
        {
            if (_customMacAddress.Equals(macAddress)) return;
            _customMacAddress = macAddress;
            SetString("custom_mac_address", _customMacAddress);
            Save();
        }
    }
}
