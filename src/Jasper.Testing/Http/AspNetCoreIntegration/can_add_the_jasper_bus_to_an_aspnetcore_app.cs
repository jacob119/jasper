﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jasper.Http;
using Jasper.Messaging;
using Lamar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Http.AspNetCoreIntegration
{

    public class AspNetCombinedFixture : IDisposable
    {
        public IWebHost theHost;

        public AspNetCombinedFixture()
        {
            // SAMPLE: adding-jasper-to-aspnetcore-app
            var builder = new WebHostBuilder();
            builder
                .UseKestrel()
                .UseUrls("http://localhost:3003")
                .UseStartup<Startup>() //;
                .UseJasper<SimpleJasperBusApp>();


            theHost = builder.Build();

            theHost.Start();
            // ENDSAMPLE
        }

        public void Dispose()
        {
            theHost?.Dispose();
        }
    }

    public class can_add_the_jasper_bus_to_an_aspnetcore_app : IClassFixture<AspNetCombinedFixture>
    {
        private readonly IWebHost theHost;


        public can_add_the_jasper_bus_to_an_aspnetcore_app(AspNetCombinedFixture fixture)
        {
            theHost = fixture.theHost;

        }

        // ReSharper disable once UnusedMember.Global
        public void sample()
        {
            // SAMPLE: ordering-middleware-with-jasper
            var builder = new WebHostBuilder();
            builder
                .UseKestrel()
                .UseUrls("http://localhost:3003")
                .ConfigureServices(s => s.AddMvc())
                .Configure(app =>
                {
                    app.UseMiddleware<CustomMiddleware>();

                    // Add the Jasper middleware
                    app.UseJasper();

                    // Nothing stopping you from using Jasper *and*
                    // MVC, NancyFx, or any other ASP.Net Core
                    // compatible framework
                    app.UseMvc();
                })
                .UseJasper<SimpleJasperBusApp>();


            var host = builder.Build();

            host.Start();
            // ENDSAMPLE
        }


        [Fact]
        public async Task can_handle_an_http_request_through_Kestrel()
        {
            using (var client = new HttpClient())
            {
                var text = await client.GetStringAsync("http://localhost:3003");
                text.ShouldContain("Hello from a hybrid Jasper application");

            }
        }

        [Fact]
        public async Task can_handle_an_http_request_through_Kestrel_to_MVC_route()
        {
            using (var client = new HttpClient())
            {
                var text = await client.GetStringAsync("http://localhost:3003/values");
                text.ShouldContain("Hello from MVC Core");

            }
        }

        [Fact]
        public void has_the_bus()
        {
            ShouldBeNullExtensions.ShouldNotBeNull(theHost.Services.GetService(typeof(IMessageContext)));
        }

        [Fact]
        public void captures_registrations_from_configure_registry()
        {
            theHost.Services.GetService(typeof(IFoo)).ShouldBeOfType<Foo>();
        }

        [Fact]
        public void is_definitely_using_a_StructureMap_service_provider()
        {
            theHost.Services.ShouldBeOfType<Container>();
        }
    }

    // SAMPLE: SimpleJasperBusApp
    public class SimpleJasperBusApp : JasperRegistry
    // ENDSAMPLE
    {
        public SimpleJasperBusApp()
        {

        }
    }


    public class CustomMiddleware
    {

    }
}

