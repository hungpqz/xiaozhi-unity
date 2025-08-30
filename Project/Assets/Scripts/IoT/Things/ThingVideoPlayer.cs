using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace XiaoZhi.Unity.IoT
{
    public class ThingVideoPlayer : Thing
    {
        private VideoPlayer _videoPlayer;

        public bool IsPlaying => _videoPlayer.IsPlaying;

        public ThingVideoPlayer() : base("VideoPlayer", "视频播放器")
        {
        }

        public override async UniTask Load()
        {
            var go = GameObject.Find("VideoPlayer");
            if (!go) throw new NullReferenceException("Missing VideoPlayer");
            var playerComp = go.GetComponent<UnityEngine.Video.VideoPlayer>();
            if (!playerComp) throw new NullReferenceException("Missing VideoPlayer");
            _videoPlayer = new VideoPlayer(playerComp);
            var videoNames = "视频名称, " + string.Join(" 或 ", AppPresets.Instance.Videos.Select(i => i.Name));
            _properties.AddProperty("IsPlaying", "是否播放中", () => _videoPlayer.IsPlaying);
            _methods.AddMethod("PlayVideo", "播放视频",
                new ParameterList(new[]
                {
                    new Parameter<string>("videoName", videoNames)
                }),
                PlayVideo);
            _methods.AddMethod("StopVideo", "停止播放视频", new ParameterList(), StopVideo);
            await base.Load();
        }

        private void PlayVideo(ParameterList parameters)
        {
            PlayVideo(parameters.GetValue<string>("videoName")).Forget();
        }

        public async UniTask PlayVideo(string videoName)
        {
            var video = AppPresets.Instance.Videos.FirstOrDefault(i =>
                i.Name.Equals(videoName, StringComparison.InvariantCultureIgnoreCase));
            if (video == null) return;
            var clip = await Addressables.LoadAssetAsync<UnityEngine.Video.VideoClip>(video.Path);
            if (!clip) return;
            var codec = _context.App.GetCodec();
            codec.EnableInput(false);
            await _context.App.GetDisplay().Hide();
            var playerHandle = _videoPlayer.Play(clip);
            if (_videoPlayer.IsPlaying) await _context.App.UpdateIotStates();
            await playerHandle;
            await StopVideo();
        }

        private void StopVideo(ParameterList parameters)
        {
            StopVideo().Forget();
        }

        public async UniTask StopVideo()
        {
            _videoPlayer.Stop();
            var codec = _context.App.GetCodec();
            codec.EnableInput(true);
            await _context.App.GetDisplay().Show();
            await _context.App.UpdateIotStates();
        }
    }
}