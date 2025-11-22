using CastingManager.Core.DeviceFiltering;
using CastingManager.Core.Retry;
using CastingManager.UWP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Casting;
using Windows.UI.Core;

namespace CastingManager.UWP.Services
{
    public class CastingService
    {
        private readonly CastingDevicePicker _picker;
        private readonly CastingRetryOrchestrator _retryOrchestrator;
        private readonly CastingRetryOptions _retryOptions;
        private readonly GraphicsCaptureService _graphicsCaptureService;
        private readonly ScreenCastingSourceProvider _sourceProvider;
        private readonly Dictionary<string, CastingConnection> _activeConnections = new();
        private readonly string _deviceSelector;
        private static readonly string[] _requestedProperties = new[] { "System.Devices.Aep.Category" };

        private DeviceWatcher? _deviceWatcher;
        private CancellationTokenSource? _connectionCts;
        private bool _initialDevicesLoaded;

        public ObservableCollection<CastingDeviceModel> Devices { get; }

        public CastingService(ObservableCollection<CastingDeviceModel> devices)
        {
            Devices = devices ?? throw new ArgumentNullException(nameof(devices));

            _picker = new CastingDevicePicker();
            _picker.CastingDeviceSelected += Picker_CastingDeviceSelected;
            _picker.Filter.SupportsVideo = true;
            _picker.Filter.SupportsAudio = false;
            _picker.Filter.SupportsPictures = false;

            _deviceSelector = CastingDevice.GetDeviceSelector(CastingPlaybackTypes.Video);

            _retryOrchestrator = new CastingRetryOrchestrator();
            _retryOptions = new CastingRetryOptions
            {
                InitialDelay = TimeSpan.FromSeconds(3),
                MaxDelay = TimeSpan.FromSeconds(10),
                BackoffFactor = 1.5,
                MaxAttempts = 0 // keep retrying until the user cancels
            };

            _graphicsCaptureService = new GraphicsCaptureService();
            _sourceProvider = new ScreenCastingSourceProvider(_graphicsCaptureService);
        }

        public void ShowPicker(Rect selectionRect) => _picker.Show(selectionRect);

        public async Task EnsureDevicesLoadedAsync()
        {
            if (_initialDevicesLoaded)
            {
                return;
            }

            try
            {
                var snapshot = await DeviceInformation.FindAllAsync(_deviceSelector, _requestedProperties);
                Log($"Initial device snapshot count: {snapshot?.Count ?? 0}");
                foreach (var device in snapshot)
                {
                    await AddOrUpdateDeviceAsync(device);
                }

                _initialDevicesLoaded = true;
            }
            catch (Exception ex)
            {
                Log($"Device discovery snapshot failed: {ex.Message}");
            }
        }

        public void StartDiscovery()
        {
            if (_deviceWatcher == null)
            {
                _deviceWatcher = DeviceInformation.CreateWatcher(_deviceSelector, _requestedProperties);

                _deviceWatcher.Added += DeviceWatcher_Added;
                _deviceWatcher.Removed += DeviceWatcher_Removed;
                _deviceWatcher.Updated += DeviceWatcher_Updated;
                _deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                _deviceWatcher.Stopped += DeviceWatcher_Stopped;
            }

            if (_deviceWatcher.Status == DeviceWatcherStatus.Created ||
                _deviceWatcher.Status == DeviceWatcherStatus.Stopped ||
                _deviceWatcher.Status == DeviceWatcherStatus.Aborted)
            {
                _deviceWatcher.Start();
            }
        }

        public void StopDiscovery()
        {
            if (_deviceWatcher != null && _deviceWatcher.Status == DeviceWatcherStatus.Started)
            {
                _deviceWatcher.Stop();
            }
        }

