Raygun4Net is a small library that can listen to all unhandled exceptions in your Android and iOS applications and send the details to your Raygun.io account.

```csharp
using Raygun4Net;
...

// Place this code somewhere in the main entry point of your application.
RaygunClient.Attach("YOUR_APP_API_KEY");
```
