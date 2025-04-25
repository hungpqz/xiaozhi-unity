using Cysharp.Threading.Tasks;
using XiaoZhi.Unity.MIOT;

namespace XiaoZhi.Unity.IOT
{
    public class ThingMiot: Thing
    {
        private MiAccount _account;
        private MiioService _miioService;
        private MinaService _minaService;
        private Settings _settings;
        
        public ThingMiot() : base("Miot", "小米IoT平台，可以控制智能家居")
        {
            _account = new MiAccount();
            _miioService = new MiioService(_account);
            _minaService = new MinaService(_account);
            _settings = new Settings("ThingMiot");
            
            Methods.AddMethod("XiaoAiAction", "小爱音箱，可以执行文本指令，用于控制智能家居",
                new ParameterList(new[]
                {
                    new Parameter<string>("command", "文本指令，例如打开灯")
                }),
                XiaoAiAction);
        }

        private void XiaoAiAction(ParameterList parameters)
        {
            UniTask.Void(async () =>
            {
                var did = _settings.GetString("xiaoai_did");
                var command = parameters.GetValue<string>("command");
                var (success, data) = await _miioService.Action(did, (5, 5), command, "0");
                if (!success) await Context.UIManager.ShowNotificationUI(data);
            });
        }
    }
}