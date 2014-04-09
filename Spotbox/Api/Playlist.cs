using Nancy;
using Nancy.ModelBinding;

using Newtonsoft.Json;

using Spotbox.Api.Models;

namespace Spotbox.Api
{
    public class Playlist : NancyModule
    {
        public Playlist(Spotify.Spotify spotify)
        {
            Get["/playlist"] = x =>
            {
                var playlist = spotify.GetCurrentPlaylist();
                var response = (Response)JsonConvert.SerializeObject(playlist);
                response.ContentType = "application/json";
                return response;
            };

            Post["/playlist"] = x =>
            {
                var trackSearch = this.Bind<SimpleInput>();
                var track = spotify.SearchForTrack(trackSearch.Value);

                if (track != null)
                {
                    spotify.GetCurrentPlaylist().AddTrack(track);
                    var response = (Response)JsonConvert.SerializeObject(track);
                    response.ContentType = "application/json";
                    return response;
                }

                return HttpStatusCode.NotFound;
            };

            Put["/playlist"] = x =>
            {
                var playlistName = this.Bind<SimpleInput>();
                var found = spotify.SetPlaylist(playlistName.Value);
                return found ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            };
        }
    }
}
