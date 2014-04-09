using Nancy;
using Spotbox.Player;
using Spotbox.Player.Spotify;

namespace Spotbox.Api
{
    public class Controls : NancyModule
    {
        public Controls(Spotify spotify)
        {
            Post["/play"] = x =>
            {
                spotify.GetCurrentPlaylist().Play();
                return HttpStatusCode.OK;
            };

            Post["/pause"] = x =>
            {
                spotify.GetCurrentPlaylist().Pause();
                return HttpStatusCode.OK;
            };

            Post["/next"] = x =>
            {
                spotify.GetCurrentPlaylist().PlayNextTrack();
                return HttpStatusCode.OK;
            };

            Post["/prev"] = x =>
            {
                spotify.GetCurrentPlaylist().PlayPreviousTrack();
                return HttpStatusCode.OK;
            };
        }   
    }
}
