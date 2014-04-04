using System;
using libspotifydotnet;
using Newtonsoft.Json;
using Spotbox.Player.Libspotifydotnet;

namespace Spotbox.Player.Spotify
{
    public class PlaylistInfo
    {
        [JsonIgnore]
        public IntPtr PlaylistPtr { get; private set; }
        public string Name { get; private set; }

        [JsonIgnore]
        public libspotify.sp_playlist_type PlaylistType;
        public int TrackCount { get; private set; }
        public string Description { get; private set; }
        public int SubscriberCount { get; private set; }

        public PlaylistInfo(IntPtr playlistPtr)
        {
            PlaylistPtr = playlistPtr;

            Wait.For(() => libspotify.sp_playlist_is_loaded(PlaylistPtr), 10);

            PlaylistType = libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST;
            Name = Functions.PtrToString(libspotify.sp_playlist_name(playlistPtr));
            TrackCount = libspotify.sp_playlist_num_tracks(playlistPtr);
            Description = Functions.PtrToString(libspotify.sp_playlist_get_description(PlaylistPtr));
            SubscriberCount = (int)libspotify.sp_playlist_num_subscribers(PlaylistPtr); 
        }
    }
}