        public async Task<bool> ConnectToDeviceAsync(CastingDeviceModel deviceModel, CancellationToken cancellationToken = default)
        {
            if (deviceModel == null)
            {
                return false;
            }

            if (!deviceModel.IsVideoCapable)
            {
                deviceModel.ErrorMessage = "Selected device does not support video casting.";
                return false;
            }

            bool captureReady;
            try
            {
                captureReady = await _sourceProvider.EnsureCastingSourceAsync();
            }
            catch (UnauthorizedAccessException)
            {
                captureReady = false;
            }

            if (!captureReady)
            {
                var message = _sourceProvider.LastErrorMessage ?? "Screen capture permission denied. Enable Settings > Privacy & security > Screen capture.";
                deviceModel.ErrorMessage = message;
                deviceModel.ConnectionState = CastingConnectionState.Disconnected;
                deviceModel.IsRetrying = false;
                return false;
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _connectionCts?.Cancel();
            _connectionCts = linkedCts;

            deviceModel.ClearError();
            deviceModel.IsRetrying = true;
            deviceModel.ConnectionState = CastingConnectionState.Connecting;

            try
            {
                var result = await _retryOrchestrator.ExecuteAsync(
                    async (attempt, token) =>
                    {
                        deviceModel.RetryAttempts = attempt;
                        return await TryStartCastingAsync(deviceModel, token).ConfigureAwait(false);
                    },
                    _retryOptions,
                    linkedCts.Token).ConfigureAwait(false);

                deviceModel.IsRetrying = false;

                if (result.IsSuccess)
                {
                    deviceModel.ConnectionState = CastingConnectionState.Connected;
                    deviceModel.ClearError();
                    return true;
                }

                if (!linkedCts.Token.IsCancellationRequested)
                {
                    deviceModel.ErrorMessage = "Unable to establish casting session.";
                }

                deviceModel.ConnectionState = CastingConnectionState.Disconnected;
                return false;
            }
            catch (OperationCanceledException)
            {
                deviceModel.IsRetrying = false;
                deviceModel.ConnectionState = CastingConnectionState.Disconnected;
                return false;
            }
        }

        public async Task DisconnectFromDeviceAsync(CastingDeviceModel deviceModel)
        {
            if (deviceModel == null)
            {
                return;
            }

            _connectionCts?.Cancel();
            await ReleaseConnectionAsync(deviceModel.DeviceId, disconnect: true).ConfigureAwait(false);

            _sourceProvider.Reset();
            deviceModel.ConnectionState = CastingConnectionState.Disconnected;
            deviceModel.IsRetrying = false;
            deviceModel.ClearError();
        }

        private async Task<bool> TryStartCastingAsync(CastingDeviceModel deviceModel, CancellationToken token)
        {
            var castingDevice = await EnsureCastingDeviceAsync(deviceModel, token).ConfigureAwait(false);
            if (castingDevice == null)
            {
                return false;
            }

            var castingSource = _sourceProvider.GetCastingSource();
            if (castingSource == null)
            {
                deviceModel.ErrorMessage = "Screen capture source unavailable.";
                return false;
            }

            var connection = castingDevice.CreateCastingConnection();
            RegisterActiveConnection(deviceModel.DeviceId, connection);

            try
            {
                var status = await connection.RequestStartCastingAsync(castingSource).AsTask(token).ConfigureAwait(false);
                if (status == CastingConnectionErrorStatus.Succeeded)
                {
                    return true;
                }

                deviceModel.ErrorMessage = status.ToString();
                await ReleaseConnectionAsync(deviceModel.DeviceId, disconnect: false).ConfigureAwait(false);
                return false;
            }
            catch (OperationCanceledException)
            {
                await ReleaseConnectionAsync(deviceModel.DeviceId, disconnect: false).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                deviceModel.ErrorMessage = ex.Message;
                await ReleaseConnectionAsync(deviceModel.DeviceId, disconnect: false).ConfigureAwait(false);
                return false;
            }
        }

        private async Task<CastingDevice?> EnsureCastingDeviceAsync(CastingDeviceModel deviceModel, CancellationToken token)
        {
            if (deviceModel.CastingDevice != null)
            {
                return deviceModel.CastingDevice;
            }

            try
            {
                var castingDevice = await CastingDevice.FromIdAsync(deviceModel.DeviceId).AsTask(token).ConfigureAwait(false);
                deviceModel.CastingDevice = castingDevice;
                return castingDevice;
            }
            catch (Exception ex)
            {
                deviceModel.ErrorMessage = ex.Message;
                return null;
            }
        }

        private void RegisterActiveConnection(string deviceId, CastingConnection connection)
        {
            if (_activeConnections.TryGetValue(deviceId, out var existing))
            {
                existing.StateChanged -= Connection_StateChanged;
                existing.ErrorOccurred -= Connection_ErrorOccurred;
            }

            connection.StateChanged += Connection_StateChanged;
            connection.ErrorOccurred += Connection_ErrorOccurred;
            _activeConnections[deviceId] = connection;
        }

        private async Task ReleaseConnectionAsync(string deviceId, bool disconnect)
        {
            if (_activeConnections.TryGetValue(deviceId, out var connection))
            {
                connection.StateChanged -= Connection_StateChanged;
                connection.ErrorOccurred -= Connection_ErrorOccurred;

                if (disconnect)
                {
                    try
                    {
                        await connection.DisconnectAsync().AsTask().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disconnecting from device: {ex.Message}");
                    }
                }

                _activeConnections.Remove(deviceId);
            }
        }

        private async void Connection_ErrorOccurred(CastingConnection sender, CastingConnectionErrorOccurredEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var device = Devices.FirstOrDefault(d => d.CastingDevice == sender.Device);
                if (device != null)
                {
                    device.ErrorMessage = args.Message;
                    device.ConnectionState = CastingConnectionState.Disconnected;
                }
            });
        }

