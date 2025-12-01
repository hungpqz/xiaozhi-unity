using System.Linq;
using Cysharp.Threading.Tasks;

namespace XiaoZhi.Unity.IoT
{
    public class ThingDance : Thing
    {
        public ThingDance() : base("Dance Controller", "Bộ điều khiển nhảy của nhân vật")
        {
        }

        public override async UniTask Load()
        {
            var dances = AppPresets.Instance.Dances;
            var names = "Tên điệu nhảy, " + string.Join(" hoặc ", dances.Select(i => i.Name));
            _methods.AddMethod("Dance", "Nhân vật nhảy",
                new ParameterList(new[]
                {
                    new Parameter<string>("name", names)
                }),
                Dance);
            _properties.AddProperty("IsEnabled", "Nhân vật có hỗ trợ nhảy không", IsEnabled);
            _properties.AddProperty("IsDancing", "Nhân vật có đang nhảy không", IsDancing);
            await base.Load();
        }
        
        private void Dance(ParameterList parameters)
        {
            _context.App.Dance(parameters.GetValue<string>("name")).Forget();
        }
        
        private bool IsEnabled()
        {
            return _context.App.GetDisplay() is VRMDisplay;
        }
        
        private bool IsDancing()
        {
            return _context.App.Talk.Stat == Talk.State.Dancing;
        }

    }
}
