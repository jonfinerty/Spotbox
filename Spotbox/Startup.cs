using System.Reflection;

using log4net;

using Microsoft.Owin;
using Microsoft.Owin.Cors;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

using Owin;

using Spotbox;

[assembly: OwinStartup(typeof(Startup))]

namespace Spotbox
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map(
                "/signalr",
                map =>
                {
                    map.UseCors(CorsOptions.AllowAll);
                map.RunSignalR();
            });
            app.UseNancy();
        }
    }

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            pipelines.BeforeRequest += (ctx) =>
            {
                _logger.InfoFormat("Received request {0} {1} by {2}", ctx.Request.Method, ctx.Request.Path, ctx.Request.UserHostAddress);
                return null;
            };
        }
    }
}
