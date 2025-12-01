using System.Linq;
using Cysharp.Threading.Tasks;

namespace XiaoZhi.Unity.IoT
{
    public class ThingAnimation : Thing
    {
        public ThingAnimation() : base("Animation Controller", "Bộ điều khiển động tác nhân vật")
        {
        }

        public override async UniTask Load()
        {
            var animLib = AppPresets.Instance.GetAnimationLib();
            var labels = "Nhãn động tác, " + string.Join(" hoặc ", animLib.Sets.SelectMany(i => i.Labels));
            _methods.AddMethod("Animate", "Nhân vật thực hiện động tác",
                new ParameterList(new[]
                {
                    new Parameter<string>("label", labels)
                }),
                Animate);
            _properties.AddProperty("IsEnabled", "Nhân vật có hỗ trợ động tác không", IsEnabled);
            await base.Load();
        }

        private void Animate(ParameterList parameters)
        {
            (_context.App.GetDisplay() as VRMDisplay)?.Animate(parameters.GetValue<string>("label"));
        }
        
        private bool IsEnabled()
        {
            return _context.App.GetDisplay() is VRMDisplay;
        }
    }
}
