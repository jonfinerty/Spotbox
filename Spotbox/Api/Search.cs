using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using Spotbox.Api.Models;
using SpotSharp;

namespace Spotbox.Api
{
    public class Search : NancyModule
    {
        public Search(Spotify spotify)
        {
            Post["/search"] = x =>
            {
                var searchTerms = this.Bind<SearchQueryModel>();
                var tracks = spotify.SearchForTracks(searchTerms.Query);
                var response = (Response)JsonConvert.SerializeObject(tracks, new LinkJsonConverter());
                response.ContentType = "application/json";
                return response;
            };
        }
    }
}
