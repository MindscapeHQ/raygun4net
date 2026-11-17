using System.Net;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Breadcrumbs;
using Mindscape.Raygun4Net.Offline;
using Mindscape.Raygun4Net.Storage;

// IMPORTANT: this program must never read properties of RaygunMessage / RaygunMessageDetails etc.
// Reading a getter in app code makes the trimmer keep it, which would hide the bug we're testing.

// The library reports swallowed failures through Trace; surface them so a failing run explains itself.
System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener(useErrorStream: true));

var handler = new CapturingHandler();
var client = new RaygunClient(new RaygunSettings { ApiKey = "trim-test", ThrowOnError = true }, new HttpClient(handler));

RaygunBreadcrumbs.Record("breadcrumb before the error");

// A current W3C activity puts Details.TraceContext in the payload, so the trimmed payload is checked for it too.
using var activity = new System.Diagnostics.Activity("trim-test").SetIdFormat(System.Diagnostics.ActivityIdFormat.W3C).Start();

try
{
  throw new InvalidOperationException("Trimming test exception");
}
catch (Exception ex)
{
  var customData = new Dictionary<string, object>
  {
    ["String"] = "value",
    ["Number"] = 42,
    ["Nested"] = new Dictionary<string, object> { ["Inner"] = true },
  };

  await client.SendAsync(ex, new List<string> { "trim-test" }, customData, new RaygunIdentifierMessage("user@example.com"));
}

if (string.IsNullOrEmpty(handler.Body))
{
  Console.Error.WriteLine("No payload was sent.");
  return 1;
}

Console.WriteLine(handler.Body);

// The offline store round-trips CrashReportStoreEntry through SimpleJson's reflection-based deserializer.
var storeDirectory = Path.Combine(Path.GetTempPath(), "raygun-trimming-test-" + Guid.NewGuid().ToString("N"));
try
{
  using var sendStrategy = new TimerBasedSendStrategy(TimeSpan.FromHours(1));
  var store = new FileSystemCrashReportStore(sendStrategy, storeDirectory);
  await store.Save(handler.Body, "trim-test", CancellationToken.None);
  var entries = await store.GetAll(CancellationToken.None);

  var ok = entries.Count == 1 && entries[0].Id != Guid.Empty && entries[0].ApiKey == "trim-test" && entries[0].MessagePayload == handler.Body;
  Console.Error.WriteLine($"offline store round trip: {(ok ? "ok" : "FAILED")}");
  return ok ? 0 : 2;
}
finally
{
  if (Directory.Exists(storeDirectory)) Directory.Delete(storeDirectory, true);
}

sealed class CapturingHandler : HttpMessageHandler
{
  public string? Body { get; private set; }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    Body = await request.Content!.ReadAsStringAsync(cancellationToken);
    return new HttpResponseMessage(HttpStatusCode.Accepted);
  }
}
