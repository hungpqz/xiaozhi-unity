using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using XiaoZhi.Unity;
using XiaoZhi.Unity.MIOT;

namespace MIoT
{
    public class MiotObject
    {
        private const string Domain = "miot";

        private static readonly string[] Hosts =
        {
            "https://miot-spec.org",
            "https://spec.miot-spec.com"
        };

        private static readonly Dictionary<string, string> ModelTypeMap = new();

        public static async UniTask<JObject> AsyncFromModel(string model, bool useRemote = false)
        {
            var type = await AsyncGetModelType(model, useRemote);
            return await AsyncFromType(type);
        }

        public static async UniTask<string> AsyncGetModelType(string model, bool useRemote = false)
        {
            if (string.IsNullOrEmpty(model)) return null;
            string type = null;
            var filePath = $"{Domain}/instances.json";
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            JObject data = null;
            if (!useRemote)
            {
                if (ModelTypeMap.TryGetValue(model, out type)) return type;
                if (FileUtility.FileExists(FileUtility.FileType.DataPath, filePath))
                {
                    try
                    {
                        var json = await FileUtility.ReadAllTextAsync(FileUtility.FileType.DataPath, filePath);
                        data = JObject.Parse(json);
                        var ptm = data.Value<int>("_updated_time");
                        data.Remove("_updated_time");
                        if (data.Count > 0 && now - ptm > 86400 * 7) data = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to load cached instances: {ex}");
                    }
                }
            }

            if (data == null)
            {
                try
                {
                    const string url = "/miot-spec-v2/instances?status=all";
                    var (success, json) = await AsyncDownloadMiotSpec(url, 3, 90);
                    if (!success) return null;
                    var sdt = new JObject
                    {
                        ["_updated_time"] = now
                    };
                    data = JObject.Parse(json);
                    if (data.TryGetValue("instances", out var instances))
                    {
                        foreach (var jToken in instances)
                        {
                            var v = (JObject)jToken;
                            var m = v.Value<string>("model");
                            if (string.IsNullOrEmpty(m)) continue;
                            var o = sdt.Value<JObject>(m);
                            if (o != null)
                            {
                                if (o.Value<string>("status") == "released" &&
                                    v.Value<string>("status") != "released")
                                    continue;
                                if (v.Value<int>("version") < o.Value<int>("version"))
                                    continue;
                            }

                            v.Remove("model");
                            sdt[m] = v;
                        }
                    }

                    await FileUtility.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(sdt));
                    data = sdt;
                    Debug.Log($"Renew miot spec instances: {filePath}, count: {sdt.Count - 1}, model: {model}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Get miot specs failed: {ex}");
                }
            }

            if (data != null)
            {
                if (data.TryGetValue("instances", out var iss))
                {
                    type = (from v in iss where v.Value<string>("model") == model select v.Value<string>("type"))
                        .FirstOrDefault();
                }

                if (data.TryGetValue(model, out var value))
                {
                    type = value.Value<string>("type");
                }
            }

            ModelTypeMap[model] = type;
            return type;
        }

        public static async UniTask<JObject> AsyncFromType(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            var filePath = $"{Domain}/{type}.json";
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor) filePath = filePath.Replace(":", "_");
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            JObject data = null;
            if (FileUtility.FileExists(FileUtility.FileType.DataPath, filePath))
            {
                try
                {
                    var json = await FileUtility.ReadAllTextAsync(FileUtility.FileType.DataPath, filePath);
                    data = JObject.Parse(json);
                    var ptm = data.Value<int>("_updated_time");
                    data.Remove("_updated_time");
                    var ttl = data.ContainsKey("services") ? 86400 * UnityEngine.Random.Range(30, 50) : 60;
                    if (now - ptm > ttl) data = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load cached type: {ex}");
                }
            }

            if (data == null || !data.ContainsKey("type"))
            {
                try
                {
                    var url = $"/miot-spec-v2/instance?type={type}";
                    var (success, json) = await AsyncDownloadMiotSpec(url, 3);
                    if (success)
                    {
                        data = JObject.Parse(json);
                        data["_updated_time"] = now;
                        await FileUtility.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Get miot-spec for {type} failed: {ex}");
                }
            }

            return data;
        }

