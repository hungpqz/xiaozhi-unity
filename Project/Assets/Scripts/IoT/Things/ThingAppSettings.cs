using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Settings;

namespace XiaoZhi.Unity.IoT
{
    public class ThingAppSettings : Thing
    {
        public ThingAppSettings() : base("AppSettings", "Trung tâm cài đặt, cấu hình chủ đề/âm lượng/ngôn ngữ...")
        {
        }

        public override async UniTask Load()
        {
            _properties.AddProperty("theme", "Chủ đề", GetTheme);
            _methods.AddMethod("SetTheme", "Thiết lập chủ đề",
                new ParameterList(new[]
                {
                    new Parameter<string>("theme", "Chế độ chủ đề, Light hoặc Dark")
                }),
                SetTheme);
            _properties.AddProperty("volume", "Giá trị âm lượng hiện tại", GetVolume);
            _methods.AddMethod("SetVolume", "Thiết lập âm lượng",
                new ParameterList(new[]
                {
                    new Parameter<int>("volume", "Số nguyên từ 0 đến 100")
                }),
                SetVolume);
            _properties.AddProperty("lang", "Ngôn ngữ", GetLang);
            _methods.AddMethod("SetLang", "Thiết lập ngôn ngữ",
                new ParameterList(new[]
                {
                    new Parameter<string>("lang", "Ngôn ngữ, Tiếng Việt hoặc English")
                }),
                SetLang);
            _properties.AddProperty("zoom", "Độ thu phóng ống kính", GetZoom);
            _methods.AddMethod("SetZoom", "Thiết lập độ thu phóng ống kính",
                new ParameterList(new[]
                {
                    new Parameter<string>("zoom", "Độ thu phóng ống kính, " + string.Join(" hoặc ", Enum.GetNames(typeof(ZoomMode))))
                }),
                SetZoom);
            var wallpaperNames = "Tên hình nền, " + string.Join(" hoặc ", AppPresets.Instance.Wallpapers.Select(i => i.Name));
            _methods.AddMethod("SetWallpaper", "Thiết lập hình nền",
                new ParameterList(new[]
                {
                    new Parameter<string>("wallpaperName", wallpaperNames)
                }),
                SetWallpaper);
            await base.Load();
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

        private string GetZoom()
        {
            return AppSettings.Instance.GetZoomMode().ToString();
        }

        private void SetZoom(ParameterList parameters)
        {
            AppSettings.Instance.SetZoomMode(Enum.Parse<ZoomMode>(parameters.GetValue<string>("zoom")));
        }

        private void SetWallpaper(ParameterList parameters)
        {
            AppSettings.Instance.SetWallpaper(parameters.GetValue<string>("wallpaperName"));
        }
    }
}
