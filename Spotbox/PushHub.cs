using Microsoft.AspNet.SignalR;

using Nancy.TinyIoc;

using Newtonsoft.Json;

using Spotbox.Player.Spotify;

namespace Spotbox
{
    public class PushHub : Hub
    {
        public void RequestTrack()
        {
            var spotify = TinyIoCContainer.Current.Resolve<Spotify>();
            var track = spotify.GetCurrentPlaylist().GetCurrentTrack();
            Clients.Caller.newTrack(JsonConvert.SerializeObject(track));
        }
    }
}
