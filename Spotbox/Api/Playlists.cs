using Nancy;
using Newtonsoft.Json;
using Spotbox.Player.Spotify;

namespace Spotbox.Api
{
    public class Playlists : NancyModule
    {
        public Playlists()
        {
            Get["/playlists"] = x =>
            {                
                var playlistContainer = Spotify.GetSessionUserPlaylists();
                var response = (Response)JsonConvert.SerializeObject(playlistContainer);
                response.ContentType = "application/json";
                return response;
            };
        }
    }
}
