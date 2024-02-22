# Full Change Log for Raygun4Net.* packages

### v9.0.2
- Remove the usage of `ThreadLocal` in the RaygunClient (for .NET Framework)
  - This does not affect the NetCore version 

### v9.0.1
- Fixed issue for lock upgrade/recursion exception raised
  - https://github.com/MindscapeHQ/raygun4net/issues/513

### v9.0.0
- SendInBackground will now queue the message to be sent
  - Fixes issue in .NET Core for Azure where sending many messages Async can cause SNAT exhaustion
  - Fixes issue in .NET Framework for Azure where sending many messages causes many Threads to be used
- Moved .NET Framework projects to new SDK style
  - Packages are now built using the new SDK style and nuspecs removed
- Drop support for non-supported Frameworks (.NET Framework 4.6.2 onwards support only)
  - This drops support for Client Profile
- Include HttpClient on RaygunClient in AspNetCore project as its a parameter defined in RaygunClientBase

### v8.2.1
- Fixed memory leak when multiple RaygunClient instances are created

### v8.2.0
- Added public ctor to allow RaygunClient to accept a custom HttpClient
- Changed the default timeout of HttpClient from 100 seconds to 30 seconds

### v8.1.0
- Fully support catching unhandled exceptions in NetCore
- Use reflection to discover and attach to exception handlers in Android, iOS, MacCatalyst targets
- Add `UnhandledExceptionBridge.RaiseUnhandledException`. This can be called from unsupported platforms to raise and log exceptions via Raygun

### v8.0.1
- Removed dependency on Microsoft.Win32.Registry for `Mindscape.Raygun4Net.NetCore.Common` package

### v8.0.0
- This release fixes the strong naming and ensures the Assembly version is fixed to the Major version

### v7.1.1 (Mindscape.Raygun4Net.WebApi)
- Fixed issue with missing dependency on `Mindscape.Raygun4Net.Common`

### v7.1.0
- Fixed Environment Information Memory
- Added cache for Environment Information to reduce overhead
- Added support for Environment Information on Windows/Linux/MacOS
- Changed CPU / OS information to be more accurate / understandable

### v7.0.1
- Fixed nuspec packaging wrong files

### v7.0.0
- Correctly populate missing Environment Information
- Deprecated .Signed suffix packages
- Strong Name all packages

### v6.7.2
- Fixed issue with signed packages for NetCore nugets

### v6.7.0
- Changed `SendInBackground` to no longer be blocking
