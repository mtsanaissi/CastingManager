using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
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

        public ScreenCastingSourceProvider(GraphicsCaptureService graphicsCaptureService)
        {
            _graphicsCaptureService = graphicsCaptureService ?? throw new ArgumentNullException(nameof(graphicsCaptureService));
        }

        public async Task<bool> EnsureCastingSourceAsync()
        {
            if (_castingSource != null)
            {
                return true;
            }

            var dispatcher = CoreApplication.MainView?.CoreWindow?.Dispatcher;
            if (dispatcher == null)
            {
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
                return false;
            }
        }

        public CastingSource? GetCastingSource() => _castingSource;

        public void Reset()
        {
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
            _castingSource = null;
            _graphicsCaptureService.StopCapture();
        }

        private async Task<bool> InitializeCaptureAsync()
        {
            var mediaStreamSource = await _graphicsCaptureService.PickAndCaptureAsync();
            if (mediaStreamSource == null)
            {
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
    }
}
