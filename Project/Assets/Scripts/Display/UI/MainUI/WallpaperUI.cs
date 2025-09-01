using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.Video;

namespace XiaoZhi.Unity
{
    public class WallpaperUI : BaseUI
    {
        private GameObject _goSprite;
        private Image _spriteImage;
        private AspectRatioFitter _spriteAspect;

        private GameObject _goVideo;
        private RawImage _videoImage;
        private UnityEngine.Video.VideoPlayer _rawVideoPlayer;
        private VideoPlayer _videoPlayer;

        public override string GetResourcePath()
        {
            return "Assets/Res/UI/MainUI/WallpaperUI.prefab";
        }

        protected override void OnInit()
        {
            _goSprite = GetGo(Tr, "Sprite");
            _spriteImage = GetComponent<Image>(_goSprite.transform);
            _spriteAspect = GetComponent<AspectRatioFitter>(_goSprite.transform);

            _goVideo = GetGo(Tr, "Video");
            _videoImage = GetComponent<RawImage>(_goVideo.transform);
            _rawVideoPlayer = GetComponent<UnityEngine.Video.VideoPlayer>(_goVideo.transform);
            _videoPlayer = new VideoPlayer(_rawVideoPlayer);
        }

        protected override async UniTask OnShow(BaseUIData data = null)
        {
            AppSettings.Instance.OnWallPaperUpdate -= OnWallpaperUpdate;
            AppSettings.Instance.OnWallPaperUpdate += OnWallpaperUpdate;
            OnWallpaperUpdate(AppSettings.Instance.GetWallpaper());
            await UniTask.CompletedTask;
        }

        protected override async UniTask OnHide()
        {
            AppSettings.Instance.OnWallPaperUpdate -= OnWallpaperUpdate;
            await UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Dispose();
                _videoPlayer = null;
            }

            if (_rawVideoPlayer.targetTexture)
            {
                _rawVideoPlayer.targetTexture.Release();
                _rawVideoPlayer.targetTexture = null;
            }
        }

        private void OnWallpaperUpdate(string paperName)
        {
            var wallpaper = AppPresets.Instance.GetWallpaper(paperName);
            wallpaper ??= AppPresets.Instance.GetWallpaper();
            _goSprite.SetActive(wallpaper.Type == WallpaperType.Sprite);
            _goVideo.SetActive(wallpaper.Type == WallpaperType.Video);
            if (wallpaper.Type != WallpaperType.Video) _videoPlayer.Stop();
            switch (wallpaper.Type)
            {
                case WallpaperType.Sprite:
                {
                    UniTask.Void(async () =>
                    {
                        var sprite = await Addressables.LoadAssetAsync<Sprite>(wallpaper.Path);
                        if (sprite == null) return;
                        _spriteImage.sprite = sprite;
                        _spriteAspect.aspectRatio = sprite.rect.width / sprite.rect.height;
                    });
                    break;
                }
                case WallpaperType.Video:
                {
                    UniTask.Void(async () =>
                    {
                        if (!_rawVideoPlayer.targetTexture)
                        {
                            var rect = _videoImage.rectTransform.rect;
                            var rt = new RenderTexture((int)rect.width, (int)rect.height, 0,
                                RenderTextureFormat.ARGB32);
                            _videoImage.texture = rt;
                            _rawVideoPlayer.targetTexture = rt;
                        }

                        _rawVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                        var video = await Addressables.LoadAssetAsync<VideoClip>(wallpaper.Path);
                        if (video == null) return;
                        _videoPlayer.Play(video, -1).Forget();
                    });
                    break;
                }
                case WallpaperType.Default:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}