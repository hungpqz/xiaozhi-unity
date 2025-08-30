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
        private RawImage _image;
        private UnityEngine.Video.VideoPlayer _rawVideoPlayer;
        private VideoPlayer _videoPlayer;

        public override string GetResourcePath()
        {
            return "Assets/Res/UI/MainUI/WallpaperUI.prefab";
        }

        protected override void OnInit()
        {
            _image = GetComponent<RawImage>(Tr);
            _rawVideoPlayer = GetComponent<UnityEngine.Video.VideoPlayer>(Tr);
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
            switch (wallpaper.Type)
            {
                case WallpaperType.Default:
                {
                    _videoPlayer.Stop();
                    _image.enabled = false;
                    break;
                }
                case WallpaperType.Texture:
                {
                    UniTask.Void(async () =>
                    {
                        _videoPlayer.Stop();
                        var texture = await Addressables.LoadAssetAsync<Texture>(wallpaper.Path);
                        if (texture == null) return;
                        _image.texture = texture;
                        _image.enabled = true;
                    });
                    break;
                }
                case WallpaperType.Video:
                {
                    UniTask.Void(async () =>
                    {
                        if (!_rawVideoPlayer.targetTexture)
                        {
                            var rect = _image.rectTransform.rect;
                            var rt = new RenderTexture((int)rect.width, (int)rect.height, 0,
                                RenderTextureFormat.ARGB32);
                            _image.texture = rt;
                            _rawVideoPlayer.targetTexture = rt;
                        }
                        
                        _rawVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                        var video = await Addressables.LoadAssetAsync<VideoClip>(wallpaper.Path);
                        if (video == null) return;
                        _videoPlayer.Play(video, -1).Forget();
                        _image.enabled = true;
                    });
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}