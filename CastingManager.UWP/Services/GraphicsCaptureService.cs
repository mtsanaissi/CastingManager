using System;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Media.Core;
using WinRT.Interop;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.MediaProperties;

namespace CastingManager.UWP.Services
{
    public class GraphicsCaptureService
    {
        private GraphicsCaptureItem? _captureItem;
        private GraphicsCaptureSession? _session;
        private MediaStreamSource? _mediaStreamSource;
        private IDirect3DDevice _device;
        private Direct3D11CaptureFramePool? _framePool;
        private MediaStreamSourceSampleRequest? _sampleRequest;

        public GraphicsCaptureService()
        {
            _device = Direct3D11Helpers.CreateDevice();
        }

        public async Task<MediaStreamSource?> PickAndCaptureAsync()
        {
            StopCapture();

            var picker = new GraphicsCapturePicker();
            _captureItem = await picker.PickSingleItemAsync();

            if (_captureItem != null && _captureItem.Size.Width > 0 && _captureItem.Size.Height > 0)
            {
                try
                {
                    var videoProperties = VideoEncodingProperties.CreateH264();
                    videoProperties.Width = (uint)_captureItem.Size.Width;
                    videoProperties.Height = (uint)_captureItem.Size.Height;
                    videoProperties.Bitrate = 2 * 1024 * 1024; // 2 Mbps
                    videoProperties.FrameRate.Numerator = 30;
                    videoProperties.FrameRate.Denominator = 1;

                    var videoDescriptor = new VideoStreamDescriptor(videoProperties);

                    _mediaStreamSource = new MediaStreamSource(videoDescriptor);
                    _mediaStreamSource.SampleRequested += OnSampleRequested;

                    _framePool = Direct3D11CaptureFramePool.Create(
                        _device,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        1,
                        _captureItem.Size);

                    _framePool.FrameArrived += OnFrameArrived;

                    _session = _framePool.CreateCaptureSession(_captureItem);
                    _session.IsCursorCaptureEnabled = true;
                    _session.StartCapture();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Graphics capture failed: {ex.Message}");
                    StopCapture();
                    return null;
                }
            }

            return _mediaStreamSource;
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            if (_sampleRequest != null)
            {
                using (var frame = sender.TryGetNextFrame())
                {
                    if (frame != null)
                    {
                        var sample = MediaStreamSample.CreateFromDirect3D11Surface(frame.Surface, frame.SystemRelativeTime);
                        _sampleRequest.Sample = sample;
                    }
                }
            }
        }

        private void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            _sampleRequest = args.Request;
        }

        public void StopCapture()
        {
            if (_mediaStreamSource != null)
            {
                _mediaStreamSource.SampleRequested -= OnSampleRequested;
            }

            if (_framePool != null)
            {
                _framePool.FrameArrived -= OnFrameArrived;
            }

            _framePool?.Dispose();
            _session?.Dispose();
            _captureItem = null;
            _mediaStreamSource = null;
        }
    }
}
