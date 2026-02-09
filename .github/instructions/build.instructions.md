# DSInternals Build Instructions

## Overview
This document provides comprehensive instructions for building the DSInternals solution for specific platforms and configurations using solution filter (.slnf) files.

## Prerequisites
- Visual Studio 2022 with C++ build tools
- .NET Framework 4.7.2 SDK
- Windows 10/11 development environment
- PowerShell 5.1 or later

## Build Process

### Step 1: Environment Setup
Open a **Visual Studio Developer PowerShell** session and navigate to the Src directory:
```powershell
# Initialize VS Developer environment
Import-Module "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
Enter-VsDevShell -VsInstallPath "C:\Program Files\Microsoft Visual Studio\2022\Professional" -SkipAutomaticLocation
cd "D:\development\dsinternals-tms\Src"
```

### Step 2: Clean Build Environment
Remove all obj directories to avoid NuGet MSBuild target conflicts:
```powershell
Get-ChildItem -Path . -Filter "obj" -Directory -Recurse | Remove-Item -Recurse -Force
```

### Step 3: Restore NuGet Packages
Restore packages using the local nuget.exe:
```powershell
.\nuget.exe restore "DSInternals.sln"
```

### Step 4: Build Unmanaged Components First
Build the C++ interop and its dependencies using the unmanaged solution filter:

**For Release x86 (recommended):**
```powershell
msbuild "DSInternals-Unmanaged.slnf" -p:Configuration=Release -p:Platform=x86 -v:m
```

**For Release x64:**
```powershell
msbuild "DSInternals-Unmanaged.slnf" -p:Configuration=Release -p:Platform=x64 -v:m
```

### Step 5: Build Managed Components
Build the pure managed C# projects using the managed solution filter:

**For Release x86:**
```powershell
msbuild "DSInternals-Managed.slnf" -p:Configuration=Release -p:Platform=x86 -v:m
```

**For Release x64:**
```powershell
msbuild "DSInternals-Managed.slnf" -p:Configuration=Release -p:Platform=x64 -v:m
```

### Step 6: Complete Solution Build
Ensure all projects are built by running the full solution:
```powershell
msbuild "DSInternals.sln" -p:Configuration=Release -p:Platform=x86 -v:m
```

### Step 7: Centralize Output Files
Copy architecture-specific files to the main output directory:
```powershell
# For x64 builds
Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\amd64\DSInternals.Replication.Interop.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force
Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\amd64\msvcp140.dll", "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\amd64\vcruntime140*.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force

# For x86 builds  
Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\x86\DSInternals.Replication.Interop.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force
Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\x86\msvcp140.dll", "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\x86\vcruntime140*.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force
```

## Solution Filter Files

### DSInternals-Unmanaged.slnf
Contains C++ interop and its direct dependencies:
- DSInternals.Replication.Interop (C++)
- DSInternals.Replication
- DSInternals.PowerShell
- DSInternals.Common
- DSInternals.Replication.Model
- Other required dependencies

### DSInternals-Managed.slnf  
Contains pure managed C# projects:
- DSInternals.Common
- DSInternals.DataStore
- DSInternals.SAM
- DSInternals.Replication.Model
- Microsoft.Database.Isam
- Microsoft.Isam.Esent.Interop
- All test projects

## Output Directory
All compiled files will be available in:
**`D:\development\dsinternals-tms\Build\bin\Release\DSInternals\`**

This includes:
- All .NET assemblies (DSInternals.*.dll)
- C++ interop library (DSInternals.Replication.Interop.dll)
- Dependencies (Newtonsoft.Json, System.Memory, etc.)
- Visual C++ runtime libraries
- PowerShell module files (.psm1, .psd1, .ps1xml)
- Documentation and licensing files

## Build Order Importance
1. **Unmanaged first**: C++ projects must be built before managed projects that depend on them
2. **Clean obj directories**: Prevents NuGet MSBuild target conflicts with .NET Framework 4.7.2
3. **Use x86 platform**: More projects are configured for x86 than x64 in the solution
4. **Centralize output**: Architecture-specific DLLs need to be copied to main directory

## Common Issues & Solutions

### NuGet Framework Errors
If you encounter ".NETFramework,Version=v4.7.2 framework" errors:
- Ensure obj directories are cleaned before building
- Use the exact command sequence above
- Make sure Visual Studio Developer PowerShell is initialized

### Missing C++ Build Tools
If C++ compilation fails:
- Install "MSVC v143 - VS 2022 C++ x64/x86 build tools"
- Install "Windows 10/11 SDK"
- Ensure Visual Studio Developer environment is properly initialized

### Platform Configuration Issues  
If projects don't build for x64:
- Use x86 platform instead (more projects are configured for it)
- Check solution configuration in DSInternals.sln for available platforms

## Quick Build Commands

### Complete Release x86 Build:
```powershell
# Setup environment
Import-Module "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"; Enter-VsDevShell -VsInstallPath "C:\Program Files\Microsoft Visual Studio\2022\Professional" -SkipAutomaticLocation; cd "D:\development\dsinternals-tms\Src"

# Clean, restore, build
Get-ChildItem -Path . -Filter "obj" -Directory -Recurse | Remove-Item -Recurse -Force; .\nuget.exe restore "DSInternals.sln"; msbuild "DSInternals-Unmanaged.slnf" -p:Configuration=Release -p:Platform=x86 -v:m; msbuild "DSInternals.sln" -p:Configuration=Release -p:Platform=x86 -v:m

# Centralize output
Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\x86\DSInternals.Replication.Interop.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force
```

### Complete Release x64 Build:
```powershell
# Setup environment  
Import-Module "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"; Enter-VsDevShell -VsInstallPath "C:\Program Files\Microsoft Visual Studio\2022\Professional" -SkipAutomaticLocation; cd "D:\development\dsinternals-tms\Src"

# Clean, restore, build
Get-ChildItem -Path . -Filter "obj" -Directory -Recurse | Remove-Item -Recurse -Force; .\nuget.exe restore "DSInternals.sln"; msbuild "DSInternals-Unmanaged.slnf" -p:Configuration=Release -p:Platform=x64 -v:m; msbuild "DSInternals.sln" -p:Configuration=Release -p:Platform=x86 -v:m

# Centralize output
Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\amd64\DSInternals.Replication.Interop.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force; Copy-Item "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\amd64\msvcp140.dll", "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\amd64\vcruntime140*.dll" "D:\development\dsinternals-tms\Build\bin\Release\DSInternals\" -Force
```

---

**Note**: These instructions were created based on successful builds performed on February 9, 2026, using Visual Studio 2022 and .NET Framework 4.7.2.