        public static async UniTask<Langs> AsyncGetLangs(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            var fnm = $"{Domain}/spec-langs/{type}.json";
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor) fnm = fnm.Replace(":", "_");
            var filePath = Path.Combine(Application.persistentDataPath, fnm);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            JObject data = null;
            if (FileUtility.FileExists(FileUtility.FileType.DataPath, filePath))
            {
                try
                {
                    var json = await FileUtility.ReadAllTextAsync(FileUtility.FileType.DataPath, filePath);
                    data = JObject.Parse(json);
                    var ptm = data.Value<int>("_updated_time");
                    data.Remove("_updated_time");
                    var ttl = data.ContainsKey("data") ? 86400 * UnityEngine.Random.Range(30, 50) : 60;
                    if (now - ptm > ttl) data = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load cached langs: {ex}");
                }
            }

            if (data == null || !data.ContainsKey("type"))
            {
                try
                {
                    var url = $"/instance/v2/multiLanguage?urn={type}";
                    var (success, json) = await AsyncDownloadMiotSpec(url, 3);
                    if (success)
                    {
                        data = JObject.Parse(json);
                        data["_updated_time"] = now;
                        await FileUtility.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Get miot-spec langs for {type} failed: {ex}");
                }
            }

            var map = data?.Value<JObject>("data");
            if (map == null) return null;
            var langs = new Langs();
            foreach (var one in map.Properties())
                langs.Map.Add(one.Name, new Lang(one.Value.ToObject<Dictionary<string, string>>()));
            return langs;
        }

        private static async UniTask<(bool, string)> AsyncDownloadMiotSpec(string path, int tries = 1, int timeout = 30)
        {
            while (tries > 0)
            {
                foreach (var host in Hosts)
                {
                    var url = $"{host}{path}";
                    using var request = UnityWebRequest.Get(url);
                    request.timeout = timeout;
                    try
                    {
                        await request.SendWebRequest();
                    }
                    catch (Exception ex)
                    {
                        return (false, $"HTTP Request Error: {url}\n{ex}");
                    }

                    return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
                }

                tries--;
                await Task.Delay(1000);
            }

            return (false, "Max retries exceeded.");
        }

        protected static string GetSpecLangKey(long siid, long piid = 0, long aiid = 0, long viid = -1)
        {
            var key = $"service:{siid:D3}";
            if (piid > 0) return $"{key}:property:{piid:D3}";
            if (aiid > 0) return $"{key}:action:{aiid:D3}";
            return viid >= 0 ? $"{key}:valuelist:{viid:D3}" : key;
        }

        protected static string FormatName(string name)
        {
            name = name.Trim();
            return Regex.Replace(name, @"\W+", "_").ToLower();
        }

        protected static string FormatDescName(string description, string name)
        {
            return FormatName(!string.IsNullOrEmpty(description) && !Regex.IsMatch(description, @"[^x00-xff]")
                ? description
                : name);
        }

        protected static string NameByType(string type)
        {
            var arr = $"{type}:::".Split(':');
            var name = arr.Length > 3 ? arr[3] : "";
            return FormatName(name);
        }

        public class Lang
        {
            public Dictionary<string, string> Map;

            public Lang(Dictionary<string, string> map)
            {
                Map = map;
            }
        }

        public class Langs
        {
            public Dictionary<string, Lang> Map = new();

            public static string Language
            {
                get
                {
                    var code = LocalizationSettings.SelectedLocale.Identifier.Code;
                    return code switch
                    {
                        "zh-Hans" => "zh_cn",
                        "en" => "en",
                        _ => code
                    };
                }
            }

            public Lang Get(string language = null)
            {
                language ??= Language;
                return Map.GetValueOrDefault(language);
            }
        }

        public long Iid { get; private set; }

        public string Type { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }


        private string[] _translationKeys;

        protected virtual void Deserialize(JObject data)
        {
            Iid = data.Value<long>("iid");
            Type = data.Value<string>("type");
            Name = NameByType(Type);
            Description = data.Value<string>("description");
        }

        protected virtual string[] GetTranslationKeys()
        {
            return new[] { "_globals" };
        }

        public string GetTranslation(string des)
        {
            _translationKeys ??= GetTranslationKeys();
            var dls = new[]
            {
                des.ToLower(),
                des,
                des.Replace('-', ' ').ToLower(),
                des.Replace('-', ' '),
            };
            var trans = MiotTranslation.Get();
            foreach (var d in dls)
            {
                foreach (var key in _translationKeys)
                {
                    if (!trans.TryGetValue(key, out var tran)) continue;
                    switch (tran)
                    {
                        case string str:
                            return str;
                        case Dictionary<string, string> dict when dict.TryGetValue(d, out var value):
                            return value;
                    }
                }
            }

            return des;
        }

