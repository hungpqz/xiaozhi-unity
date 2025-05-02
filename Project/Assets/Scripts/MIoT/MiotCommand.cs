using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using MIoT;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace XiaoZhi.Unity.MIOT
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

        public async UniTask<(bool, string)> Login(string region, string userId, string passToken)
        {
            var (success, data) = await _account.Login(region, userId, passToken);
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

        public async UniTask<string> GetProp(string did, (long, long) iid)
        {
            var (success, data) = await _miioService.GetProp(did, iid);
            if (!success)
            {
                Debug.LogError(data);
                return null;
            }

            return JObject.Parse(data)["result"]?.First?["value"]?.Value<string>();
        }

        public async UniTask<bool> SetProp(string did, (long, long, object) iid)
        {
            var (success, data) = await _miioService.SetProp(did, iid);
            if (!success) Debug.LogError(data);
            return success;
        }

        public async UniTask<bool> Action(string did, (long, long) iid, params string[] args)
        {
            var (success, data) = await _miioService.Action(did, iid, args);
            if (!success) Debug.LogError(data);
            return success;
        }
    }
}