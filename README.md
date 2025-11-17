# UWP Windows 11 Casting Management Application

A comprehensive UWP application for Windows 11 that provides enhanced control and monitoring of Miracast/DLNA/WiFi Direct connections to TVs and displays, serving as an advanced alternative to the built-in casting service.

## 🎯 Features

### Core Functionality
- **Device Discovery & Enumeration**: Automatically scan and list available casting targets
- **Connection Management**: Initiate, monitor, and terminate casting sessions with detailed status
- **Connection Recovery**: Automatic retry with exponential backoff for failed connections
- **Enhanced Information Display**: Real-time connection status, quality metrics, and device capabilities
- **Manual Retry Controls**: User-triggered reconnection attempts with visual feedback

### Technical Highlights
- **Modern Windows 11 UI**: Follows Windows 11 design principles with Fluent Design
- **Real-time Monitoring**: Background monitoring of connection health and device availability
- **Robust Error Handling**: Comprehensive exception handling for network and API issues
- **MVVM Architecture**: Clean separation of concerns with proper data binding
- **Asynchronous Operations**: Non-blocking UI with proper async/await patterns

## 🏗️ Architecture

### Project Structure
```
CastingManager/
├── Models/
│   └── CastingDeviceModel.cs          # Device data model with connection state
├── Services/
│   └── CastingService.cs              # Core casting service with device management
├── ViewModels/
│   └── MainViewModel.cs               # Main view model with command handling
├── Commands/
│   └── RelayCommand.cs                # Command implementation for MVVM
├── Converters/
│   └── ValueConverters.cs             # XAML value converters for UI binding
├── Views/
│   ├── MainPage.xaml                  # Main UI layout
│   └── MainPage.xaml.cs               # Code-behind for UI events
├── App.xaml                           # Application resources and theming
├── App.xaml.cs                        # Application entry point
├── Package.appxmanifest               # UWP app manifest with capabilities
└── CastingManager.csproj              # Project configuration
```

### Key Components

#### CastingService
- Device discovery using `DeviceWatcher` with casting device selector
- Connection management with proper `CastingSource` integration
- Background monitoring with configurable timers
- Automatic retry logic with exponential backoff
- Event-driven architecture for real-time updates

#### CastingDeviceModel
- Rich device information with capability detection
- Connection state tracking with status updates
- Error handling and retry attempt tracking
- Signal strength and last-seen monitoring
- Property change notifications for UI binding

#### MainViewModel
- Command pattern implementation for all user actions
- Proper service lifecycle management
- Status message handling and error display
- Device selection and operation coordination

## 🔧 Technical Requirements

### Development Environment
- **Visual Studio 2022** (17.8 or later)
- **Windows 11 SDK** (22H2 or later)
- **.NET 8** target framework
- **Windows App SDK 1.4** or later

### Target Platform
- **Windows 11** (Build 22000 or later)
- **UWP Application Model**
- Support for x86, x64, and ARM64 architectures

### Required Capabilities
```xml
<Capabilities>
    <Capability Name="internetClient" />
    <Capability Name="internetClientServer" />
    <Capability Name="privateNetworkClientServer" />
    <uap:Capability Name="allJoyn" />
    <DeviceCapability Name="wiFiControl" />
    <DeviceCapability Name="proximity" />
    <rescap:Capability Name="wiFiControl" />
</Capabilities>
```

## 🚀 Getting Started

### Prerequisites
1. Enable **Developer Mode** in Windows 11 Settings
2. Install **Visual Studio 2022** with UWP development workload
3. Ensure **Windows 11 SDK (22H2)** is installed

### Building the Application
```bash
# Clone or create the project
# Open CastingManager.sln in Visual Studio 2022

# Restore NuGet packages
dotnet restore

# Build for debug (x64)
dotnet build -c Debug -r win10-x64

# Build for release (all platforms)
dotnet build -c Release
```

### Deployment
```bash
# Create app package for sideloading
# In Visual Studio: Project > Publish > Create App Packages
# Or use command line:
msbuild CastingManager.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Always
```

## 💡 Usage Guide

