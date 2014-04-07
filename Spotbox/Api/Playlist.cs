using Nancy;
using Newtonsoft.Json;
using Spotbox.Player;

namespace Spotbox.Api
{
    public class Playlist : NancyModule
    {
        public Playlist()
        {
            Get["/playlist"] = x =>
            {
                var playlist = Audio.CurrentPlaylist;
                var response = (Response)JsonConvert.SerializeObject(playlist);
                response.ContentType = "application/json";
                return response;
            };

            Post["/playlist"] = x =>
            {
                return 200;
            };

            Put["/playlist"] = x =>
            {
                return 200;
            };
        }
    }
}
