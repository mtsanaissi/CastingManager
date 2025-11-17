# Build Instructions for UWP Casting Manager

## 📋 Prerequisites Checklist

### Required Software
- [ ] **Windows 11** (Build 22000 or later)
- [ ] **Visual Studio 2022** (Version 17.8 or later)
- [ ] **Windows 11 SDK** (Build 22621 or later)
- [ ] **.NET 8 SDK** (8.0.100 or later)
- [ ] **Windows App SDK** (1.4.231008000 or later)

### Visual Studio Workloads
Ensure these workloads are installed in Visual Studio:
- [ ] **Universal Windows Platform development**
- [ ] **.NET desktop development**
- [ ] **Windows application packaging**

### System Configuration
- [ ] **Developer Mode** enabled in Windows Settings
- [ ] **Hyper-V** enabled (for emulator support)
- [ ] **Windows Subsystem for Linux** (optional, for advanced development)

## 🛠️ Step-by-Step Build Process

### 1. Project Setup

```bash
# Create project directory
mkdir CastingManager
cd CastingManager

# Initialize git repository (optional)
git init
```

### 2. File Structure Creation

Create the following directory structure:
```
CastingManager/
├── Assets/                 # App icons and images
├── Commands/              # Command implementations
├── Converters/            # XAML value converters
├── Models/                # Data models
├── Services/              # Business logic services
├── ViewModels/            # MVVM view models
├── Views/                 # Additional views (if needed)
├── App.xaml              # Application resources
├── App.xaml.cs           # Application entry point
├── MainPage.xaml         # Main UI
├── MainPage.xaml.cs      # Main page code-behind
├── Package.appxmanifest  # UWP manifest
└── CastingManager.csproj # Project file
```

### 3. Asset Preparation

Create the following image assets in the `Assets/` folder:

```
Assets/
├── app.ico                           # 256x256 app icon
├── LockScreenLogo.scale-200.png      # 48x48
├── SplashScreen.scale-200.png        # 1240x600
├── Square44x44Logo.scale-200.png     # 88x88
├── Square44x44Logo.targetsize-24_altform-unplated.png # 24x24
├── Square150x150Logo.scale-200.png   # 300x300
├── StoreLogo.png                     # 50x50
└── Wide310x150Logo.scale-200.png     # 620x300
```

**Asset Creation Tips:**
- Use transparent backgrounds for logos
- Follow Windows 11 design guidelines
- Consider high contrast and dark theme variants
- Test assets at different scales (100%, 125%, 150%, 200%)

### 4. Building the Application

#### Option A: Visual Studio GUI Method

1. **Open Visual Studio 2022**
2. **Create New Project** → **Blank App (Universal Windows)**
3. **Replace generated files** with the provided code files
4. **Set target version** to Windows 11 (10.0.22000.0)
5. **Build** → **Build Solution** (Ctrl+Shift+B)

#### Option B: Command Line Method

```bash
# Restore NuGet packages
dotnet restore CastingManager.csproj

# Build for Debug (x64)
dotnet build CastingManager.csproj -c Debug -r win10-x64

# Build for Release (all platforms)
dotnet build CastingManager.csproj -c Release

# Clean build artifacts
dotnet clean CastingManager.csproj
```

### 5. Platform-Specific Builds

```bash
# Build for x86
dotnet build -c Release -r win10-x86

# Build for x64
dotnet build -c Release -r win10-x64

# Build for ARM64
dotnet build -c Release -r win10-arm64

# Build all platforms
dotnet build -c Release --runtime win10-x86 --runtime win10-x64 --runtime win10-arm64
```

## 📦 Packaging for Distribution

### 1. Create App Package (Visual Studio)

1. **Right-click project** → **Publish** → **Create App Packages**
2. **Choose distribution method:**
   - Microsoft Store
   - Sideloading
   - Continuous Integration
3. **Select architecture:** x86, x64, ARM64
4. **Configure package settings:**
   - Version number
   - Bundle configuration
   - Include public symbol files

### 2. Command Line Packaging

```bash
# Create MSIX package
msbuild CastingManager.csproj ^
  /p:Configuration=Release ^
  /p:Platform=x64 ^
  /p:AppxBundle=Always ^
  /p:AppxBundlePlatforms="x86|x64|ARM64" ^
  /p:UapAppxPackageBuildMode=SideloadOnly

# Create store-ready package
msbuild CastingManager.csproj ^
  /p:Configuration=Release ^
  /p:Platform=x64 ^
  /p:AppxBundle=Always ^
  /p:AppxBundlePlatforms="x86|x64|ARM64" ^
  /p:UapAppxPackageBuildMode=StoreUpload
```

