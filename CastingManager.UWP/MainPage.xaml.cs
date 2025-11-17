using CastingManager.UWP.Models;
using CastingManager.UWP.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CastingManager.UWP
{
    public sealed partial class MainPage : Page
    {
        private readonly ObservableCollection<CastingDeviceModel> _devices = new ObservableCollection<CastingDeviceModel>();
        private CastingService _castingService;

        public MainPage()
        {
            InitializeComponent();
            // Subscribe to Loaded first to ensure _castingService is always initialized.
            Loaded += MainPage_Loaded;

            try
            {
                if (DeviceListView == null)
                {
                    Debug.WriteLine("DeviceListView is null!");
                    return;
                }

                Debug.WriteLine("About to set ItemsSource...");
                DeviceListView.ItemsSource = _devices;
                Debug.WriteLine("ItemsSource set successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _castingService = new CastingService(_devices);
            // Discovery is now started here, after the service is created.
            _castingService.StartDiscovery();
        }

        private void StartCastingButton_Click(object sender, RoutedEventArgs e)
        {
            _castingService.ShowPicker(new Windows.Foundation.Rect(0, 0, 10, 10));
        }

        private void StartDiscoveryButton_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
