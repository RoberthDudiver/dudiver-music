using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Windows.Threading;

namespace DudiverMusic.Services;

/// <summary>Un dispositivo de salida de audio (parlantes, audífonos, etc.).</summary>
public sealed record AudioDevice(string Id, string Name)
{
    /// <summary>Id reservado para "seguir el predeterminado del sistema".</summary>
    public const string DefaultId = "";
    public bool IsDefault => Id == DefaultId;
}

/// <summary>
/// Reproductor basado en NAudio + WASAPI (modo compartido). Permite elegir el
/// dispositivo de salida sin afectar al resto del sistema. Reproduce MP3, WAV,
/// FLAC, M4A/AAC, WMA vía Media Foundation.
/// </summary>
public sealed class AudioPlayer : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly DispatcherTimer _timer;

    private WasapiOut? _output;
    private MediaFoundationReader? _reader;
    private MediaFoundationResampler? _resampler;
    private string? _currentFile;
    private string _deviceId = AudioDevice.DefaultId;
    private double _volume = 0.8;

    public AudioPlayer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => PositionChanged?.Invoke(this, Position);
    }

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public bool HasMedia => _reader is not null;

    public TimeSpan Position
    {
        get => _reader?.CurrentTime ?? TimeSpan.Zero;
        set { if (_reader is not null) _reader.CurrentTime = Clamp(value); }
    }

    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 1);
            if (_volumeProvider is not null) _volumeProvider.Volume = (float)_volume;
        }
    }

    private VolumeSampleProvider? _volumeProvider;

    public event EventHandler? MediaOpened;
    public event EventHandler? MediaEnded;
    public event EventHandler<Exception?>? MediaFailed;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler? PlayStateChanged;

    // ===================== Dispositivos de salida =====================

    /// <summary>Lista de salidas activas, con la opción "Predeterminado del sistema" primero.</summary>
    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        // El nombre del predeterminado lo pone la capa de UI (localizado).
        var list = new List<AudioDevice> { new(AudioDevice.DefaultId, "") };
        try
        {
            foreach (var d in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                list.Add(new AudioDevice(d.ID, d.FriendlyName));
        }
        catch { /* si falla la enumeración, al menos queda el predeterminado */ }
        return list;
    }

    public string CurrentDeviceId => _deviceId;

    /// <summary>Cambia la salida. Si hay algo cargado, re-enruta sin cortar la posición.</summary>
    public void SetOutputDevice(string deviceId)
    {
        if (deviceId == _deviceId) return;
        _deviceId = deviceId;

        if (_reader is null) return;

        var pos = Position;
        var wasPlaying = IsPlaying;
        DisposeOutput();
        BuildOutput();
        Position = pos;
        if (wasPlaying) Play();
    }

    // ===================== Reproducción =====================

    public void Load(string filePath)
    {
        DisposeOutput();
        _reader?.Dispose();

        _currentFile = filePath;
        _reader = new MediaFoundationReader(filePath);
        BuildOutput();
    }

    private void BuildOutput()
    {
        if (_reader is null) return;

        var device = ResolveDevice();
        _output = new WasapiOut(device, AudioClientShareMode.Shared, true, 150);
        _output.PlaybackStopped += OnPlaybackStopped;

        _volumeProvider = new VolumeSampleProvider(_reader.ToSampleProvider()) { Volume = (float)_volume };
        _resampler = new MediaFoundationResampler(_volumeProvider.ToWaveProvider(), device.AudioClient.MixFormat)
        {
            ResamplerQuality = 60
        };
        _output.Init(_resampler);
        MediaOpened?.Invoke(this, EventArgs.Empty);
    }

    private MMDevice ResolveDevice()
    {
        if (_deviceId != AudioDevice.DefaultId)
        {
            try
            {
                var match = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                                       .FirstOrDefault(d => d.ID == _deviceId);
                if (match is not null) return match;
            }
            catch { /* cae al predeterminado */ }
        }
        return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public void Play()
    {
        if (_output is null) return;
        _output.Play();
        _timer.Start();
        PlayStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        _output?.Pause();
        _timer.Stop();
        PlayStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void TogglePlayPause()
    {
        if (IsPlaying) Pause();
        else Play();
    }

    public void Stop()
    {
        _output?.Stop();
        _timer.Stop();
        Position = TimeSpan.Zero;
        PlayStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Seek(double seconds) => Position = TimeSpan.FromSeconds(seconds);

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null) { MediaFailed?.Invoke(this, e.Exception); return; }

        // WASAPI marca "stopped" también al llegar al final del archivo.
        if (_reader is not null && _reader.CurrentTime >= _reader.TotalTime - TimeSpan.FromMilliseconds(400))
            MediaEnded?.Invoke(this, EventArgs.Empty);
    }

    private TimeSpan Clamp(TimeSpan t)
    {
        if (t < TimeSpan.Zero) return TimeSpan.Zero;
        var dur = Duration;
        return dur > TimeSpan.Zero && t > dur ? dur : t;
    }

    private void DisposeOutput()
    {
        if (_output is not null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _output = null;
        }
        _resampler?.Dispose();
        _resampler = null;
        _volumeProvider = null;
    }

    public void Dispose()
    {
        _timer.Stop();
        DisposeOutput();
        _reader?.Dispose();
        _enumerator.Dispose();
    }
}
