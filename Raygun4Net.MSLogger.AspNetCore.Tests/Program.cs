using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registers the Raygun Client for AspNetCore
builder.Services.AddRaygun(settings =>
{
  settings.ApiKey = "*your_api_key*";
});
builder.Services.AddSingleton<IMessageBuilder, DefaultTags>();
   
// (Optional) Registers the Raygun User Provider
// builder.Services.AddRaygunUserProvider();

// Registers the Raygun Logger for use in MS Logger
builder.Logging.AddRaygunLogger(x =>
{
  x.OnlyLogExceptions = false;
  x.MinimumLogLevel = LogLevel.Information;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
  name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


public class DefaultTags : IMessageBuilder
{
  public Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
  {
    message.Details.Tags.Add("DefaultTag");
    
    return Task.FromResult(message);
  }
}