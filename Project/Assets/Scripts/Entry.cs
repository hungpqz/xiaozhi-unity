using Cysharp.Threading.Tasks;
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
        // var account = new MiAccount();
        // var miioService = new MiioService(account);
        // var minaService = new MinaService(account);
        // // var (success, data) = await miioService.Action("800078485", (5, 5), "打开书房灯", "0");
        // // var (success, data) = await minaService.ListDevice();
        // var (success, data) = await minaService.Text2Speech("cc78d2c6-e02d-4248-8238-fd299c4319cd", "你在玩什么球球？");
        // // var (success, data) = await minaService.SetVolume("800078485", 100);
        // Debug.Log(data);
    }

    private void OnApplicationQuit()
    {
        _context.Dispose();
    }
}