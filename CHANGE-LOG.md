# Full Change Log for Raygun4Net.* packages

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
