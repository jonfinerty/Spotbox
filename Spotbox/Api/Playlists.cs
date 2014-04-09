using Nancy;
using Newtonsoft.Json;

namespace Spotbox.Api
{
    public class Playlists : NancyModule
    {
        public Playlists(Spotify.Spotify spotify)
        {
            Get["/playlists"] = x =>
            {
                var playlists = spotify.GetAllPlaylists();
                var response = (Response)JsonConvert.SerializeObject(playlists);
                response.ContentType = "application/json";
                return response;
            };
        }
    }
}
