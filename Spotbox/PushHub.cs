using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Spotbox.Player;

namespace Spotbox
{
    public class PushHub : Hub
    {
        public void RequestTrack()
        {
            Clients.Caller.newTrack(JsonConvert.SerializeObject(Audio.CurrentlyPlayingTrack));
        }
    }
}
