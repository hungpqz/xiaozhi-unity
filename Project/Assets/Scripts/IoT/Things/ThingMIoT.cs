using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Pool;
using XiaoZhi.Unity.MIoT;

namespace XiaoZhi.Unity.IoT
{
    public class ThingMIoT : Thing
    {
        [Flags]
        public enum RegPartial
        {
            Getter = 1,
            Setter = 2,
            Action = 4
        }

        private const int RefreshInterval = 60 * 1000;

        private readonly Settings _settings;
        private readonly MiotCommand _command;
        private Dictionary<string, MiotHome> _homeMap;
        private Dictionary<string, MiotRoom> _roomMap;
        private Dictionary<string, MiotRoom> _deviceRoomMap;
        private Dictionary<string, MiotDevice> _deviceMap;
        private readonly HashSet<string> _watchDids;
        private CancellationTokenSource _mainCts;

        public ThingMIoT() : base("Miot", "小米IoT平台，可以控制智能家居")
        {
            _command = new MiotCommand();
            _settings = new Settings("miot");
            _watchDids = new HashSet<string>(_settings.GetString("watch_device_dids").Split(","));
            _mainCts = new CancellationTokenSource();
        }

        public override async UniTask Load()
        {
            if (!IsLogin) return;
            if (!await LoadDevices())
            {
                Logout();
                return;
            }

            await RegisterDevices();
            UniTask.Void(MainLoop, _mainCts.Token);
        }

