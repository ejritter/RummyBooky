
namespace RummyBooky.Services;

public class AppAudioService : IAppAudioService
{
 private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;

    // Cache audio in memory to allow seamless restarts
    private MemoryStream? _audioBuffer;

    public AppAudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public void Mute()
    {
        if (_player is not null) _player.Volume = 0.0;
    }

    public void Unmute()
    {
        if (_player is not null) _player.Volume = 0.5;
    }

    public bool IsPlaying => _player?.IsPlaying ?? false;

    public double Volume
    {
        get => _player?.Volume ?? 0.5;
        set { if (_player is not null) _player.Volume = value; }
    }

    public async Task StartAsync()
    {
        if (_player is not null && _player.IsPlaying)
            return;

        if (_audioBuffer is null)
        {
            // Load once from app package to memory
            using var packageStream = await FileSystem.OpenAppPackageFileAsync("the_gambler.mp3");
            _audioBuffer = new MemoryStream();
            await packageStream.CopyToAsync(_audioBuffer);
        }

        CreatePlayerFromBufferAndPlay();
    }

    public void Pause() => _player?.Pause();

    public void Resume()
    {
        if (_player is null)
        {
            // If player was disposed (e.g., GC, lifecycle), recreate it
            if (_audioBuffer is not null)
            {
                CreatePlayerFromBufferAndPlay();
                return;
            }
        }

        _player?.Play();
    }

    public void Stop()
    {
        if (_player is null) return;

        _player.PlaybackEnded -= OnPlaybackEnded;
        _player.Stop();
        _player.Dispose();
        _player = null;
    }

    private void CreatePlayerFromBufferAndPlay()
    {
        if (_audioBuffer is null) return;

        // Create a fresh readable stream every time
        var fresh = new MemoryStream(_audioBuffer.ToArray());
        _player = _audioManager.CreatePlayer(fresh);

        _player.Loop = true;
        _player.Volume = _player.Volume is > 0 and <= 1 ? _player.Volume : 0.5;

        // Ensure single subscription for fallback
        _player.PlaybackEnded -= OnPlaybackEnded;
        _player.PlaybackEnded += OnPlaybackEnded;

        _player.Play();
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        // Recreate player from buffer and play again for gapless loop
        try
        {
            // Clean up old player
            if (_player is not null)
            {
                _player.PlaybackEnded -= OnPlaybackEnded;
                _player.Dispose();
                _player = null;
            }

            CreatePlayerFromBufferAndPlay();
        }
        catch
        {
            // Swallow to avoid crashing the app if audio fails; consider logging if you have Serilog here
        }
    }
}