### Initial Setup
1. Launch the application
2. Click **"Initialize"** to start the casting service
3. The app will automatically begin scanning for available devices

### Connecting to Devices
1. Select a device from the discovered list
2. Click **"Connect"** to initiate casting
3. Monitor connection status in real-time
4. Use **"Retry"** for failed connections or **"Disconnect"** to terminate

### Monitoring Features
- **Device Statistics**: View total devices found and active connections
- **Real-time Status**: Connection state updates with visual indicators
- **Error Handling**: Clear error messages with retry options
- **Background Monitoring**: Automatic detection of connection drops

## 🔍 Technical Deep Dive

### DevicePicker Integration Challenge
The application addresses the complex requirement where `CastingDevicePicker` needs a `CastingSource` created from a UI element:

```csharp
// Get the main view's ID
var currentView = ApplicationView.GetForCurrentView();
var mainViewId = currentView.Id;

// Start projection from the main view
await ProjectionManager.StartProjectingAsync(mainViewId, mainViewId);
```

### Connection Monitoring Strategy
- **Timer-based monitoring** every 10 seconds for connection health
- **Event-driven updates** for immediate state changes
- **Automatic cleanup** of stale device entries after 5 minutes
- **Connection recovery** with progressive retry delays

### Error Handling Approach
- **Service-level error handling** with event propagation
- **Device-level error tracking** with retry attempt counting
- **UI error display** with clear user messaging
- **Graceful degradation** when APIs are unavailable

## 🎨 UI/UX Design

### Windows 11 Design Principles
- **Rounded corners** and modern card layouts
- **Fluent Design System** colors and typography
- **Contextual information** with status indicators
- **Progressive disclosure** of device details
- **Accessible color contrast** and keyboard navigation

### Responsive Layout
- **Grid-based layouts** that adapt to window sizing
- **Collapsible sections** for different screen sizes
- **Touch-friendly** button sizing and spacing
- **High DPI support** with vector icons

## 🔒 Security Considerations

### Network Security
- Application uses standard Windows APIs for device discovery
- No custom network protocols or unsecured connections
- Respects Windows firewall and network policies

### Privacy
- No personal data collection or transmission
- Device information stays local to the application
- Standard Windows permission model for network access

## 🐛 Troubleshooting

### Common Issues

**"No devices found"**
- Ensure TV/display is on and connected to same network
- Check Windows firewall settings
- Verify network discovery is enabled in Windows

**"Connection failed"**
- Restart the target device
- Use the manual retry function
- Check for Windows updates on both devices

**"Service initialization failed"**
- Run Visual Studio as Administrator
- Check that all required capabilities are in manifest
- Verify Windows 11 SDK is properly installed

### Debug Logging
The application includes comprehensive debug logging accessible via Visual Studio output window:
```csharp
System.Diagnostics.Debug.WriteLine($"CastingService: {message}");
```

## 🛠️ Development Notes

### Known Limitations
- **Casting source requirement**: UI element needed for DevicePicker integration
- **Background limitations**: UWP background execution constraints
- **API restrictions**: Some WinRT casting APIs have undocumented limitations

### Future Enhancements
- **Background app support** for system tray operation
- **Custom UI casting** with MediaElement integration
- **Device grouping** and management features
- **Connection quality metrics** and bandwidth monitoring

## 📋 Requirements Validation

### ✅ Completed Features
- [x] Device discovery and enumeration
- [x] Connection management with status monitoring
- [x] Automatic retry with exponential backoff
- [x] Enhanced device information display
- [x] Manual retry controls
- [x] Real-time connection monitoring
- [x] Error handling and recovery
- [x] Modern Windows 11 UI
- [x] MVVM architecture
- [x] Comprehensive logging

### 🎯 Technical Challenges Addressed
- [x] DevicePicker CastingSource integration
- [x] Background connection monitoring
- [x] WinRT API interop and error handling
- [x] .NET 8 UWP compatibility
- [x] Device capability detection
- [x] Connection state management

## 📄 License

This project is provided as a development template and reference implementation. Modify and use according to your requirements.

---

**Note**: This application requires Windows 11 and appropriate network permissions. Some features may require administrative privileges or specific hardware capabilities.