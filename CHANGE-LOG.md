# Full Change Log for Raygun4Net.* packages

### v10.1.1
- Cleanup of the constructors for Net Core RaygunClient
  - See: https://github.com/MindscapeHQ/raygun4net/pull/525
- Reduce overhead of RaygunBreadcrumb
  - See: https://github.com/MindscapeHQ/raygun4net/pull/524 

### v10.1.0
- Add support for capturing Environment Variables in NetCore
  - New setting `EnvironmentVariables` which takes a list of search terms
  - Supports Exact, StartsWith, EndsWith, and Contains matching
  - See: https://github.com/MindscapeHQ/raygun4net/pull/523

### v10.0.0
- RaygunClient for NET Core can now be treated as a Singleton
- Changed the Middleware for AspNetCore
  - See: https://github.com/MindscapeHQ/raygun4net/pull/518 
  - Updated the registration of `.AddRaygun()`
  - Updated the usage of `.UseRaygun()`
  - Removed `RaygunMiddlewareSettings` and `RaygunClientProvider` in favour of `IRaygunUserProvider`
  - Removed custom code for maintaining request body in the middleware and used `Request.EnableBuffering()` instead
  - Deprecated multiple settings in RaygunClient in favour of using `RaygunSettings`
  - Deprecated `UserInfo` property in favour of `IRaygunUserProvider`
  - Deprecated `RaygunMiddlewareOptions` in favour of `RaygunSettings`
  - Introduced new `.AddRaygunUserProvider()` to register a default implementation
  - Introduced new `.AddRaygunUserProvider<T>()` to register a custom implementation
- Fixed bug with filters where uppercase properties were compared against lowercase filters
- Fixed null reference when accessing CustomData and Tags in RaygunMessage using OnSendingMessage
- Adds Breadcrumbs to NetCore and AspNetCore
  - See: https://github.com/MindscapeHQ/raygun4net/pull/516
  - Breadcrumbs are by default local to the asynchronous context using `AsyncLocalBreadcrumbStorage`
  - Additional `InMemmoryBreadcrumbStorage` implemented for global context breadcrumbs

### v9.0.4
- Fixed `RaygunClient` in .NET Framework to correctly gather HTTP data and remove [ThreadStatic] attribute
- Fixed `RaygunWebApiClient` to correctly get Form data (it was looking at QueryString instead of Form)

### v9.0.3
- Fixed `RaygunWebApiClient` constructor to create `ThrottledBackgroundMessageProcessor` when the empty constructor is used
  - See: https://github.com/MindscapeHQ/raygun4net/pull/519 

### v9.0.2
- Remove the usage of `ThreadLocal` in the RaygunClient (for .NET Framework)
  - This change removes the SetCurrentHttpRequest method and uses HttpContext.Current directly
  - This does not affect the AspNetCore version as it uses IHttpContextAccessor

### v9.0.1
- Fixed issue for lock upgrade/recursion exception raised
  - See: https://github.com/MindscapeHQ/raygun4net/issues/513

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
