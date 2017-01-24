// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#if false 
namespace Microsoft.Perks.JsonRpc
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
  
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
        }

        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // todo: this should really discover the plugin assemblies and use that rather 
            // todo: than hardcoding the controllers we know about at build time.

            // manually add the controllers. 
            services.AddMvc().AddControllersAsServices();
            
            services.Configure<MvcJsonOptions>(options =>
            {
                options.SerializerSettings.Converters = new JsonConverter[] {new StringEnumConverter()};
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

                options.SerializerSettings.ObjectCreationHandling = ObjectCreationHandling.Reuse;
            });
            
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // setup default routes.
            app.UseMvc();
        }
    }
}
#endif 