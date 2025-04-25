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

        public async UniTask<(bool, string)> ListDevice(bool getVirtualModel = false, int getHuamiDevices = 0)
        {
            return await _account.MiioRequest("/home/device_list",
                JsonConvert.SerializeObject(new { getVirtualModel, getHuamiDevices }));
        }

        public async UniTask<(bool, string)> GetProps(string did, (int, int)[] iids)
        {
            var parameters = JsonConvert.SerializeObject(iids.Select(i =>
                new { did, siid = i.Item1, piid = i.Item2 }).ToArray());
            return await MiotRequest("/prop/get", parameters);
        }

        public async UniTask<(bool, string)> SetProps(string did, (int, int, int)[] iids)
        {
            var parameters = JsonConvert.SerializeObject(iids.Select(i =>
                new { did, siid = i.Item1, piid = i.Item2, value = i.Item3 }).ToArray());
            return await MiotRequest("/prop/set", parameters);
        }

        public async UniTask<(bool, string)> GetProp(string did, (int, int) iid)
        {
            return await GetProps(did, new[] { iid });
        }

        public async UniTask<(bool, string)> SetProp(string did, (int, int, int) iid)
        {
            return await SetProps(did, new[] { iid });
        }

        public async UniTask<(bool, string)> Action(string did, (int, int) iid, params string[] args)
        {
            var parameters = JsonConvert.SerializeObject(new { did, siid = iid.Item1, aiid = iid.Item2, @in = args });
            return await MiotRequest("/action", parameters);
        }

        public async UniTask<(bool, string)> GetHomeProps(string did, string[] props)
        {
            return await HomeRequest(did, "get_prop", JsonConvert.SerializeObject(props));
        }

        public async UniTask<(bool, string)> GetHomeProp(string did, string prop)
        {
            return await GetHomeProps(did, new[] { prop });
        }

        public async UniTask<(bool, string)> SetHomeProp(string did, string prop, object value)
        {
            return await HomeRequest(did, $"set_{prop}",
                JsonConvert.SerializeObject(value.GetType().IsArray ? value : new[] { value }));
        }

        private async UniTask<(bool, string)> MiotRequest(string url, string parameters)
        {
            return await _account.MiioRequest($"/miotspec{url}", BuildMiotParams(parameters));
        }

        private async UniTask<(bool, string)> HomeRequest(string did, string method, string parameters)
        {
            return await _account.MiioRequest($"/home/rpc/{did}", BuildHomeParams(parameters, method));
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

        private static string BuildHomeParams(string parameters, string method)
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("id");
            jsonWriter.WriteValue(1);
            jsonWriter.WritePropertyName("method");
            jsonWriter.WriteValue(method);
            jsonWriter.WritePropertyName("accessKey");
            jsonWriter.WriteValue("IOS00026747c5acafc2");
            jsonWriter.WritePropertyName("params");
            jsonWriter.WriteRawValue(parameters);
            jsonWriter.WriteEndObject();
            return stringWriter.ToString();
        }
    }
}