using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Session;
using Newtonsoft.Json;

using Spotbox.Api.Models;
using SpotSharp;

namespace Spotbox.Api
{
    public class Playlist : NancyModule
    {
        public Playlist(SpotSharp.SpotSharp spotify)
        {
            Get["/playlist"] = x =>
            {
                var playlist = spotify.GetCurrentPlaylist();
                var response = (Response)JsonConvert.SerializeObject(playlist, new LinkJsonConverter());
                response.ContentType = "application/json";
                return response;
            };

            Post["/playlist"] = x =>
            {
                var linkModel = this.Bind<LinkModel>();

                if (linkModel.Link != null)
                {
                    var trackLink = Link.Create(linkModel.Link);
                    if (trackLink != null && trackLink.LinkType == LinkType.Track)
                    {
                        var trackAdded = spotify.GetCurrentPlaylist().AddTrack(trackLink);
                        if (trackAdded)
                        {
                            return HttpStatusCode.NoContent;
                        }
                    }
                }

                return HttpStatusCode.BadRequest;
            };

            Put["/playlist"] = x =>
            {
                var linkModel = this.Bind<LinkModel>();
                if (linkModel.Link != null)
                {
                    var playlistLink = Link.Create(linkModel.Link);
                    if (playlistLink != null && playlistLink.LinkType == LinkType.Playlist)
                    {
                        var playlistSet = spotify.SetCurrentPlaylist(playlistLink);

                        if (playlistSet)
                        {
                            spotify.Play();
                            return HttpStatusCode.OK;
                        }
                    }
                }

                return HttpStatusCode.BadRequest;
            };
        }
    }
}
