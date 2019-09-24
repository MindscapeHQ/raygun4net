using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mindscape.Raygun4Net.AspNetCore;

namespace Mindscape.Raygun4Net.AspNetCore2.Tests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddRaygun(Configuration, new RaygunMiddlewareSettings()
            {
                // adds an optional example of over riding the client provider
                ClientProvider = new ExampleRaygunAspNetCoreClientProvider()
            });
            
            // because we're using a library that uses Raygun, we need to initalize that too
            TestLib.RaygunClientFactory.Initialize(Configuration["RaygunSettings:ApiKey"]);
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            
            app.UseRaygun();

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}