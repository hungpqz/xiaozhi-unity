using System.Threading;
using UnityEngine;

namespace XiaoZhi.Unity
{
    public class ULipSyncAudioProxy
    {
        private readonly uLipSync.uLipSync _uLipSync;

        private readonly AudioCodec _codec;
        
        private CancellationTokenSource _updateCts;

        private readonly float[] _buffer;

        public ULipSyncAudioProxy(uLipSync.uLipSync uLipSync, AudioCodec codec)
        {
            _codec = codec;
            _uLipSync = uLipSync;
            _uLipSync.profile.sampleCount = AudioCodec.SpectrumWindowSize;
            _uLipSync.profile.targetSampleRate = codec.OutputSampleRate;
            var configuration = AudioSettings.GetConfiguration();
            configuration.sampleRate = codec.OutputSampleRate;
            AudioSettings.Reset(configuration);
            _buffer = new float[_uLipSync.profile.sampleCount];
        }

        public void Update()
        {
            if (!_codec.GetOutputSpectrum(false, out var data)) return;
            data.CopyTo(_buffer);
            _uLipSync.OnDataReceived(_buffer, _codec.OutputChannels);
        }
    }
}