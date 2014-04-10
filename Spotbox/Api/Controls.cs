using Nancy;
using SpotSharp;

namespace Spotbox.Api
{
    public class Controls : NancyModule
    {
        public Controls(Spotify spotify)
        {
            Post["/play"] = x =>
            {
                spotify.Unpause();
                return HttpStatusCode.OK;
            };

            Post["/pause"] = x =>
            {
                spotify.Pause();
                return HttpStatusCode.OK;
            };

            Post["/next"] = x =>
            {
                spotify.PlayNextTrack();
                return HttpStatusCode.OK;
            };

            Post["/prev"] = x =>
            {
                spotify.PlayPreviousTrack();
                return HttpStatusCode.OK;
            };
        }   
    }
}
