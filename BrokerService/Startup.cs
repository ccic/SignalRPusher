﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PushServer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        public IConfigurationRoot Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddOptions();
            services.AddSingleton(typeof(IPusher<>), typeof(DefaultPusher<>));
            //services.Configure<BrokerOption>(Configuration);
            BrokerOption bo = new BrokerOption();
            var cn = Configuration.GetValue<int>("BrokerOption:ConnectionNumber");
            bo.ConnectionNumber = cn == 0 ? 1 : cn;
            services.AddSingleton(typeof(BrokerOption), bo);
            Console.WriteLine("Concurrent connection number: {0}", Configuration.GetValue<int>("BrokerOption:ConnectionNumber"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSignalR(routes =>
            {
                routes.MapHub<ServerHub>("/server");
                routes.MapHub<ClientHub>("/client");
            });
        }
    }
}