        private async UniTaskVoid MainLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(RefreshInterval, cancellationToken: cancellationToken);
                if (!IsLogin) continue;
                await RegisterGetters();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_mainCts != null)
            {
                _mainCts.Cancel();
                _mainCts.Dispose();
                _mainCts = null;
            }
        }

        public bool IsLogin => _command.GetToken().IsValid;

        public string UserId => _command.GetToken().UserId;

        public MiotHome[] GetHomes() => _homeMap != null
            ? _homeMap.Values.OrderBy(i => i.CreateTime).ToArray()
            : Array.Empty<MiotHome>();

        public MiotRoom[] GetRooms(string homeId)
        {
            return _homeMap == null || !_homeMap.TryGetValue(homeId, out var home)
                ? Array.Empty<MiotRoom>()
                : home.Rooms.OrderBy(r => r.CreateTime).ToArray();
        }

        public MiotDevice[] GetDevices(string roomId)
        {
            return _roomMap == null || !_roomMap.TryGetValue(roomId, out var room)
                ? Array.Empty<MiotDevice>()
                : room.Dids.Select(d => _deviceMap.GetValueOrDefault(d))
                    .Where(d => d is { IsVisible: true })
                    .OrderBy(d => d.OrderTime).ToArray();
        }

        public bool IsWatchDevice(string deviceId)
        {
            return _watchDids.Contains(deviceId);
        }

        public void WatchDevice(string deviceId)
        {
            _watchDids.Add(deviceId);
            _settings.SetString("watch_device_dids", string.Join(",", _watchDids.ToArray()));
            _settings.Save();
        }

        public void UnwatchDevice(string deviceId)
        {
            _watchDids.Remove(deviceId);
            _settings.SetString("watch_device_dids", string.Join(",", _watchDids.ToArray()));
            _settings.Save();
        }

        public void UnwatchAllDevices()
        {
            _watchDids.Clear();
            _settings.EraseKey("watch_device_dids");
            _settings.Save();
        }

        public async UniTask<(bool, string)> Login(string userId, string passToken)
        {
            if (IsLogin) return (true, null);
            return await _command.Login(userId, passToken);
        }

        public async UniTask<(bool, string)> Verify(string url, string ticket)
        {
            return await _command.Verify(url, ticket);
        }

        public void Logout()
        {
            _command.Logout();
        }

        public async UniTask<bool> LoadDevices()
        {
            var homes = await _command.ListHome();
            if (homes == null) return false;
            _homeMap = homes.ToDictionary(h => h.Id);
            _roomMap = homes.SelectMany(h => h.Rooms).ToDictionary(r => r.Id);
            foreach (var home in homes)
            {
                if (home.Dids.Length == 0) continue;
                var room = MiotRoom.UnassignedRoom(home);
                _roomMap.Add(room.Id, room);
            }

            _deviceRoomMap = _roomMap.Values.SelectMany(room => room.Dids, (room, did) => (room, did))
                .ToDictionary(p => p.did, p => p.room);
            _deviceMap = new Dictionary<string, MiotDevice>();
            foreach (var home in homes)
            {
                var devices = await _command.ListDevice(home.Id);
                if (devices == null) continue;
                foreach (var device in devices) _deviceMap[device.Did] = device;
            }

            foreach (var device in _deviceMap.Values)
            {
                if (!string.IsNullOrEmpty(device.SplitParentId) &&
                    _deviceMap.TryGetValue(device.SplitParentId, out var parent))
                    parent.IsVisible = false;
            }

            return true;
        }

        public async UniTask RegisterGetters()
        {
            var props = ListPool<(MiotDevice, MiotProperty)>.Get();
            foreach (var did in _watchDids)
                await RegisterDevice(did, RegPartial.Getter, props);
            await RegisterGetters(props);
            ListPool<(MiotDevice, MiotProperty)>.Release(props);
        }

        public async UniTask RegisterDevices()
        {
            _properties.Clear();
            _methods.Clear();
            var props = ListPool<(MiotDevice, MiotProperty)>.Get();
            foreach (var did in _watchDids)
                await RegisterDevice(did, RegPartial.Getter | RegPartial.Setter | RegPartial.Action, props);
            await RegisterGetters(props);
            ListPool<(MiotDevice, MiotProperty)>.Release(props);
        }

        public async UniTask RegisterDevice(string did, RegPartial part, List<(MiotDevice, MiotProperty)> props)
        {
            if (!_deviceMap.TryGetValue(did, out var device) || !device.IsVisible || !device.IsOnline) return;
            // Debug.Log($"Start register: {device.Name}");
            var room = _deviceRoomMap.GetValueOrDefault(did);
            if (room == null) return;
            var home = room.Home;
            var spec = await MiotSpec.Fetch(device.Type);
            foreach (var service in spec.Services.Skip(1))
            {
                if (service.Properties != null)
                {
                    if (part.HasFlag(RegPartial.Getter))
                    {
                        props.AddRange(service.Properties.Where(p => p.Access.HasFlag(MiotProperty.AccessType.Read))
                            .Select(p => (device, p)));
                    }

                    if (part.HasFlag(RegPartial.Setter))
                    {
                        foreach (var p in service.Properties
                                     .Where(p => p.Access.HasFlag(MiotProperty.AccessType.Write)))
                            RegisterSetter(p, device, room, home);
                    }
                }

                if (service.Actions != null && part.HasFlag(RegPartial.Action))
                {
                    foreach (var a in service.Actions)
                    {
                        RegisterAction(did, service.Iid, a, device, room, home);
                    }
                }
            }

            // Debug.Log($"End register: {device.Name}");
        }

        private async UniTask RegisterGetters(List<(MiotDevice, MiotProperty)> props)
        {
            if (props.Count == 0) return;
            var values = await _command.GetProps(props.Select(i => (i.Item1.Did, i.Item2.Service.Iid, i.Item2.Iid)));
            for (var index = 0; index < props.Count; index++)
            {
                var (device, prop) = props[index];
                var room = _deviceRoomMap[device.Did];
                RegisterGetter(prop, device, room, room.Home, values[index]);
            }
        }

        private void RegisterGetter(MiotProperty prop, MiotDevice device, MiotRoom room, MiotHome home, string valueStr)
        {
            if (string.IsNullOrEmpty(valueStr)) return;
            switch (prop.Format)
            {
                case "bool":
                    RegisterGetter(prop, device, room, home, Convert.ToBoolean(valueStr));
                    break;
                case "float":
                    RegisterGetter(prop, device, room, home, Convert.ToSingle(valueStr));
                    break;
                case "int8":
                    RegisterGetter(prop, device, room, home, Convert.ToSByte(valueStr));
                    break;
                case "int16":
                    RegisterGetter(prop, device, room, home, Convert.ToInt16(valueStr));
                    break;
                case "int32":
                    RegisterGetter(prop, device, room, home, Convert.ToInt32(valueStr));
                    break;
                case "int64":
                    RegisterGetter(prop, device, room, home, Convert.ToInt64(valueStr));
                    break;
                case "uint8":
                    RegisterGetter(prop, device, room, home, Convert.ToByte(valueStr));
                    break;
                case "uint16":
                    RegisterGetter(prop, device, room, home, Convert.ToUInt16(valueStr));
                    break;
                case "uint32":
                    RegisterGetter(prop, device, room, home, Convert.ToUInt32(valueStr));
                    break;
                case "uint64":
                    RegisterGetter(prop, device, room, home, Convert.ToUInt64(valueStr));
                    break;
                case "string":
                    RegisterGetter<string>(prop, device, room, home, valueStr);
                    break;
                default:
                    throw new NotSupportedException($"Format {prop.Format} is not supported.");
            }
        }

        private void RegisterGetter<T>(MiotProperty prop, MiotDevice device, MiotRoom room, MiotHome home, T value)
        {
            var name = $"{device.Did}-{prop.FriendlyName}";
            var desc = JsonConvert.SerializeObject(new
            {
                name = prop.FriendlyDesc,
                device = device.Name,
                room = room?.Name,
                home = home?.Name
            });
            _properties.AddProperty(name, desc, () => value);
            // Debug.Log($"Registered property getter: name: {name}, desc: {desc}, value: {value}");
        }

        private void RegisterSetter(MiotProperty prop, MiotDevice device, MiotRoom room,
            MiotHome home)
        {
            switch (prop.Format)
            {
                case "bool":
                    RegisterSetter<bool>(prop, device, room, home);
                    break;
                case "float":
                    RegisterSetter<float>(prop, device, room, home);
                    break;
                case "int8":
                    RegisterSetter<sbyte>(prop, device, room, home);
                    break;
                case "int16":
                    RegisterSetter<short>(prop, device, room, home);
                    break;
                case "int32":
                    RegisterSetter<int>(prop, device, room, home);
                    break;
                case "int64":
                    RegisterSetter<long>(prop, device, room, home);
                    break;
                case "uint8":
                    RegisterSetter<byte>(prop, device, room, home);
                    break;
                case "uint16":
                    RegisterSetter<ushort>(prop, device, room, home);
                    break;
                case "uint32":
                    RegisterSetter<uint>(prop, device, room, home);
                    break;
                case "uint64":
                    RegisterSetter<ulong>(prop, device, room, home);
                    break;
                case "string":
                    RegisterSetter<string>(prop, device, room, home);
                    break;
                default:
                    throw new NotSupportedException($"Format {prop.Format} is not supported.");
            }
        }

        private void RegisterSetter<T>(MiotProperty prop, MiotDevice device, MiotRoom room, MiotHome home)
        {
            var name = $"{device.Did}-{prop.FriendlyName}-set";
            var desc = JsonConvert.SerializeObject(new
            {
                name = prop.FriendlyDesc,
                device = device.Name,
                room = room?.Name,
                home = home?.Name
            });
            _methods.AddMethod(name, desc,
                new ParameterList(new[]
                {
                    new Parameter<T>(prop.Name, prop.ExtendDesc)
                }),
                parameters =>
                {
                    var value = parameters.GetValue<T>(prop.Name);
                    SetProp(prop, device, room, home, value).Forget();
                });

            // Debug.Log(
            //     $"Registered property setter: name: {name}, desc: {desc}, paramName: {prop.Name}, paramDesc: {prop.ExtendDesc}");
        }

        private async UniTask SetProp<T>(MiotProperty prop, MiotDevice device, MiotRoom room, MiotHome home, T value)
        {
            var success = await _command.SetProp(device.Did, prop.Service.Iid, prop.Iid, value);
            if (success) RegisterGetter(prop, device, room, home, value);
            else
                await _context.UIManager.ShowNotificationUI(Lang.GetRef("MIoT_ModifyPropertyFailed",
                    new KeyValuePair<string, IVariable>("0", new StringVariable { Value = device.Name }),
                    new KeyValuePair<string, IVariable>("1", new StringVariable { Value = prop.FriendlyDesc })));
        }

        private void RegisterAction(string did, long sid, MiotAction action, MiotDevice device, MiotRoom room,
            MiotHome home)
        {
            var name = $"{device.Did}-{action.FriendlyName}-action";
            var desc = JsonConvert.SerializeObject(new
            {
                name = action.FriendlyDesc,
                device = device.Name,
                room = room?.Name,
                home = home?.Name
            });
            var parameterList = new ParameterList(action.In.Select(Property2Parameter));
            _methods.AddMethod(name, desc, parameterList, parameters =>
            {
                var args = parameters.Select(i => i.ValueString).ToArray();
                _command.Action(did, sid, action.Iid, args).Forget();
            });

            // Debug.Log(
            //     $"Registered action: name: {name}, desc: {desc}, params: [{string.Join(", ", action.In.Select(i => i.Name))}]");
        }

        private static IParameter Property2Parameter(MiotProperty prop)
        {
            return prop.Format switch
            {
                "bool" => new Parameter<bool>(prop.Name, prop.ExtendDesc),
                "float" => new Parameter<float>(prop.Name, prop.ExtendDesc),
                "int8" => new Parameter<sbyte>(prop.Name, prop.ExtendDesc),
                "int16" => new Parameter<short>(prop.Name, prop.ExtendDesc),
                "int32" => new Parameter<int>(prop.Name, prop.ExtendDesc),
                "int64" => new Parameter<long>(prop.Name, prop.ExtendDesc),
                "uint8" => new Parameter<byte>(prop.Name, prop.ExtendDesc),
                "uint16" => new Parameter<ushort>(prop.Name, prop.ExtendDesc),
                "uint32" => new Parameter<uint>(prop.Name, prop.ExtendDesc),
                "uint64" => new Parameter<ulong>(prop.Name, prop.ExtendDesc),
                "string" => new Parameter<string>(prop.Name, prop.ExtendDesc),
                _ => throw new NotSupportedException($"Format {prop.Format} is not supported.")
            };
        }
    }
}