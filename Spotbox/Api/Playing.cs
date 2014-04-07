using System.IO;
using Nancy;

using Newtonsoft.Json;
using Spotbox.Player;

namespace Spotbox.Api
{
    public class Playing : NancyModule
    {
        public Playing()
        {
            Get["/playing"] = x =>
            {
                var track = Audio.CurrentlyPlayingTrack;
                var response = (Response) JsonConvert.SerializeObject(track);
                response.ContentType = "application/json";
                return response;
            };

            Get["/playing/cover.jpeg"] = x =>
            {
                var imageBytes = Audio.CurrentlyPlayingTrack.GetAlbumArt();
                return new ByteArrayResponse(imageBytes, "image/jpeg");
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
