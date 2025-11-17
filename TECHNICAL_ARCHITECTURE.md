# Technical Architecture Documentation

## 🏛️ System Architecture Overview

The UWP Casting Manager follows a layered MVVM architecture designed for maintainability, testability, and scalability while addressing the specific challenges of Windows casting APIs.

```
┌─────────────────────────────────────────────────────┐
│                 Presentation Layer                   │
├─────────────────────────────────────────────────────┤
│  MainPage.xaml  │  ValueConverters  │  Resources    │
│  Commands       │  Styles          │  Templates    │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│                 ViewModel Layer                     │
├─────────────────────────────────────────────────────┤
│  MainViewModel  │  RelayCommand    │  INotifyPC     │
│  Data Binding   │  State Mgmt      │  UI Logic     │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│                 Service Layer                       │
├─────────────────────────────────────────────────────┤
│  CastingService │  ConnectionMgmt   │  DeviceMonitor │
│  RetryLogic     │  EventHandling    │  TimerMgmt    │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│                 Model Layer                         │
├─────────────────────────────────────────────────────┤
│  CastingDeviceModel │  ConnectionState │  Capabilities │
│  DeviceProperties   │  ErrorTracking   │  StatusInfo   │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│              Windows Runtime APIs                   │
├─────────────────────────────────────────────────────┤
│  DeviceWatcher  │  CastingDevice    │  CastingSource │
│  DeviceInfo     │  ConnectionState  │  EventHandlers │
└─────────────────────────────────────────────────────┘
```

## 🔧 Core Components Deep Dive

### 1. CastingService Architecture

The `CastingService` is the heart of the application, implementing a robust device management system:

```csharp
┌─────────────────────────────────────────────────────┐
│                 CastingService                      │
├─────────────────────────────────────────────────────┤
│ Responsibilities:                                   │
│ • Device Discovery (DeviceWatcher)                 │
│ • Connection Management (CastingDevice)            │
│ • Background Monitoring (Timer-based)              │
│ • Error Handling & Recovery                        │
│ • Event Coordination                               │
└─────────────────────────────────────────────────────┘
```

#### Key Design Patterns:

**Observer Pattern**: Event-driven updates
```csharp
public event EventHandler<CastingDeviceModel> DeviceConnectionStateChanged;
public event EventHandler<string> ErrorOccurred;
```

**Singleton Service**: Single instance manages all casting operations
```csharp
private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
```

**Timer-Based Monitoring**: Background health checks
```csharp
_connectionMonitorTimer = new Timer(MonitorConnections, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
```

### 2. Device Discovery Strategy

The application implements a multi-layered device discovery approach:

```
Device Discovery Flow:
┌─────────────┐    ┌──────────────┐    ┌─────────────┐
│DeviceWatcher│───▶│ DeviceInfo   │───▶│CastingDevice│
│   Started   │    │  Received    │    │  Created    │
└─────────────┘    └──────────────┘    └─────────────┘
                            │
                            ▼
                    ┌──────────────┐
                    │DeviceModel   │
                    │  Added to    │
                    │ Collection   │
                    └──────────────┘
```

**Device Selector Implementation:**
```csharp
var deviceSelector = CastingDevice.GetDeviceSelector(CastingPlaybackTypes.Video);
_deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector);
```

**Capability Detection Logic:**
```csharp
private void ParseDeviceCapabilities()
{
    // Extract device type from instance ID patterns
    if (instanceIdStr.Contains("MIRACAST", StringComparison.OrdinalIgnoreCase))
        DeviceType = "Miracast Display";
    else if (instanceIdStr.Contains("DLNA", StringComparison.OrdinalIgnoreCase))
        DeviceType = "DLNA Device";
    // ... additional protocol detection
}
```

### 3. Connection Management Architecture

The connection management system addresses the complex requirements of Windows casting APIs:

```
Connection Flow:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│UI Element   │───▶│CastingSource│───▶│  Connection │
│(Hidden Grid)│    │  Created    │    │ Established │
└─────────────┘    └─────────────┘    └─────────────┘
                            │
                            ▼
                    ┌─────────────┐
                    │Event Handler│
                    │   Setup     │
                    └─────────────┘
```

