using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Windows.Media.Casting;
using Windows.Devices.Enumeration;

namespace CastingManager.UWP.Models
{
    /// <summary>
    /// Represents a casting-capable device with connection status and capabilities
    /// </summary>
    public class CastingDeviceModel : INotifyPropertyChanged
    {
        private DeviceInformation _deviceInfo;
        private CastingDevice? _castingDevice;
        private CastingConnectionState _connectionState = CastingConnectionState.Disconnected;
        private string _connectionStatus = "Disconnected";
        private DateTime _lastSeen = DateTime.Now;
        private bool _isRetrying = false;
        private int _retryAttempts = 0;
        private string _errorMessage = string.Empty;
        private double _signalStrength = 0.0;
        private bool _isVideoCapable = true;

        public CastingDeviceModel(DeviceInformation deviceInfo, CastingDevice? castingDevice = null, bool isVideoCapable = true)
        {
            _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
            _castingDevice = castingDevice;
            DeviceId = deviceInfo.Id;
            DeviceName = deviceInfo.Name;
            LastSeen = DateTime.Now;
            _isVideoCapable = isVideoCapable;
            
            // Parse device capabilities from properties
            ParseDeviceCapabilities();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        #region Public Properties

        public string DeviceId { get; }
        
        public string DeviceName { get; }

        public DeviceInformation DeviceInfo => _deviceInfo;

        public CastingDevice? CastingDevice
        {
            get => _castingDevice;
            set
            {
                if (_castingDevice != value)
                {
                    _castingDevice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsConnectable));
                }
            }
        }

        public CastingConnectionState ConnectionState
        {
            get => _connectionState;
            set
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    UpdateConnectionStatus();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(IsConnectable));
                    OnPropertyChanged(nameof(CanRetry));
                    OnPropertyChanged(nameof(CanDisconnect));
                }
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set
            {
                if (_lastSeen != value)
                {
                    _lastSeen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LastSeenDisplay));
                }
            }
        }

        public string LastSeenDisplay => 
            DateTime.Now.Subtract(_lastSeen).TotalMinutes < 1 
                ? "Just now" 
                : $"{DateTime.Now.Subtract(_lastSeen).TotalMinutes:F0} min ago";

        public bool IsRetrying
        {
            get => _isRetrying;
            set
            {
                if (_isRetrying != value)
                {
                    _isRetrying = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(CanDisconnect));
                }
            }
        }

        public int RetryAttempts
        {
            get => _retryAttempts;
            set
            {
                if (_retryAttempts != value)
                {
                    _retryAttempts = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public double SignalStrength
        {
            get => _signalStrength;
            set
            {
                if (Math.Abs(_signalStrength - value) > 0.01)
                {
                    _signalStrength = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SignalStrengthDisplay));
                }
            }
        }

        public string SignalStrengthDisplay => $"{_signalStrength:P0}";

        // Computed Properties
        public bool IsConnected => _connectionState == CastingConnectionState.Connected;
        
        public bool IsVideoCapable
        {
            get => _isVideoCapable;
            set
            {
                if (_isVideoCapable != value)
                {
                    _isVideoCapable = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsConnectable));
                }
            }
        }

        public bool IsConnectable => _castingDevice != null && 
                                   _connectionState != CastingConnectionState.Connecting &&
                                   !_isRetrying &&
                                   _isVideoCapable;

        public bool CanRetry => _connectionState == CastingConnectionState.Disconnected && 
                               !_isRetrying && 
                               !string.IsNullOrEmpty(_errorMessage);

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public bool CanDisconnect => IsConnected || _isRetrying;

        public string StatusText
        {
            get
            {
                if (_isRetrying)
                    return $"Retrying... (Attempt {_retryAttempts})";
                
                if (HasError)
                    return $"Error: {_errorMessage}";
                
                return _connectionStatus;
            }
        }

        #endregion

        #region Device Capabilities

        public string DeviceType { get; private set; } = "Unknown";
        
        public bool SupportsMiracast { get; private set; }
        
        public bool SupportsDLNA { get; private set; }
        
        public bool SupportsWiFiDirect { get; private set; }
        
        public string[] SupportedProtocols { get; private set; } = Array.Empty<string>();

        #endregion

        #region Public Methods

        public void UpdateDeviceInfo(DeviceInformation deviceInfo)
        {
            if (deviceInfo?.Id == DeviceId)
            {
                _deviceInfo = deviceInfo;
                LastSeen = DateTime.Now;
                ParseDeviceCapabilities();
                OnPropertyChanged(nameof(DeviceInfo));
            }
        }

        public void ClearError()
        {
            ErrorMessage = string.Empty;
            RetryAttempts = 0;
        }

        public void IncrementRetryAttempts()
        {
            RetryAttempts++;
        }

        #endregion

        #region Private Methods

        private void UpdateConnectionStatus()
        {
            ConnectionStatus = _connectionState switch
            {
                CastingConnectionState.Connected => "Connected",
                CastingConnectionState.Connecting => "Connecting...",
                CastingConnectionState.Disconnected => "Disconnected",
                _ => "Unknown"
            };
        }

        private void ParseDeviceCapabilities()
        {
            if (_deviceInfo?.Properties == null) return;

            try
            {
                // Extract device type and capabilities from device properties
                if (_deviceInfo.Properties.TryGetValue("System.Devices.DeviceInstanceId", out var instanceId))
                {
                    var instanceIdStr = instanceId?.ToString() ?? string.Empty;
                    
                    // Determine device type based on instance ID patterns
                    if (instanceIdStr.Contains("MIRACAST", StringComparison.OrdinalIgnoreCase))
                    {
                        DeviceType = "Miracast Display";
                        SupportsMiracast = true;
                    }
                    else if (instanceIdStr.Contains("DLNA", StringComparison.OrdinalIgnoreCase))
                    {
                        DeviceType = "DLNA Device";
                        SupportsDLNA = true;
                    }
                    else if (instanceIdStr.Contains("WIFI", StringComparison.OrdinalIgnoreCase))
                    {
                        DeviceType = "WiFi Direct Device";
                        SupportsWiFiDirect = true;
                    }
                }

                // Try to get signal strength if available
                if (_deviceInfo.Properties.TryGetValue("System.Devices.WiFi.SignalBars", out var signalBars))
                {
                    if (int.TryParse(signalBars?.ToString(), out var bars))
                    {
                        SignalStrength = bars / 5.0; // Convert to 0-1 range
                    }
                }

                // Build supported protocols list
                var protocols = new List<string>();
                if (SupportsMiracast) protocols.Add("Miracast");
                if (SupportsDLNA) protocols.Add("DLNA");
                if (SupportsWiFiDirect) protocols.Add("WiFi Direct");
                SupportedProtocols = protocols.ToArray();

                OnPropertyChanged(nameof(DeviceType));
                OnPropertyChanged(nameof(SupportsMiracast));
                OnPropertyChanged(nameof(SupportsDLNA));
                OnPropertyChanged(nameof(SupportsWiFiDirect));
                OnPropertyChanged(nameof(SupportedProtocols));
            }
            catch (Exception ex)
            {
                // Log parsing error but don't throw
                System.Diagnostics.Debug.WriteLine($"Error parsing device capabilities: {ex.Message}");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{DeviceName} ({DeviceType}) - {ConnectionStatus}";
        }

        public override bool Equals(object? obj)
        {
            return obj is CastingDeviceModel other && DeviceId == other.DeviceId;
        }

        public override int GetHashCode()
        {
            return DeviceId?.GetHashCode() ?? 0;
        }

        #endregion
    }
}