        private async void Connection_StateChanged(CastingConnection sender, object args)
        {
            if (sender == null)
            {
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var device = Devices.FirstOrDefault(d => d.CastingDevice == sender.Device);
                if (device != null)
                {
                    device.ConnectionState = sender.State;
                    if (sender.State == CastingConnectionState.Connected)
                    {
                        device.ClearError();
                    }
                }
            });
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var deviceToUpdate = Devices.FirstOrDefault(d => d.DeviceId == args.Id);
                if (deviceToUpdate != null)
                {
                    var deviceInfo = await DeviceInformation.CreateFromIdAsync(args.Id);
                    deviceToUpdate.UpdateDeviceInfo(deviceInfo);
                }
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var deviceToRemove = Devices.FirstOrDefault(d => d.DeviceId == args.Id);
                if (deviceToRemove != null)
                {
                    await ReleaseConnectionAsync(deviceToRemove.DeviceId, disconnect: true);
                    Devices.Remove(deviceToRemove);
                }
            });
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Log("DeviceWatcher enumeration completed.");
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Log($"DeviceWatcher stopped with status: {sender?.Status}");
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            await AddOrUpdateDeviceAsync(args);
        }

        private async Task AddOrUpdateDeviceAsync(DeviceInformation deviceInfo)
        {
            if (deviceInfo == null)
            {
                return;
            }

            var isVideoDevice = IsVideoDevice(deviceInfo);
            if (!CastingDeviceFilter.ShouldInclude(isVideoDevice))
            {
                Log($"Skipping non-video device: {deviceInfo.Name} ({deviceInfo.Id})");
                return;
            }

            CastingDevice? castingDevice = null;
            try
            {
                castingDevice = await CastingDevice.FromIdAsync(deviceInfo.Id);
            }
            catch (Exception ex)
            {
                Log($"Unable to materialize casting device: {ex.Message}");
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var existing = Devices.FirstOrDefault(d => d.DeviceId == deviceInfo.Id);
                if (existing != null)
                {
                    existing.UpdateDeviceInfo(deviceInfo);
                    existing.CastingDevice = existing.CastingDevice ?? castingDevice;
                    existing.IsVideoCapable = isVideoDevice;
                    Log($"Device updated: {deviceInfo.Name} ({deviceInfo.Id})");
                }
                else
                {
                    Devices.Add(new CastingDeviceModel(deviceInfo, castingDevice, isVideoDevice));
                    Log($"Device added: {deviceInfo.Name} ({deviceInfo.Id})");
                }
            });
        }

        private static bool IsVideoDevice(DeviceInformation deviceInfo)
        {
            if (deviceInfo?.Properties != null &&
                deviceInfo.Properties.TryGetValue("System.Devices.Aep.Category", out var categoryValue))
            {
                var category = categoryValue?.ToString() ?? string.Empty;
                if (category.IndexOf("audio", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Log(string message)
        {
            Debug.WriteLine($"[CastingService] {message}");
        }

        private async void Picker_CastingDeviceSelected(CastingDevicePicker sender, CastingDeviceSelectedEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            var selectedDevice = args.SelectedCastingDevice;
            if (selectedDevice == null)
            {
                return;
            }

            var deviceModel = Devices.FirstOrDefault(d => d.DeviceId == selectedDevice.Id);
            if (deviceModel != null)
            {
                deviceModel.CastingDevice = selectedDevice;
                await ConnectToDeviceAsync(deviceModel);
            }
        }
    }
}
