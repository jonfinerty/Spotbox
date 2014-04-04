using System.IO;

using Jamcast.Plugins.Spotify.API;

using JukeApi;

using Nancy;

using Newtonsoft.Json;

namespace Spotbox.api
{
    public class Current : NancyModule
    {
        public Current()
        {
            Get["/current"] = x =>
            {
                var track = Player.CurrentlyPlayingTrack;
                var response = (Response) JsonConvert.SerializeObject(track);
                response.ContentType = "application/json";
                return response;
            };

            Post["/current/play"] = x =>
            {
                Player.Play();
                return HttpStatusCode.OK;
            };

            Post["/current/pause"] = x =>
            {
                Player.Pause();
                return HttpStatusCode.OK;
            };

            Post["/current/next"] = x =>
            {
                Player.Next();
                return HttpStatusCode.OK;
            };

            Post["/current/prev"] = x =>
            {
                Player.Previous();
                return HttpStatusCode.OK;
            };

            Get["/current/cover.jpeg"] = x =>
            {
                return new ByteArrayResponse(Spotify.GetAlbumArt(Player.CurrentlyPlayingTrack.Album.AlbumPtr), "image/jpeg");
            };
        }

        public class ByteArrayResponse : Response
        {
            public ByteArrayResponse(byte[] body, string contentType = null)
            {
                ContentType = contentType ?? "application/octet-stream";

                Contents = stream =>
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(body);
                    }
                };
            }
        }
    }
}
