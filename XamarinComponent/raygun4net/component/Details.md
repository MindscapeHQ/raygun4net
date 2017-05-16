Raygun4Net is a small library that can listen to all unhandled exceptions in your Android and iOS applications and send the details to your [Raygun.com](https://raygun.com/?utm_source=xamarin-components) account. This allows you to see issues in your application as soon as your users encounter them alongside performance issues that are causing bad user experiences. Debug and release improvements and fixes for your customers faster than ever before using the Raygun software intelligence platform.

As you can see below, only a small amount of code is required to integrate this Raygun provider into your application. Full documentation is provided when you install this component, and a step-by-step guide is available when you create an application on your [Raygun.com](https://raygun.com/?utm_source=xamarin-components) dashboard.

In the code below, attaching both Crash Reporting and Real User Monitoring (Pulse) monitors the errors, crashes and performance issues affecting your users and presents them immediately for review within your Raygun dashboard.

```csharp
using Mindscape.Raygun4Net;
...

// Place this code somewhere in the main entry point of your application.
// For Android, make sure to pass in the main entry activity to the AttachPulse method.
RaygunClient.Initialize("YOUR_APP_API_KEY").AttachCrashReporting().AttachPulse();
```

[Sign up to Raygun.com](https://app.raygun.com/signup?utm_source=xamarin-components) to generate api keys for the above code and to view the error reports and performance data sent from your applications.
