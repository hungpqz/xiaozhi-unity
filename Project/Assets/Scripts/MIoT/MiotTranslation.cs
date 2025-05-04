using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.Localization.Settings;

namespace XiaoZhi.Unity.MIoT
{
    public static class MiotTranslation
    {
        private const string FilePath = "Assets/Settings/MIoT/translation_languages.json";

        private static readonly Dictionary<string, Dictionary<string, object>> Lang = new();

        public static string Language
        {
            get
            {
                var code = LocalizationSettings.SelectedLocale.Identifier.Code;
                return code switch
                {
                    "zh-Hans" => "cn",
                    "en" => "en",
                    _ => code
                };
            }
        }

        public static Dictionary<string, object> Get(string language = null)
        {
            language ??= Language;
            if (Lang.TryGetValue(language, out var translations))
                return translations;
            translations = new Dictionary<string, object>();
            var text = FileUtility.ReadAllText(FileUtility.FileType.Addressable, FilePath);
            var root = JObject.Parse(text);
            var lang = root.Value<JObject>(language);
            if (lang != null)
            {
                foreach (var prop in lang.Properties())
                {
                    translations[prop.Name] = prop.Value is JObject nestedObj
                        ? nestedObj.Properties().ToDictionary(i => i.Name, i => i.Value<string>())
                        : prop.Value<string>();
                }
            }

            Lang[language] = translations;
            return translations;
        }
    }
}