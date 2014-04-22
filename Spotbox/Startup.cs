using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Owin;

using Spotbox;
using SpotSharp;

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
        private readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            var spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);
            var spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];
            var spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];

            var spotify = new SpotSharp.SpotSharp(spotifyApiKey);
            spotify.Login(spotifyUsername, spotifyPassword);

            spotify.TrackChanged = BroadcastTrackChange;
            spotify.PlaylistChanged = SavePlaylistPositionToSettings;

            // for api modules
            container.Register(spotify);

            // for application startup
            TinyIoCContainer.Current.Register(spotify);            

            pipelines.BeforeRequest += (ctx) =>
            {
                _logger.InfoFormat("Received request {0} {1} by {2}", ctx.Request.Method, ctx.Request.Path, ctx.Request.UserHostAddress);
                return null;
            };
        }

        private void BroadcastTrackChange(Track track)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<PushHub>();
            hubContext.Clients.All.newTrack(JsonConvert.SerializeObject(track));
        }

        private void SavePlaylistPositionToSettings(Playlist playlist)
        {
            Settings.Default.CurrentPlaylistLink = playlist.Link.ToString();
            Settings.Default.CurrentPlaylistPosition = playlist.CurrentPosition;
            Settings.Default.Save();
        }

    }

    public class LinkJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var link = (Link)value;
            writer.WriteValue(link.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Link);
        }
    }
}
