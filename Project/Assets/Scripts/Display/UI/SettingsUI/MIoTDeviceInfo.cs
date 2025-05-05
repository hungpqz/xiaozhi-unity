using Cysharp.Threading.Tasks;
using TMPro;
using XiaoZhi.Unity.MIoT;

namespace XiaoZhi.Unity
{
    public class MIoTDeviceInfo : BaseUI
    {
        public class Data : BaseUIData
        {
            public MiotDevice Device;
        }
        
        private TMP_Text _text;
        
        public override string GetResourcePath()
        {
            return "Assets/Res/UI/SettingsUI/MIoTDeviceInfo.prefab";
        }

        protected override void OnInit()
        {
            _text = GetComponent<TMP_Text>(Tr, "Viewport/Content/Text");
        }

        protected override async UniTask OnShow(BaseUIData data = null)
        {
            if (data is not Data deviceInfo) return;
            var spec = await MiotSpec.Fetch(deviceInfo.Device.Type);
            _text.text = $"{deviceInfo.Device}\n{spec}";
        }

        protected override async UniTask OnHide()
        {
            await UniTask.CompletedTask;
        }

    }
}