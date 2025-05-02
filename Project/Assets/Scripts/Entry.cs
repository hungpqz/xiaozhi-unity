using Cysharp.Threading.Tasks;
using MIoT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using XiaoZhi.Unity;
using XiaoZhi.Unity.MIOT;

public class Entry : MonoBehaviour
{
    private Context _context;

    private void Start()
    {
        _context = new Context();
        _context.Init();
        _context.Start();
        // var account = new MiAccount(() => UniTask.FromResult(("cn", "2556468231",
        //     "V1:DXmurwq2/R1BHTELu6obCV2W16Ch9IjHqkrWktEk0sg5IViAJnt7Bwl8Pc3raeFQt2prC+QOxdpVUGVRo0GpXCnMuSqa7pyqov1hz2TLcNpglaYFRI+aJjbRuuMkQHCt2Ccn9LhRwIofNREc/9DsjwqXHmLYxw51zExWpIFk6MfXoUZe9f68D1P/wJ2/xr/Q430ny5GHE/SougB0EJmfdM8OGmfAn0dno5UjhwTvnOgkWg1WsNj+mr7iALa0Ipy2W1BhKAhyYmjPx58xhDvu04fVM9g/oOfr2aGP7o0FogfFp6jSvvsnA51nAc4na2QUlgiHiM6xJKyKQnfgWFOf+A==")));
        // var miioService = new MiioService(account);
        // var minaService = new MinaService(account);
        // var (success, data) = await miioService.Action("800078485", (5, 5), "打开入户灯", "0");
        // var (success, data) = await miioService.GetProp("800078485", (1, 1));
        // var (success, data) = await miioService.ListDevice();
        // Debug.Log(data);
        // var (success, data) = await miioService.ListDevice();
        // var (success, data) = await minaService.Text2Speech("cc78d2c6-e02d-4248-8238-fd299c4319cd", "你在玩什么球球？");
        // var (success, data) = await minaService.SetVolume("800078485", 100);
        // var data = await MiotObject.AsyncFromModel("xiaomi.tv.v1");
        // Debug.Log(JsonConvert.SerializeObject(data));
        // var type = await MiotObject.AsyncGetModelType("xiaomi.tv.v1");
        // var data1 = await MiotObject.AsyncGetLangs(type);
        // Debug.Log(JsonConvert.SerializeObject(data1));
        // var (success, data) = await miioService.ListHomes();
        // var (success, data) = await miioService.ListDeviceV2(2556468231, 504001292567, "");
        // var obj = JObject.Parse(data);
        // var device = await MiotDevice.Build(obj["result"]["device_info"].First.Next.Next.Value<JObject>());
        // Debug.Log(device.Spec.Services[0].Properties[0].FriendlyDesc);
        // Debug.Log(data);
    }

    private void OnApplicationQuit()
    {
        _context.Dispose();
    }
}