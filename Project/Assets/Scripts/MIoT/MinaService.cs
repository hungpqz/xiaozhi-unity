using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace XiaoZhi.Unity.MIoT
{
    public class MinaService
    {
        private readonly MiAccount _account;

        public MinaService(MiAccount account)
        {
            _account = account;
        }

        public async UniTask<(bool, string)> ListDevice(int master = 0)
        {
            return await _account.MinaRequest($"/admin/v2/device_list?master={master}");
        }

        public async UniTask<(bool, string)> Text2Speech(string deviceId, string text)
        {
            return await UbusRequest(deviceId, "text_to_speech", "mibrain", JsonConvert.SerializeObject(new { text }));
        }

        public async UniTask<(bool, string)> SetVolume(string deviceId, int volume)
        {
            return await UbusRequest(deviceId, "player_set_volume", "mediaplayer",
                JsonConvert.SerializeObject(new { volume, media = "app_ios" }));
        }

        private async UniTask<(bool, string)> UbusRequest(string deviceId, string method, string path,
            string jsonMessage)
        {
            Debug.Log(jsonMessage);
            return await _account.MinaRequest($"/remote/ubus", new Dictionary<string, string>
            {
                { "deviceId", deviceId },
                { "method", method },
                { "path", path },
                { "message", jsonMessage }
            });
        }
    }
}