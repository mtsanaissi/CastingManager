using CastingManager.UWP.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Casting;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace CastingManager.UWP.Services
{
    public class CastingService
    {
        private CastingDevicePicker _picker;
        private DeviceWatcher? _deviceWatcher;
        private CancellationTokenSource? _connectionCts;

        public ObservableCollection<CastingDeviceModel> Devices { get; }

        public CastingService(ObservableCollection<CastingDeviceModel> devices)
        {
            Devices = devices;
            _picker = new CastingDevicePicker();
            _picker.CastingDeviceSelected += Picker_CastingDeviceSelected;
        }

        public void ShowPicker(Windows.Foundation.Rect selectionRect)
        {
            _picker.Show(selectionRect);
        }

        public void StartDiscovery()
        {
            if (_deviceWatcher == null)
            {
                string selector = CastingDevice.GetDeviceSelector(CastingPlaybackTypes.Video);
                _deviceWatcher = DeviceInformation.CreateWatcher(selector);

                _deviceWatcher.Added += DeviceWatcher_Added;
                _deviceWatcher.Removed += DeviceWatcher_Removed;
                _deviceWatcher.Updated += DeviceWatcher_Updated;
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

        public async Task<bool> ConnectToDeviceAsync(CastingDeviceModel deviceModel)
        {
            if (deviceModel == null || deviceModel.CastingDevice == null) return false;

            _connectionCts = new CancellationTokenSource();
            deviceModel.IsRetrying = false;
            deviceModel.RetryAttempts = 0;

            try
            {
                var connection = deviceModel.CastingDevice.CreateCastingConnection();
                if(connection == null) return false;

                connection.StateChanged += Connection_StateChanged;
                connection.ErrorOccurred += Connection_ErrorOccurred;

                var coreWindow = Windows.UI.Core.CoreWindow.GetForCurrentThread();
                if (coreWindow == null) return false;

                var dispatcher = coreWindow.Dispatcher;
                if (dispatcher == null) return false;

                var currentView = ApplicationView.GetForCurrentView();
                if (currentView == null) return false;

                var mainViewId = currentView.Id;

                await ProjectionManager.StartProjectingAsync(mainViewId, mainViewId);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
                deviceModel.ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task DisconnectFromDeviceAsync(CastingDeviceModel deviceModel)
        {
            if (deviceModel == null) return;

            _connectionCts?.Cancel();

            try
            {
                var currentView = ApplicationView.GetForCurrentView();
                if (currentView == null) return;

                await ProjectionManager.StopProjectingAsync(currentView.Id, currentView.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disconnecting from device: {ex.Message}");
            }
        }

        private async void Connection_ErrorOccurred(CastingConnection sender, CastingConnectionErrorOccurredEventArgs args)
        {
            if (sender == null || args == null) return;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var device = Devices.FirstOrDefault(d => d.CastingDevice == sender.Device);
                if (device != null)
                {
                    device.ErrorMessage = args.Message;
                    if (device.RetryAttempts < 5)
                    {
                        device.IsRetrying = true;
                        device.IncrementRetryAttempts();
                        var timer = new Timer(async (state) => await ConnectToDeviceAsync(device), null, (int)Math.Pow(2, device.RetryAttempts) * 1000, Timeout.Infinite);
                    }
                    else
                    {
                        device.IsRetrying = false;
                    }
                }
            });
        }

        private async void Connection_StateChanged(CastingConnection sender, object args)
        {
            if (sender == null) return;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var device = Devices.FirstOrDefault(d => d.CastingDevice == sender.Device);
                if (device != null)
                {
                    device.ConnectionState = sender.State;
                    switch (sender.State)
                    {
                        case CastingConnectionState.Disconnected:
                            device.ClearError();
                            break;
                        case CastingConnectionState.Connected:
                            device.ClearError();
                            break;
                    }
                }
            });
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (sender == null || args == null) return;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var deviceToUpdate = Devices.FirstOrDefault(d => d.DeviceId == args.Id);
                if (deviceToUpdate != null)
                {
                    var deviceInfo = await DeviceInformation.CreateFromIdAsync(args.Id);
                    if(deviceInfo != null)
                    {
                        deviceToUpdate.UpdateDeviceInfo(deviceInfo);
                    }
                }
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (sender == null || args == null) return;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var deviceToRemove = Devices.FirstOrDefault(d => d.DeviceId == args.Id);
                if (deviceToRemove != null)
                {
                    Devices.Remove(deviceToRemove);
                }
            });
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (sender == null || args == null) return;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Devices.Add(new CastingDeviceModel(args));
            });
        }

        private async void Picker_CastingDeviceSelected(CastingDevicePicker sender, CastingDeviceSelectedEventArgs args)
        {
            if (sender == null || args == null) return;

            var selectedDevice = args.SelectedCastingDevice;
            if (selectedDevice == null) return;

            var deviceModel = Devices.FirstOrDefault(d => d.DeviceId == selectedDevice.Id);
            if (deviceModel != null)
            {
                await ConnectToDeviceAsync(deviceModel);
            }
        }
    }
}
