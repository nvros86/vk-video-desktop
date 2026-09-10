using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class PlaybackService
{
    private PlaybackState _state = new();

    public PlaybackState State => _state;

    public event EventHandler<PlaybackState>? StateChanged;

    public void UpdateState(Video video, string playbackUrl)
    {
        _state.CurrentVideoId = video.Id;
        _state.Duration = video.Duration;
        _state.Position = TimeSpan.Zero;
        _state.IsPlaying = true;
        StateChanged?.Invoke(this, _state);
    }

    public void TogglePlayPause()
    {
        _state.IsPlaying = !_state.IsPlaying;
        StateChanged?.Invoke(this, _state);
    }

    public void SetPosition(TimeSpan position)
    {
        _state.Position = position;
        StateChanged?.Invoke(this, _state);
    }

    public void SetVolume(double volume)
    {
        _state.Volume = volume;
        _state.IsMuted = volume == 0;
        StateChanged?.Invoke(this, _state);
    }
}
