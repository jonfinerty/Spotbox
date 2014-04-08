using Nancy;
using Nancy.ModelBinding;

using Newtonsoft.Json;

using Spotbox.Api.Models;
using Spotbox.Player;
using Spotbox.Player.Spotify;

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
                var trackSearch = this.Bind<SimpleInput>();
                var track = Spotify.SearchForTrack(trackSearch.Value);

                if (track != null)
                {
                    Audio.CurrentPlaylist.AddTrack(track);
                    return HttpStatusCode.OK;
                }
                return HttpStatusCode.NotFound;
            };

            Put["/playlist"] = x =>
            {
                var playlistName = this.Bind<SimpleInput>();
                var found = Audio.SetPlaylist(playlistName.Value);
                return found ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            };
        }
    }
}
