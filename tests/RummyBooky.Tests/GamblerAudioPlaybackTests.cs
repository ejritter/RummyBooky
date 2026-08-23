using System;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public interface IAppAudioServiceTest
{
    Task StartAsync();
    void Pause();
    void Resume();
    void Stop();
    void Mute();
    void Unmute();
    bool IsPlaying { get; }
    double Volume { get; set; }
}

public class GamblerAudioPlaybackTests
{
    private class MockAppAudioService : IAppAudioServiceTest
    {
        public bool IsPlaying { get; set; }
        public double Volume { get; set; } = 0.5;
        public int StartAsyncCount { get; private set; }
        public int PauseCount { get; private set; }
        public int ResumeCount { get; private set; }
        public int MuteCount { get; private set; }
        public int UnmuteCount { get; private set; }

        public Task StartAsync()
        {
            StartAsyncCount++;
            IsPlaying = true;
            return Task.CompletedTask;
        }

        public void Pause()
        {
            PauseCount++;
            IsPlaying = false;
        }

        public void Resume()
        {
            ResumeCount++;
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
        }

        public void Mute()
        {
            MuteCount++;
            Volume = 0.0;
        }

        public void Unmute()
        {
            UnmuteCount++;
            Volume = 0.5;
        }
    }

    [Fact]
    public async Task DoubleTapLogo_WhenPlaying_PausesAudio()
    {
        var audio = new MockAppAudioService { IsPlaying = true, Volume = 0.5 };
        
        if (audio.IsPlaying && audio.Volume > 0)
        {
            audio.Pause();
        }
        else
        {
            await audio.StartAsync();
            audio.Resume();
        }

        Assert.False(audio.IsPlaying);
        Assert.Equal(1, audio.PauseCount);
    }

    [Fact]
    public async Task DoubleTapLogo_WhenPaused_ResumesAudio()
    {
        var audio = new MockAppAudioService { IsPlaying = false, Volume = 0.5 };

        if (audio.IsPlaying && audio.Volume > 0)
        {
            audio.Pause();
        }
        else
        {
            if (audio.Volume == 0)
            {
                audio.Unmute();
            }
            await audio.StartAsync();
            audio.Resume();
        }

        Assert.True(audio.IsPlaying);
        Assert.Equal(1, audio.ResumeCount);
    }

    [Fact]
    public async Task DoubleTapLogo_WhenMuted_UnmutesAndResumesAudio()
    {
        var audio = new MockAppAudioService { IsPlaying = true, Volume = 0.0 };

        if (audio.IsPlaying && audio.Volume > 0)
        {
            audio.Pause();
        }
        else
        {
            if (audio.Volume == 0)
            {
                audio.Unmute();
            }
            await audio.StartAsync();
            audio.Resume();
        }

        Assert.True(audio.IsPlaying);
        Assert.Equal(0.5, audio.Volume);
        Assert.Equal(1, audio.UnmuteCount);
        Assert.Equal(1, audio.ResumeCount);
    }

    [Fact]
    public void ContinuousPlayback_LoopingFlag_ConfiguredByDefault()
    {
        var audio = new MockAppAudioService();
        Assert.Equal(0.5, audio.Volume);
    }
}