### 3. Package Validation

```bash
# Install Windows App Certification Kit
# Then run validation
"C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe" ^
  reset ^
  -reportoutputpath "C:\CertificationReport.xml" ^
  -packagepath "CastingManager_1.0.0.0_Test\CastingManager_1.0.0.0_x64.msix"
```

## 🔧 Advanced Build Configuration

### 1. Conditional Compilation

Add to `.csproj` for different build configurations:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <DefineConstants>DEBUG;TRACE;DEVELOPMENT_BUILD</DefineConstants>
  <Optimize>false</Optimize>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <DefineConstants>TRACE;PRODUCTION_BUILD</DefineConstants>
  <Optimize>true</Optimize>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

### 2. Code Analysis Integration

```xml
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
  <CodeAnalysisRuleSet>custom.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>
```

### 3. Automated Testing Integration

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  <PackageReference Include="MSTest.TestAdapter" Version="3.1.1" />
  <PackageReference Include="MSTest.TestFramework" Version="3.1.1" />
</ItemGroup>
```

## 🚀 Deployment Options

### 1. Development/Testing Deployment

```powershell
# Install for current user (PowerShell as Admin)
Add-AppxPackage -Path "CastingManager_1.0.0.0_x64.msix"

# Install with dependency packages
Add-AppxPackage -Path "CastingManager_1.0.0.0_x64.msix" -DependencyPath "Dependencies\x64\"

# Uninstall for testing
Remove-AppxPackage -Package "CastingManager_1.0.0.0_x64__8wekyb3d8bbwe"
```

### 2. Enterprise Deployment

```bash
# Create deployment script
echo 'Add-AppxPackage -Path "CastingManager.msix" -ForceApplicationShutdown' > Deploy.ps1

# Group Policy deployment
# 1. Copy MSIX to network share
# 2. Create GPO for software installation
# 3. Link to appropriate OU
```

### 3. Microsoft Store Deployment

1. **Partner Center Account** setup
2. **App submission** with package upload
3. **Store certification** compliance
4. **Gradual rollout** configuration

## 🐛 Build Troubleshooting

### Common Build Errors

**Error: "Package.appxmanifest not found"**
```bash
# Solution: Ensure manifest is in project root
# Check project file includes:
<AppxManifest Include="Package.appxmanifest" />
```

**Error: "Unable to load Windows Runtime type"**
```bash
# Solution: Verify Windows App SDK reference
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.4.231008000" />
```

**Error: "Casting APIs not available"**
```bash
# Solution: Check manifest capabilities
<DeviceCapability Name="wiFiControl" />
<Capability Name="privateNetworkClientServer" />
```

**Error: "Build failed with exit code 1"**
```bash
# Solution: Clean and rebuild
dotnet clean
dotnet restore
dotnet build --verbosity detailed
```

### Performance Optimization

**Reduce Package Size:**
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

**Enable Native AOT (when supported):**
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

## 🔍 Build Verification

### 1. Automated Testing

```bash
# Run unit tests
dotnet test CastingManager.Tests.csproj

# Generate code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### 2. Static Analysis

```bash
# Run code analysis
dotnet build --verbosity normal --configuration Release /p:RunAnalyzersDuringBuild=true

# Check security vulnerabilities
dotnet list package --vulnerable
```

### 3. Performance Profiling

```bash
# Install profiling tools
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-counters

# Profile application startup
dotnet-trace collect --process-id [PID] --providers Microsoft-Windows-DotNETRuntime
```

## 📊 Build Metrics

### Expected Build Times
- **Debug build:** 30-60 seconds
- **Release build:** 1-2 minutes
- **Package creation:** 2-5 minutes
- **Full clean build:** 3-8 minutes

### Package Size Expectations
- **Debug MSIX:** ~15-25 MB
- **Release MSIX:** ~8-15 MB
- **With dependencies:** ~50-80 MB
- **Store package:** ~10-20 MB

## 🔄 Continuous Integration

### GitHub Actions Example

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    - name: Restore dependencies
      run: dotnet restore CastingManager.UWP/CastingManager.UWP.csproj
      
    - name: Build
      run: "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" CastingManager.UWP/CastingManager.UWP.csproj /p:Platform=x64 /p:Configuration=Release
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
      
    - name: Create package
      run: |
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" CastingManager.UWP/CastingManager.UWP.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Always
```

This comprehensive build guide ensures you can successfully compile, test, and deploy the UWP Casting Manager application across all target platforms with proper validation and optimization.