**CastingSource Challenge Solution:**
```csharp
// Hidden UI element serves as casting source
<Grid x:Name="CastingSourceGrid" Width="1" Height="1" Opacity="0">
    <Rectangle Fill="Black"/>
</Grid>

// Code-behind integration
_viewModel.SetCastingSource(CastingSourceGrid);
```

**Connection State Management:**
```csharp
public enum ConnectionState
{
    Disconnected,    // No active connection
    Connecting,      // Connection in progress
    Connected,       // Active connection established
    Retrying        // Attempting reconnection
}
```

### 4. Error Handling & Recovery System

The application implements a comprehensive error handling strategy:

```
Error Handling Hierarchy:
┌─────────────────────────────────────────────────────┐
│                Service Level                        │
│ • Global exception handling                         │
│ • Service state recovery                           │
│ • Event propagation                                │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│                Device Level                         │
│ • Connection-specific errors                       │
│ • Retry attempt tracking                           │
│ • Device state management                          │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│                UI Level                             │
│ • User-friendly error messages                     │
│ • Status indicators                                │
│ • Recovery action buttons                          │
└─────────────────────────────────────────────────────┘
```

**Exponential Backoff Implementation:**
```csharp
private async Task<bool> RetryConnectionAsync(CastingDeviceModel deviceModel, FrameworkElement sourceElement)
{
    var delaySeconds = Math.Min(Math.Pow(2, deviceModel.RetryAttempts - 1), 30);
    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
    // ... retry logic
}
```

## 📊 Data Flow Architecture

### 1. MVVM Data Binding Flow

```
Data Binding Flow:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Model     │◄──▶│  ViewModel  │◄──▶│    View     │
│(DeviceModel)│    │(MainVM)     │    │(MainPage)   │
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│PropertyChanged   │ICommand     │    │Value        │
│Notifications │    │Execution    │    │Converters   │
└─────────────┘    └─────────────┘    └─────────────┘
```

### 2. Event Propagation System

```csharp
Event Flow:
WinRT API Event ──▶ CastingService ──▶ MainViewModel ──▶ UI Update
     │                    │                 │              │
     ▼                    ▼                 ▼              ▼
DeviceWatcher.Added  ──▶ OnDeviceAdded ──▶ PropertyChanged ──▶ UI Refresh
ConnectionStateChanged ──▶ OnStateChanged ──▶ DeviceUpdated ──▶ Status Update
ErrorOccurred ────────▶ HandleError ────▶ ErrorMessage ──▶ Error Display
```

### 3. Asynchronous Operation Management

The application uses a structured approach to async operations:

```csharp
Async Pattern:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│UI Thread    │───▶│Task.Run     │───▶│Background   │
│Command      │    │Execution    │    │Processing   │
└─────────────┘    └─────────────┘    └─────────────┘
       ▲                                       │
       │            ┌─────────────┐            │
       └────────────│Dispatcher   │◄───────────┘
                    │UI Update    │
                    └─────────────┘
```

## 🔒 Thread Safety & Concurrency

### 1. Thread-Safe Operations

**SemaphoreSlim for Critical Sections:**
```csharp
private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

public async Task<bool> ConnectToDeviceAsync(...)
{
    await _operationSemaphore.WaitAsync();
    try
    {
        // Critical section - device connection
    }
    finally
    {
        _operationSemaphore.Release();
    }
}
```

**Dispatcher for UI Updates:**
```csharp
private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
{
    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
    {
        Devices.Add(new CastingDeviceModel(args));
    });
}
```

### 2. Collection Thread Safety

**ObservableCollection Updates:**
```csharp
// Always update collections on UI thread
await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
{
    _deviceCache[deviceInfo.Id] = deviceModel;
    Devices.Add(deviceModel);
});
```

## 🎯 Performance Optimization Strategies

### 1. Memory Management

**Weak Event Patterns:**
```csharp
// Prevent memory leaks in event handlers
public void Dispose()
{
    if (_deviceWatcher != null)
    {
        _deviceWatcher.Added -= OnDeviceAdded;
        _deviceWatcher.Updated -= OnDeviceUpdated;
        _deviceWatcher.Removed -= OnDeviceRemoved;
    }
}
```

**Device Cache Management:**
```csharp
// Remove stale devices to prevent memory bloat
private void RefreshDeviceList(object state)
{
    var cutoffTime = DateTime.Now.AddMinutes(-5);
    var devicesToRemove = _deviceCache
        .Where(kvp => kvp.Value.LastSeen < cutoffTime && !kvp.Value.IsConnected)
        .Select(kvp => kvp.Key)
        .ToList();
}
```

