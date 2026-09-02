using NAudio.Wave;
using ObsMusicPlayer.Services.Interfaces;

namespace ObsMusicPlayer.Services
{
    public class AudioService : IAudioService
    {
        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioFile;

        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

        public double PositionSeconds
        {
            get => _audioFile?.CurrentTime.TotalSeconds ?? 0;
            set
            {
                if (_audioFile != null)
                    _audioFile.CurrentTime = TimeSpan.FromSeconds(Math.Max(0, value));
            }
        }

        public double TotalSeconds => _audioFile?.TotalTime.TotalSeconds ?? 0;

        public float Volume
        {
            get => _waveOut?.Volume ?? 1f;
            set
            {
                if (_waveOut != null)
                    _waveOut.Volume = Math.Clamp(value, 0f, 1f);
            }
        }

        public void Play(string filePath)
        {
            Stop();

            _audioFile = new AudioFileReader(filePath);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFile);
            _waveOut.Play();
        }

        public void Pause()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Playing)
                _waveOut.Pause();
        }

        public void Resume()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Paused)
                _waveOut.Play();
        }

        public void Stop()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFile?.Dispose();
            _waveOut = null;
            _audioFile = null;
        }

        public void Dispose() => Stop();
    }
}
