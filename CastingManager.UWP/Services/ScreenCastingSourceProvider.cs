using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Capture;
using Windows.Media.Casting;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace CastingManager.UWP.Services
{
    public class ScreenCastingSourceProvider
    {
        private readonly GraphicsCaptureService _graphicsCaptureService;
        private MediaPlayer? _mediaPlayer;
        private CastingSource? _castingSource;
        private string? _lastErrorMessage;

        public ScreenCastingSourceProvider(GraphicsCaptureService graphicsCaptureService)
        {
            _graphicsCaptureService = graphicsCaptureService ?? throw new ArgumentNullException(nameof(graphicsCaptureService));
        }

        public string? LastErrorMessage => _lastErrorMessage;

        public async Task<bool> EnsureCastingSourceAsync()
        {
            _lastErrorMessage = null;

            if (_castingSource != null)
            {
                return true;
            }

            if (!GraphicsCaptureSession.IsSupported())
            {
                _lastErrorMessage = "This PC doesn't support screen capture (WDDM 2.0+ GPU required).";
                return false;
            }

            var dispatcher = CoreApplication.MainView?.CoreWindow?.Dispatcher;
            if (dispatcher == null)
            {
                _lastErrorMessage = "Unable to access UI dispatcher for capture initialization.";
                return false;
            }

            try
            {
                if (dispatcher.HasThreadAccess)
                {
                    return await InitializeCaptureAsync();
                }

                var tcs = new TaskCompletionSource<bool>();
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        var initialized = await InitializeCaptureAsync();
                        tcs.SetResult(initialized);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                return await tcs.Task;
            }
            catch (UnauthorizedAccessException)
            {
                _lastErrorMessage = "Screen capture permission denied.";
                return false;
            }
            catch (Exception ex)
            {
                _lastErrorMessage = $"Screen capture failed: {ex.Message}";
                return false;
            }
        }

        public CastingSource? GetCastingSource() => _castingSource;

        public void Reset()
        {
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
            _castingSource = null;
            _lastErrorMessage = null;
            _graphicsCaptureService.StopCapture();
        }

        private async Task<bool> InitializeCaptureAsync()
        {
            try
            {
                var mediaStreamSource = await _graphicsCaptureService.PickAndCaptureAsync();
                if (mediaStreamSource == null)
                {
                    _lastErrorMessage = "Screen capture canceled.";
                    return false;
                }

                _mediaPlayer = new MediaPlayer
                {
                    Source = MediaSource.CreateFromMediaStreamSource(mediaStreamSource),
                    IsLoopingEnabled = true
                };

                _mediaPlayer.Play();
                _castingSource = _mediaPlayer.GetAsCastingSource();
                return _castingSource != null;
            }
            catch (UnauthorizedAccessException)
            {
                _lastErrorMessage = "Screen capture permission denied.";
                return false;
            }
            catch (Exception ex)
            {
                _lastErrorMessage = $"Screen capture failed: {ex.Message}";
                return false;
            }
        }
    }
}