### 2. UI Performance

**Virtualization for Large Lists:**
```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <ItemsControl ItemsSource="{Binding Devices}"
                  ItemTemplate="{StaticResource DeviceListItemTemplate}"/>
</ScrollViewer>
```

**Efficient Value Converters:**
```csharp
// Cache converter instances
public class ConnectionStateToColorConverter : IValueConverter
{
    private static readonly Dictionary<CastingConnectionState, SolidColorBrush> _brushCache 
        = new Dictionary<CastingConnectionState, SolidColorBrush>();
}
```

## 🧪 Testing Architecture

### 1. Unit Testing Strategy

```csharp
Test Structure:
┌─────────────────────────────────────────────────────┐
│                 Unit Tests                          │
├─────────────────────────────────────────────────────┤
│ • Service Logic Testing                             │
│ • ViewModel Command Testing                         │
│ • Model State Testing                              │
│ • Converter Logic Testing                          │
└─────────────────────────────────────────────────────┘
```

**Service Testing Example:**
```csharp
[TestMethod]
public async Task InitializeAsync_ShouldReturnTrue_WhenSuccessful()
{
    // Arrange
    var castingService = new CastingService();
    
    // Act
    var result = await castingService.InitializeAsync();
    
    // Assert
    Assert.IsTrue(result);
    Assert.IsTrue(castingService.IsInitialized);
}
```

### 2. Integration Testing

**Device Discovery Testing:**
```csharp
[TestMethod]
public async Task DeviceDiscovery_ShouldFindDevices_WhenAvailable()
{
    // Test with mock devices or actual hardware
    var service = new CastingService();
    await service.InitializeAsync();
    
    // Wait for discovery
    await Task.Delay(5000);
    
    Assert.IsTrue(service.TotalDevicesFound >= 0);
}
```

## 🔧 Configuration & Extensibility

### 1. Configuration System

**App Settings Structure:**
```csharp
public class CastingConfiguration
{
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 5;
    public int DeviceRefreshIntervalMinutes { get; set; } = 2;
    public int ConnectionMonitorIntervalSeconds { get; set; } = 10;
    public bool EnableBackgroundMonitoring { get; set; } = true;
}
```

### 2. Plugin Architecture

**Extensible Service Interface:**
```csharp
public interface ICastingService
{
    Task<bool> InitializeAsync();
    Task<bool> ConnectToDeviceAsync(CastingDeviceModel device, FrameworkElement source);
    Task<bool> DisconnectFromDeviceAsync(CastingDeviceModel device);
    ObservableCollection<CastingDeviceModel> Devices { get; }
}
```

## 📈 Scalability Considerations

### 1. Device Limit Handling

**Pagination for Large Device Lists:**
```csharp
public class PaginatedDeviceCollection : ObservableCollection<CastingDeviceModel>
{
    private const int PageSize = 50;
    private int _currentPage = 0;
    
    public void LoadNextPage()
    {
        // Load next batch of devices
    }
}
```

### 2. Background Service Support

**Service Extension for Background Operations:**
```csharp
// Future enhancement: Background app service
public class BackgroundCastingService : BackgroundTaskBase
{
    public void Run(IBackgroundTaskInstance taskInstance)
    {
        // Monitor connections in background
    }
}
```

## 🔍 Monitoring & Diagnostics

### 1. Logging Architecture

**Structured Logging System:**
```csharp
public static class CastingLogger
{
    public static void LogDeviceDiscovered(string deviceName, string deviceType)
    {
        Debug.WriteLine($"[DISCOVERY] Device found: {deviceName} ({deviceType})");
    }
    
    public static void LogConnectionAttempt(string deviceName, int attempt)
    {
        Debug.WriteLine($"[CONNECTION] Attempting to connect to {deviceName} (Attempt {attempt})");
    }
}
```

### 2. Performance Metrics

**Key Performance Indicators:**
- Device discovery time
- Connection establishment time
- Memory usage patterns
- Background task efficiency
- Error rate tracking

This technical architecture provides a solid foundation for the UWP Casting Manager application, ensuring maintainability, performance, and extensibility while addressing the specific challenges of Windows casting APIs.