using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace XiaoZhi.Unity.MIoT
{
    public class MiioService
    {
        private readonly MiAccount _account;

        public MiioService(MiAccount account)
        {
            _account = account;
        }

        public async UniTask<(bool, string)> ListHome(bool fg = true, bool fetchShare = true,
            bool fetchShareDev = true, bool fetchCariot = true, int limit = 300, int appVer = 7, int platForm = 0)
        {
            return await _account.MiioRequest("/v2/homeroom/gethome_merged",
                JsonConvert.SerializeObject(new
                {
                    fg, fetch_share = fetchShare, fetch_share_dev = fetchShareDev, fetch_cariot = fetchCariot, limit,
                    app_ver = appVer,
                    plat_form = platForm
                }));
        }

        public async UniTask<(bool, string)> ListDevice(bool getVirtualModel = false, int getHuamiDevices = 0,
            bool getSplitDevice = false, bool supportSmartHome = true)
        {
            return await _account.MiioRequest("/home/device_list",
                JsonConvert.SerializeObject(new
                {
                    getVirtualModel, getHuamiDevices, get_split_device = getSplitDevice,
                    support_smart_home = supportSmartHome
                }));
        }

        public async UniTask<(bool, string)> ListDeviceV2(long homeOwner, long homeID, string startDid,
            int limit = 300, bool getSplitDevice = true, bool supportSmartHome = true,
            bool getCariotDevice = true, bool getThirdDevice = true)
        {
            return await _account.MiioRequest("/v2/home/home_device_list",
                JsonConvert.SerializeObject(new
                {
                    home_owner = homeOwner,
                    home_id = homeID,
                    start_did = startDid, limit,
                    get_split_device = getSplitDevice,
                    support_smart_home = supportSmartHome,
                    get_cariot_device = getCariotDevice,
                    get_third_device = getThirdDevice
                }));
        }

        public async UniTask<(bool, string)> GetProps(IEnumerable<(string, long, long)> iids)
        {
            var parameters = JsonConvert.SerializeObject(iids.Select(i =>
                new { did = i.Item1, siid = i.Item2, piid = i.Item3 }).ToArray());
            return await MiotRequest("/prop/get", parameters);
        }

        public async UniTask<(bool, string)> SetProps(IEnumerable<(string, long, long, object)> iids)
        {
            var parameters = JsonConvert.SerializeObject(iids.Select(i =>
                new { did = i.Item1, siid = i.Item2, piid = i.Item3, value = i.Item4 }).ToArray());
            return await MiotRequest("/prop/set", parameters);
        }

        public async UniTask<(bool, string)> GetProp(string did, long siid, long piid)
        {
            return await GetProps(new[] { (did, siid, piid) });
        }

        public async UniTask<(bool, string)> SetProp(string did, long siid, long piid, object value)
        {
            return await SetProps(new[] { (did, siid, piid, value) });
        }

        public async UniTask<(bool, string)> Action(string did, long siid, long aiid, params string[] args)
        {
            var parameters = JsonConvert.SerializeObject(new { did, siid, aiid, @in = args });
            return await MiotRequest("/action", parameters);
        }

        private async UniTask<(bool, string)> MiotRequest(string url, string parameters)
        {
            return await _account.MiioRequest($"/miotspec{url}", BuildMiotParams(parameters));
        }

        private static string BuildMiotParams(string parameters)
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("params");
            jsonWriter.WriteRawValue(parameters);
            jsonWriter.WriteEndObject();
            return stringWriter.ToString();
        }
    }
}