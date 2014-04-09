using System.Configuration;
using System.IO;
using System.Reflection;

using Spotbox.Player.Spotify;

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
            var spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);
            var spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];
            var spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];

            var spotify = new Spotify(spotifyApiKey, spotifyUsername, spotifyPassword);

            container.Register(typeof(Spotify), spotify);

            pipelines.BeforeRequest += (ctx) =>
            {
                _logger.InfoFormat("Received request {0} {1} by {2}", ctx.Request.Method, ctx.Request.Path, ctx.Request.UserHostAddress);
                return null;
            };
        }
    }
}
