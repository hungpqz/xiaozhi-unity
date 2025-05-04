using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace XiaoZhi.Unity.MIoT
{
    public class MiotCommand
    {
        private readonly MiAccount _account;

        private readonly MiioService _miioService;

        public MiotCommand()
        {
            _account = new MiAccount(MiAccount.Miio);
            _miioService = new MiioService(_account);
        }

        public MiAccount.Token GetToken() => _account.GetToken();

        public async UniTask<(bool, string)> Login(string userId, string passToken)
        {
            var (success, data) = await _account.Login(userId, passToken);
            if (!success) Debug.LogError(data);
            return (success, data);
        }

        public async UniTask<(bool, string)> Verify(string url, string ticket)
        {
            var (success, data) = await _account.Verify(url, ticket);
            if (!success) Debug.LogError(data);
            return (success, data);
        }

        public void Logout()
        {
            _account.Logout();
        }

        public async UniTask<MiotHome[]> ListHome()
        {
            var (success, data) = await _miioService.ListHome();
            if (!success)
            {
                Debug.LogError(data);
                return null;
            }

            return JObject.Parse(data)["result"]?["homelist"]?.Select(i => MiotHome.Build((JObject)i))
                .ToArray();
        }

        public async UniTask<MiotDevice[]> ListDevice(string homeId)
        {
            // Todo 分页处理
            var (success, data) =
                await _miioService.ListDeviceV2(long.Parse(_account.GetToken().UserId), long.Parse(homeId), "");
            if (!success)
            {
                Debug.LogError(data);
                return null;
            }

            return JObject.Parse(data)["result"]?["device_info"]?.Select(i => MiotDevice.Build((JObject)i)).ToArray();
        }

        public async UniTask<string[]> GetProps(IEnumerable<(string, long, long)> iids)
        {
            var (success, data) = await _miioService.GetProps(iids);
            if (!success)
            {
                Debug.LogError(data);
                return null;
            }
            
            var map = DictionaryPool<string, string>.Get();
            var list = JObject.Parse(data).Value<JArray>("result");
            foreach (var i in list)
            {
                var propKey = $"{i.Value<string>("did")}.{i.Value<string>("siid")}.{i.Value<string>("piid")}";
                map[propKey] = i.Value<string>("value");
            }
            
            var result = iids.Select(i => map[$"{i.Item1}.{i.Item2}.{i.Item3}"]).ToArray();
            DictionaryPool<string, string>.Release(map);
            return result;
        }

        public async UniTask<string> GetProp(string did, long siid, long piid)
        {
            var (success, data) = await _miioService.GetProp(did, siid, piid);
            if (!success)
            {
                Debug.LogError(data);
                return null;
            }

            return JObject.Parse(data)["result"]?.First?["value"]?.Value<string>();
        }

        public async UniTask<bool> SetProp(string did, long siid, long piid, object value)
        {
            var (success, data) = await _miioService.SetProp(did, siid, piid, value);
            if (!success) Debug.LogError(data);
            return success;
        }

        public async UniTask<bool> Action(string did, long siid, long aiid, params string[] args)
        {
            var (success, data) = await _miioService.Action(did, siid, aiid, args);
            if (!success) Debug.LogError(data);
            return success;
        }
    }
}