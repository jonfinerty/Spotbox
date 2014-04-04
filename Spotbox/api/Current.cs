using System.IO;
using Nancy;

using Newtonsoft.Json;
using Spotbox.Player.Spotify;

namespace Spotbox.Api
{
    public class Current : NancyModule
    {
        public Current()
        {
            Get["/current"] = x =>
            {
                var track = Player.Player.CurrentlyPlayingTrack;
                var response = (Response) JsonConvert.SerializeObject(track);
                response.ContentType = "application/json";
                return response;
            };

            Post["/current/play"] = x =>
            {
                Player.Player.Play();
                return HttpStatusCode.OK;
            };

            Post["/current/pause"] = x =>
            {
                Player.Player.Pause();
                return HttpStatusCode.OK;
            };

            Post["/current/next"] = x =>
            {
                Player.Player.Next();
                return HttpStatusCode.OK;
            };

            Post["/current/prev"] = x =>
            {
                Player.Player.Previous();
                return HttpStatusCode.OK;
            };

            Get["/current/cover.jpeg"] = x =>
            {
                return new ByteArrayResponse(Spotify.GetAlbumArt(Player.Player.CurrentlyPlayingTrack.Album.AlbumPtr), "image/jpeg");
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