        public virtual string FriendlyName => $"{Name}";

        public virtual string FriendlyDesc => GetTranslation(!string.IsNullOrEmpty(Description) ? Description : Name);
    }

    public class MiotProperty : MiotObject
    {
        public static MiotProperty Build(JObject data, MiotService service)
        {
            var format = data.Value<string>("format");
            var hasRange = data.ContainsKey("value-range");
            var hasList = data.ContainsKey("value-list");
            var type = format switch
            {
                "bool" => typeof(bool),
                "float" => typeof(float),
                "int8" => typeof(sbyte),
                "int16" => typeof(short),
                "int32" => typeof(int),
                "int64" => typeof(long),
                "uint8" => typeof(byte),
                "uint16" => typeof(ushort),
                "uint32" => typeof(uint),
                "uint64" => typeof(ulong),
                "string" => typeof(string),
                _ => throw new NotSupportedException($"Format {format} is not supported.")
            };
            var clsType = typeof(MiotProperty<>);
            if (hasRange) clsType = typeof(RangeMiotProperty<>);
            if (hasList) clsType = typeof(EnumMiotProperty<>);
            clsType = clsType.MakeGenericType(type);
            var ctor = clsType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new[] { typeof(MiotService) },
                null);
            var property = ctor!.Invoke(new object[] { service }) as MiotProperty;
            property!.Deserialize(data);
            return property;
        }

        [Flags]
        public enum AccessType
        {
            None = 0,
            Read = 1,
            Write = 2,
            Notify = 4
        }

        public MiotService Service { get; }

        public AccessType Access { get; private set; }

        public string Format { get; private set; }

        public string Unit { get; private set; }

        protected MiotProperty(MiotService service)
        {
            Service = service;
        }

        protected override void Deserialize(JObject data)
        {
            base.Deserialize(data);
            Format = data.Value<string>("format");
            Unit = data.Value<string>("unit");
            var access = data.Value<JArray>("access").Values<string>();
            Access = AccessType.None;
            foreach (var a in access)
                Access |= Enum.Parse<AccessType>(a, true);
        }

        protected override string[] GetTranslationKeys()
        {
            return new[] { "_globals", Service.Name, Name, $"{Service.Name}.{Name}" };
        }

        protected virtual string GetSpecTranslation(long viid = -1)
        {
            return Service.GetSpecTranslation(Iid);
        }

        public override string FriendlyName => $"{Service.FriendlyName}.{base.FriendlyName}";

        public override string FriendlyDesc
        {
            get
            {
                var sde = "";
                if (Name is "on" or "switch")
                    sde = Service.GetSpecTranslation();
                var pde = GetSpecTranslation();
                if (!string.IsNullOrEmpty(sde) && !string.IsNullOrEmpty(pde))
                    pde = pde.Replace(sde, "");
                var des = $"{sde} {pde}".Trim();
                if (string.IsNullOrEmpty(des))
                {
                    sde = (!string.IsNullOrEmpty(Service.Description) ? Service.Description : Service.Name).Trim();
                    pde = (!string.IsNullOrEmpty(Description) ? Description : Name).Trim();
                    des = sde == pde ? pde : $"{sde} {pde}".Trim();
                    var ret = GetTranslation(des);
                    if (ret != des) return ret;
                    sde = Service.GetTranslation(sde);
                    pde = GetTranslation(pde);
                    if (sde != pde) return $"{sde} {pde}".Trim();
                }

                var arr = des.Split(" ");
                return string.Join(" ", arr.Distinct());
            }
        }

        public virtual string ExtendDesc => JsonConvert.SerializeObject(new { name = FriendlyDesc, format = Format });
    }

    public class MiotProperty<T> : MiotProperty where T : IComparable, IComparable<T>, IEquatable<T>
    {
        protected MiotProperty(MiotService service) : base(service)
        {
        }
    }

    public class RangeMiotProperty<T> : MiotProperty<T> where T : IComparable, IComparable<T>, IEquatable<T>
    {
        public T Min { get; private set; }

        public T Max { get; private set; }

        public T Step { get; private set; }

        protected RangeMiotProperty(MiotService service) : base(service)
        {
        }

        protected override void Deserialize(JObject data)
        {
            base.Deserialize(data);
            var range = data.Value<JArray>("value-range").Values<T>().ToArray();
            Min = range[0];
            Max = range[1];
            Step = range[2];
        }

        public override string ExtendDesc =>
            JsonConvert.SerializeObject(new
            {
                name = FriendlyDesc,
                format = Format,
                range = new
                {
                    min = Min,
                    max = Max,
                    step = Step
                }
            });
    }

    public class EnumMiotProperty<T> : MiotProperty where T : IEquatable<T>
    {
        public Tuple<T, string>[] Enums { get; private set; }

        protected EnumMiotProperty(MiotService service) : base(service)
        {
        }

        protected override void Deserialize(JObject data)
        {
            base.Deserialize(data);
            var list = data.Value<JArray>("value-list");
            if (list == null) return;
            Enums = list.Select(i => new Tuple<T, string>(i.Value<T>("value"), i.Value<string>("description")))
                .ToArray();
        }

        protected override string GetSpecTranslation(long viid = -1)
        {
            if (viid == -1) return base.GetSpecTranslation(viid);
            if (viid < 0 || viid >= Enums.Length) return string.Empty;
            return Service.GetSpecTranslation(piid: Iid, viid: viid);
        }

        public override string ExtendDesc =>
            JsonConvert.SerializeObject(new
            {
                name = FriendlyName,
                format = Format,
                enums = Enums.Select((i, index) =>
                {
                    var desc = GetSpecTranslation(index);
                    if (string.IsNullOrEmpty(desc)) desc = i.Item2;
                    return new
                    {
                        value = i.Item1,
                        description = desc
                    };
                })
            });
    }

    public class MiotAction : MiotObject
    {
        public static MiotAction Build(JObject data, MiotService service)
        {
            var action = new MiotAction(service);
            action.Deserialize(data);
            return action;
        }

        public MiotService Service { get; }

        public MiotProperty[] In { get; private set; }

        public MiotProperty[] Out { get; private set; }

        private MiotAction(MiotService service)
        {
            Service = service;
        }

        protected override void Deserialize(JObject data)
        {
            base.Deserialize(data);
            var propertyMap = Service.Properties.ToDictionary(i => i.Iid, i => i);
            In = data.Value<JArray>("in").Values<int>().Select(i => propertyMap[i]).ToArray();
            Out = data.Value<JArray>("out").Values<int>().Select(i => propertyMap[i]).ToArray();
        }

        protected override string[] GetTranslationKeys()
        {
            return new[] { "_globals", Service.Name };
        }

        protected string GetSpecTranslation()
        {
            return Service.GetSpecTranslation(aiid: Iid);
        }

        public override string FriendlyName => $"{Service.FriendlyName}.{base.FriendlyName}";

        public override string FriendlyDesc
        {
            get
            {
                var sde = !string.IsNullOrEmpty(Description) ? Description : Name;
                var ret = GetTranslation(sde);
                if (ret != sde) return ret;
                var pde = GetSpecTranslation();
                return !string.IsNullOrEmpty(pde) ? pde : sde;
            }
        }
    }

    public class MiotService : MiotObject
    {
        public static MiotService Build(JObject data, MiotSpec spec)
        {
            var service = new MiotService(spec);
            service.Deserialize(data);
            return service;
        }

        private readonly MiotSpec _spec;

        public MiotProperty[] Properties { get; private set; }

        public MiotAction[] Actions { get; private set; }

        private MiotService(MiotSpec spec)
        {
            _spec = spec;
        }

        protected override void Deserialize(JObject data)
        {
            base.Deserialize(data);
            var properties = data.Value<JArray>("properties");
            Properties = properties != null
                ? properties.Select(i => MiotProperty.Build((JObject)i, this)).ToArray()
                : Array.Empty<MiotProperty>();
            var actions = data.Value<JArray>("actions");
            Actions = actions != null
                ? actions.Select(i => MiotAction.Build((JObject)i, this)).ToArray()
                : Array.Empty<MiotAction>();
        }

        protected override string[] GetTranslationKeys()
        {
            return new[] { "_globals", Name };
        }

        public string GetSpecTranslation(long piid = 0, long aiid = 0, long viid = -1)
        {
            return _spec.GetSpecTranslation(Iid, piid, aiid, viid);
        }

        public override string FriendlyDesc
        {
            get
            {
                var sde = !string.IsNullOrEmpty(Description) ? Description : Name;
                var ret = GetTranslation(sde);
                if (ret != sde) return ret;
                var pde = GetSpecTranslation();
                return !string.IsNullOrEmpty(pde) ? pde : sde;
            }
        }
    }

    public class MiotSpec : MiotObject
    {
        private static readonly Dictionary<string, MiotSpec> TypeSpecMap = new();

        public static async UniTask<MiotSpec> Fetch(string typeOrModel, bool useRemote = false)
        {
            if (!typeOrModel.StartsWith("urn:")) typeOrModel = await AsyncGetModelType(typeOrModel);
            if (!useRemote && TypeSpecMap.TryGetValue(typeOrModel, out var spec)) return spec;
            var data = await AsyncFromType(typeOrModel);
            if (data == null) return null;
            spec = Build(data);
            spec._langs = await AsyncGetLangs(typeOrModel);
            TypeSpecMap[typeOrModel] = spec;
            return spec;
        }

        public static MiotSpec Build(JObject data)
        {
            var spec = new MiotSpec();
            spec.Deserialize(data);
            return spec;
        }

        private Langs _langs;
        public MiotService[] Services { get; private set; }

        protected override void Deserialize(JObject data)
        {
            base.Deserialize(data);
            var services = data.Value<JArray>("services");
            if (services != null) Services = services.Select(i => MiotService.Build((JObject)i, this)).ToArray();
        }

        public string GetSpecTranslation(long siid, long piid = 0, long aiid = 0, long viid = -1)
        {
            var lang = _langs.Get();
            if (lang == null) return string.Empty;
            var key = GetSpecLangKey(siid, piid, aiid, viid);
            return lang.Map.GetValueOrDefault(key) ?? "";
        }
    }

    public class MiotDevice
    {
        public static MiotDevice Build(JObject data)
        {
            var device = new MiotDevice();
            device.Deserialize(data);
            return device;
        }

        public string Did { get; private set; }

        public int Pid { get; private set; }

        public bool IsOnline { get; private set; }

        public string Mac { get; private set; }

        public string Name { get; private set; }

        public string Model { get; private set; }

        public string Type { get; private set; }

        public int VoiceCtrl { get; private set; }

        public string ParentId { get; private set; }

        public string SplitParentId { get; private set; }

        public string SplitModuleId { get; private set; }

        public int OrderTime { get; private set; }

        public bool IsVisible { get; set; } = true;

        private void Deserialize(JObject data)
        {
            Did = data.Value<string>("did");
            Pid = data.Value<int>("pid");
            IsOnline = data.Value<bool>("isOnline");
            Mac = data.Value<string>("mac");
            Name = data.Value<string>("name");
            Model = data.Value<string>("model");
            Type = data.Value<string>("spec_type");
            VoiceCtrl = data.Value<int>("voice_ctrl");
            ParentId = data.Value<string>("parent_id");
            OrderTime = data.Value<int>("order_time");
            SplitParentId = data["extra"]?["split"]?.Value<string>("parentId");
            SplitModuleId = data["extra"]?["split"]?.Value<string>("moduleId");
        }
    }

    public class MiotRoom
    {
        public static MiotRoom Build(JObject data, MiotHome home)
        {
            var room = new MiotRoom(home);
            room.Deserialize(data);
            return room;
        }

        public static MiotRoom UnassignedRoom(MiotHome home)
        {
            var room = new MiotRoom(home)
            {
                Id = $"{home.Id}-unassigned",
                Name = "未分配房间",
                Dids = home.Dids,
                CreateTime = int.MaxValue
            };
            return room;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Icon { get; private set; }

        public string[] Dids { get; private set; }

        public int CreateTime { get; private set; }

        public MiotHome Home { get; private set; }

        private MiotRoom(MiotHome home)
        {
            Home = home;
        }

        private void Deserialize(JObject data)
        {
            Id = data.Value<string>("id");
            Name = data.Value<string>("name");
            Icon = data.Value<string>("icon");
            Dids = data.Value<JArray>("dids").Values<string>().ToArray();
            CreateTime = data.Value<int>("create_time");
        }
    }

    public class MiotHome
    {
        public static MiotHome Build(JObject data)
        {
            var room = new MiotHome();
            room.Deserialize(data);
            return room;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Icon { get; private set; }

        public string[] Dids { get; private set; }

        public int CreateTime { get; private set; }

        public MiotRoom[] Rooms { get; private set; }

        private void Deserialize(JObject data)
        {
            Id = data.Value<string>("id");
            Name = data.Value<string>("name");
            Icon = data.Value<string>("icon");
            Dids = data.Value<JArray>("dids").Values<string>().ToArray();
            CreateTime = data.Value<int>("create_time");
            Rooms = data.Value<JArray>("roomlist").Select(i => MiotRoom.Build((JObject)i, this)).ToArray();
        }
    }
}