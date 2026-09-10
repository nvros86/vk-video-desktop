using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class PlaybackService
{
    private PlaybackState _state = new();

    public PlaybackState State => _state;

    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler<string>? VideoChanged;
    public event EventHandler? PlaybackCompleted;

    public void UpdateState(Video video, string playbackUrl)
    {
        _state.CurrentVideoId = video.Id;
        _state.Duration = video.Duration;
        _state.Position = TimeSpan.Zero;
        _state.IsPlaying = true;
        StateChanged?.Invoke(this, _state);
        VideoChanged?.Invoke(this, video.Id);
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

    public void SetQueue(IReadOnlyList<string> videoIds, int startIndex = 0)
    {
        _state.Queue.Clear();
        _state.Queue.AddRange(videoIds);
        _state.CurrentQueueIndex = startIndex;
        StateChanged?.Invoke(this, _state);
    }

    public void ClearQueue()
    {
        _state.Queue.Clear();
        _state.CurrentQueueIndex = -1;
        _state.CurrentVideoId = null;
        _state.IsPlaying = false;
        StateChanged?.Invoke(this, _state);
    }

    public string? GetNextVideoId()
    {
        if (_state.Queue.Count == 0) return null;
        if (_state.CurrentQueueIndex < _state.Queue.Count - 1)
        {
            _state.CurrentQueueIndex++;
            return _state.Queue[_state.CurrentQueueIndex];
        }
        return null;
    }

    public string? GetPreviousVideoId()
    {
        if (_state.Queue.Count == 0) return null;
        if (_state.CurrentQueueIndex > 0)
        {
            _state.CurrentQueueIndex--;
            return _state.Queue[_state.CurrentQueueIndex];
        }
        return null;
    }

    public void Enqueue(string videoId)
    {
        _state.Queue.Add(videoId);
        StateChanged?.Invoke(this, _state);
    }

    public bool HasNext => _state.Queue.Count > 0 && _state.CurrentQueueIndex < _state.Queue.Count - 1;
    public bool HasPrevious => _state.Queue.Count > 0 && _state.CurrentQueueIndex > 0;

    public void SetPlaybackSpeed(double speed)
    {
        _state.PlaybackSpeed = speed;
        StateChanged?.Invoke(this, _state);
    }

    public void NotifyPlaybackCompleted() => PlaybackCompleted?.Invoke(this, EventArgs.Empty);
}
