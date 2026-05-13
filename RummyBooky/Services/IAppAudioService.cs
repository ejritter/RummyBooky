using System;
using System.Collections.Generic;
using System.Text;

namespace RummyBooky.Services;

public interface IAppAudioService
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
