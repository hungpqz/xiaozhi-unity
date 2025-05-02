using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace XiaoZhi.Unity.MIOT
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

        public async UniTask<(bool, string)> GetProps(string did, (long, long)[] iids)
        {
            var parameters = JsonConvert.SerializeObject(iids.Select(i =>
                new { did, siid = i.Item1, piid = i.Item2 }).ToArray());
            return await MiotRequest("/prop/get", parameters);
        }

        public async UniTask<(bool, string)> SetProps(string did, (long, long, object)[] iids)
        {
            var parameters = JsonConvert.SerializeObject(iids.Select(i =>
                new { did, siid = i.Item1, piid = i.Item2, value = i.Item3 }).ToArray());
            return await MiotRequest("/prop/set", parameters);
        }

        public async UniTask<(bool, string)> GetProp(string did, (long, long) iid)
        {
            return await GetProps(did, new[] { iid });
        }

        public async UniTask<(bool, string)> SetProp(string did, (long, long, object) iid)
        {
            return await SetProps(did, new[] { iid });
        }

        public async UniTask<(bool, string)> Action(string did, (long, long) iid, params string[] args)
        {
            var parameters = JsonConvert.SerializeObject(new { did, siid = iid.Item1, aiid = iid.Item2, @in = args });
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