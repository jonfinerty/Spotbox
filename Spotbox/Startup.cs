using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using Spotbox;

[assembly: OwinStartup(typeof(Startup))]
namespace Spotbox
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/signalr", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.RunSignalR();
            });
            app.UseNancy();
        }
    }
}
