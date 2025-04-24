using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace XiaoZhi.Unity
{
    public interface IDisplay : IDisposable
    {
        UniTask<bool> Load();

        void Start();
        
        void SetStatus(string status);
        
        void SetStatus(LocalizedString status);
        
        void SetEmotion(string emotion);

        void SetChatMessage(ChatRole role, string content);
        
        void SetChatMessage(ChatRole role, LocalizedString content);
    }
}