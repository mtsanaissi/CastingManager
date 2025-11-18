using CastingManager.UWP.Models;
using CastingManager.UWP.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CastingManager.UWP
{
    public sealed partial class MainPage : Page
    {
        private readonly ObservableCollection<CastingDeviceModel> _devices = new ObservableCollection<CastingDeviceModel>();
        private CastingService _castingService;
        public ObservableCollection<CastingDeviceModel> Devices => _devices;

        public MainPage()
        {
            InitializeComponent();
            // Subscribe to Loaded first to ensure _castingService is always initialized.
            Loaded += MainPage_Loaded;
            _devices.CollectionChanged += Devices_CollectionChanged;

            UpdateDeviceSummary();
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _castingService = new CastingService(_devices);
            await _castingService.EnsureDevicesLoadedAsync();
            _castingService.StartDiscovery();
        }

        private void StartCastingButton_Click(object sender, RoutedEventArgs e)
        {
            _castingService.ShowPicker(new Windows.Foundation.Rect(0, 0, 10, 10));
        }

        private async void StartDiscoveryButton_Click(object sender, RoutedEventArgs e)
        {
            await _castingService.EnsureDevicesLoadedAsync();
            _castingService.StartDiscovery();
        }

        private void StopDiscoveryButton_Click(object sender, RoutedEventArgs e)
        {
            _castingService.StopDiscovery();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Models.CastingDeviceModel device)
            {
                await _castingService.ConnectToDeviceAsync(device);
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Models.CastingDeviceModel device)
            {
                await _castingService.DisconnectFromDeviceAsync(device);
            }
        }

        private void Devices_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateDeviceSummary();
            Debug.WriteLine($"[DeviceList] Change: {e.Action}, Total: {_devices.Count}");
        }

        private void UpdateDeviceSummary()
        {
            if (DeviceSummaryText != null)
            {
                DeviceSummaryText.Text = $"Devices: {_devices.Count}";
            }

            if (EmptyStateText != null && DeviceListView != null)
            {
                EmptyStateText.Visibility = _devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                DeviceListView.Visibility = _devices.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void DeviceListView_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DeviceList] ListView loaded");
        }
    }
}
