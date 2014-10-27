Raygun4Net is a small library that can listen to all unhandled exceptions in your Android and iOS applications and send the details to your [Raygun.io](https://raygun.io/) account. This allows you to see issues in your application as soon as your users find them. Then you can debug and release a new bug-free version faster than ever before.

As you can see below, there is not much code required at all to integrate this Raygun provider into your application. Full documentation is provided when you install this component, and a step-by-step guide is available when you register an application on your [Raygun.io](https://raygun.io/) dashboard.

```csharp
using Mindscape.Raygun4Net;
...

// Place this code somewhere in the main entry point of your application.
RaygunClient.Attach("YOUR_APP_API_KEY");
```
