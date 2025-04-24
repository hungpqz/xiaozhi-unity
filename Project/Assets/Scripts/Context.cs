using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using XiaoZhi.Unity.IOT;

namespace XiaoZhi.Unity
{
    public class Context
    {
        public App App { get; private set; }
        
        public ThingManager Thing { get; private set; }

        public UIManager UIManager { get; private set; }
        
        public bool Restarting { get; private set; }
        
        public void Init()
        {
            UIManager = new UIManager();
            UIManager.Inject(this);
            Thing = new ThingManager();
            Thing.Inject(this);
            Thing.AddThing(new ThingAppSettings());
            App = new App();
            App.Inject(this);
            Application.runInBackground = true;
        }

        public void Dispose()
        {
            UIManager.Dispose();
            App.Dispose();
            DOTween.KillAll();
            AppUtility.Clear();
            Resources.UnloadUnusedAssets();
        }

        public void Start()
        {
            App.Start().Forget();
        }

        public async UniTask Restart(int delayMs = 0)
        {
            if (Restarting) return;
            Restarting = true;
            await UniTask.Delay(delayMs);
            Dispose();
            await UniTask.Delay(1000);
            Init();
            Start();
            Restarting = false;
        }
    }
}