using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.AspNetCore.Tests;
using Mindscape.Raygun4Net.AspNetCore.Tests.TestLib;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMvc();
builder.Services.AddRaygun(builder.Configuration, settings =>
{
  settings.EnvironmentVariables.Add("USER_*");
  settings.EnvironmentVariables.Add("POSH_*");
});

// because we're using a library that uses Raygun, we need to initialize that too
RaygunClientFactory.Initialize(builder.Configuration["RaygunSettings:ApiKey"]);

var app = builder.Build();

var env = app.Environment;
if (env.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}
else
{
  app.UseExceptionHandler("/Error");
}

app.UseRaygun();
app.UseRouting();

app.MapRazorPages();

app.MapGet("/", () => "Hello World!");

app.Run();