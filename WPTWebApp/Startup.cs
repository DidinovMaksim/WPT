using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Hangfire;
[assembly: OwinStartup(typeof(WPTWebApp.Startup))]

namespace WPTWebApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            
            GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection");

            app.UseHangfireDashboard();
            app.UseHangfireServer();
        }
    }
}
