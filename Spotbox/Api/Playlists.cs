using Nancy;
using Newtonsoft.Json;
using SpotSharp;

namespace Spotbox.Api
{
    public class Playlists : NancyModule
    {
        public Playlists(Spotify spotify)
        {
            Get["/playlists"] = x =>
            {
                var playlists = spotify.GetAllPlaylists();
                var response = (Response)JsonConvert.SerializeObject(playlists, new LinkJsonConverter());
                response.ContentType = "application/json";
                return response;
            };
        }
    }
}
