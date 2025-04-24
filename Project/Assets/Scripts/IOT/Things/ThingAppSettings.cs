using System;
using System.Linq;
using UnityEngine.Localization.Settings;

namespace XiaoZhi.Unity.IOT
{
    public class ThingAppSettings : Thing
    {
        public ThingAppSettings() : base("AppSettings", "设置中心，可以设置主题/音量/语言等")
        {
            properties.AddProperty("theme", "主题", GetTheme);
            methods.AddMethod("SetTheme", "设置主题",
                new ParameterList(new[]
                {
                    new Parameter<string>("theme", "主题模式, Light 或 Dark")
                }),
                SetTheme);

            properties.AddProperty("volume", "当前音量值", GetVolume);
            methods.AddMethod("SetVolume", "设置音量",
                new ParameterList(new[]
                {
                    new Parameter<int>("volume", "0到100之间的整数")
                }),
                SetVolume);

            properties.AddProperty("lang", "语言", GetLang);
            methods.AddMethod("SetLang", "设置语言",
                new ParameterList(new[]
                {
                    new Parameter<string>("lang", "语言, 简体中文 或 English")
                }),
                SetLang);
        }
        
        private string GetTheme()
        {
            return ThemeManager.Theme.ToString();
        }
        
        private void SetTheme(ParameterList parameters)
        {
            ThemeManager.SetTheme(Enum.Parse<ThemeSettings.Theme>(parameters.GetValue<string>("theme")));
        }
        
        private int GetVolume()
        {
            return AppSettings.Instance.GetOutputVolume();
        }
        
        private void SetVolume(ParameterList parameters)
        {
            AppSettings.Instance.SetOutputVolume(parameters.GetValue<int>("volume"));
        }

        private string GetLang()
        {
            return LocalizationSettings.SelectedLocale.LocaleName;
        }
        
        private void SetLang(ParameterList parameters)
        {
            var lang = parameters.GetValue<string>("lang");
            var locale = LocalizationSettings.AvailableLocales.Locales.FirstOrDefault(i => i.LocaleName == lang);
            if (locale) LocalizationSettings.SelectedLocale = locale;
        }
    }
}