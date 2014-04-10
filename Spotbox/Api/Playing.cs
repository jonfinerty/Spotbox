using System.IO;
using Nancy;

using Newtonsoft.Json;
using SpotSharp;

namespace Spotbox.Api
{
    public class Playing : NancyModule
    {
        public Playing(Spotify spotify)
        {
            Get["/playing"] = x =>
            {
                var track = spotify.GetCurrentTrack();
                var response = (Response) JsonConvert.SerializeObject(track);
                response.ContentType = "application/json";
                return response;
            };

            Get["/playing/cover.jpeg"] = x =>
            {
                var track = spotify.GetCurrentTrack();
                if (track != null)
                {
                    var imageBytes = track.GetAlbumArt();
                    return new ByteArrayResponse(imageBytes, "image/jpeg");
                }

                return new ByteArrayResponse(new byte[0], "image/jpeg");
            };
        }

        private class ByteArrayResponse : Response
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
