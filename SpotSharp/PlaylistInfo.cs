using System;
using libspotifydotnet;

namespace SpotSharp
{
    public class PlaylistInfo
    {
        private readonly Session session;
        
        internal IntPtr PlaylistPtr { get; private set; }

        public string Name { get; private set; }
        public int TrackCount { get; set; }
        public string Description { get; private set; }
        public int SubscriberCount { get; private set; }

        public libspotify.sp_playlist_type PlaylistType { get; private set; }

        public bool IsInRam { get; private set; }
        
        public libspotify.sp_playlist_offline_status OfflineStatus { get; private set; }

        public Link Link { get; private set; }

        internal PlaylistInfo(IntPtr playlistPtr, Session session)
        {
            this.session = session;
            PlaylistPtr = playlistPtr;

            Wait.For(() => libspotify.sp_playlist_is_loaded(PlaylistPtr));

            SetPlaylistInfo(playlistPtr);
        }

        private void SetPlaylistInfo(IntPtr playlistPtr)
        {
            PlaylistType = libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST;
            Name = libspotify.sp_playlist_name(playlistPtr).PtrToString();
            TrackCount = libspotify.sp_playlist_num_tracks(playlistPtr);
            Description = libspotify.sp_playlist_get_description(PlaylistPtr).PtrToString();
            SubscriberCount = (int) libspotify.sp_playlist_num_subscribers(PlaylistPtr);
            IsInRam = libspotify.sp_playlist_is_in_ram(session.SessionPtr, PlaylistPtr);
            OfflineStatus = libspotify.sp_playlist_get_offline_status(session.SessionPtr, PlaylistPtr);
            Link = new Link(PlaylistPtr, LinkType.Playlist);
        }

        public Playlist GetPlaylist()
        {
            return new Playlist(PlaylistPtr, session);
        }
    }
}