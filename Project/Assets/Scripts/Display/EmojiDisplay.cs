using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace XiaoZhi.Unity
{
    public class EmojiDisplay : IDisplay
    {
        private readonly Context _context;
        
        private WallpaperUI _wallpaperUI;

        private EmojiMainUI _emojiMainUI;
        
        public EmojiDisplay(Context context)
        {
            _context = context;
        }
        
        public void Dispose()
        {
            _wallpaperUI.Dispose();
            _emojiMainUI.Dispose();
        }
        
        public async UniTask<bool> Load()
        {
            _wallpaperUI = await _context.UIManager.ShowBgUI<WallpaperUI>();
            _emojiMainUI = await _context.UIManager.ShowSceneUI<EmojiMainUI>();
            return true;
        }

        public void Start()
        {
            
        }

        public void SetStatus(string status)
        {
            _emojiMainUI.SetStatus(status);
        }

        public void SetStatus(LocalizedString status)
        {
            _emojiMainUI.SetStatus(status);
        }

        public void SetEmotion(string emotion)
        {
            _emojiMainUI.SetEmotion(emotion);
        }

        public void SetChatMessage(ChatRole role, string content)
        {
            _emojiMainUI.SetChatMessage(role, content);
        }

        public void SetChatMessage(ChatRole role, LocalizedString content)
        {
            _emojiMainUI.SetChatMessage(role, content);
        }

        public async UniTask Show()
        {
            await _wallpaperUI.Show();
            await _emojiMainUI.Show();
        }

        public async UniTask Hide()
        {
            await _emojiMainUI.Hide();
            await _wallpaperUI.Hide();
        }
    }
}