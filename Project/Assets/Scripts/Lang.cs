using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace XiaoZhi.Unity
{
    public static class Lang
    {
        private static StringTable _tableRef;
        
        public static async UniTask LoadLocale(Locale locale = null)
        {
            _tableRef = await LocalizationSettings.StringDatabase.GetTableAsync("Lang", locale);
        }

        public static string Get(string key, params object[] args)
        {
            var entry = _tableRef.GetEntry(key);
            return entry.GetLocalizedString(args);
        }
    }
    
    public class StartupLocaleSelector: IStartupLocaleSelector
    {
        private const string DefaultCode = "zh-Hans";
        
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            var localeCode = AppSettings.Instance.GetLangCode();
            return availableLocales.GetLocale(string.IsNullOrEmpty(localeCode) ? DefaultCode : localeCode);
        }
    }
}