using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using libspotifydotnet;
using Newtonsoft.Json;

namespace Spotbox.Player.Spotify
{
    public class PlaylistContainer
    {
        [JsonIgnore]
        public IntPtr PlaylistContainerPtr { get; private set; }
        private IntPtr _callbacksPtr;

        [JsonProperty("Playlists")]
        public List<PlaylistInfo> PlaylistInfos { get; private set; }

        public PlaylistContainer(IntPtr playlistContainerPtr)
        {
            PlaylistContainerPtr = playlistContainerPtr;
            AddCallbacks();
            Wait.For(() => IsLoaded() && PlaylistInfosAreLoaded(), 10);            
        }
  
        ~PlaylistContainer()
        {
            libspotify.sp_playlistcontainer_remove_callbacks(PlaylistContainerPtr, _callbacksPtr, IntPtr.Zero);
        }


        private delegate void PlaylistAddedDelegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);
        private delegate void PlaylistRemovedDelegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);
        private delegate void PlaylistMovedDelegate(IntPtr containerPtr, IntPtr playlistPtr, int oldPosition, int newPosition, IntPtr userDataPtr);
        private delegate void ContainerLoadedDelegate(IntPtr containerPtr, IntPtr userDataPtr);

        private void AddCallbacks()
        {
            var containerLoadedDelegate = new ContainerLoadedDelegate(PlayListContainerLoaded);
            var playlistAddedDelegate = new PlaylistAddedDelegate(PlaylistAdded);
            var playlistMovedDelegate = new PlaylistMovedDelegate(PlaylistMoved);
            var playlistRemovedDelegate = new PlaylistRemovedDelegate(PlaylistRemoved);

            var playlistcontainerCallbacks = new libspotify.sp_playlistcontainer_callbacks
            {
                container_loaded = Marshal.GetFunctionPointerForDelegate(containerLoadedDelegate),
                playlist_added = Marshal.GetFunctionPointerForDelegate(playlistAddedDelegate),
                playlist_moved = Marshal.GetFunctionPointerForDelegate(playlistMovedDelegate),
                playlist_removed = Marshal.GetFunctionPointerForDelegate(playlistRemovedDelegate)
            };

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(playlistcontainerCallbacks));
            Marshal.StructureToPtr(playlistcontainerCallbacks, _callbacksPtr, true);
            libspotify.sp_playlistcontainer_add_callbacks(PlaylistContainerPtr, _callbacksPtr, IntPtr.Zero);
        }

        private bool IsLoaded()
        {
            return libspotify.sp_playlistcontainer_is_loaded(PlaylistContainerPtr);
        }

        private bool PlaylistInfosAreLoaded()
        {
            if (!IsLoaded())
            {
                return false;
            }                

            PlaylistInfos = new List<PlaylistInfo>();

            var playlistCount = libspotify.sp_playlistcontainer_num_playlists(PlaylistContainerPtr);

            for (var i = 0; i < playlistCount; i++)
            {
                if (libspotify.sp_playlistcontainer_playlist_type(PlaylistContainerPtr, i) == libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST)
                {
                    var playlistPtr = libspotify.sp_playlistcontainer_playlist(PlaylistContainerPtr, i);
                    var playlistInfo = new PlaylistInfo(playlistPtr);
                    PlaylistInfos.Add(playlistInfo);
                }
            }

            return true;
        }

        private void PlayListContainerLoaded(IntPtr containerPtr, IntPtr userDataPtr)
        {
            Console.WriteLine("PlayListContainerLoaded");
        }

        private void PlaylistAdded(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr)
        {
            Console.WriteLine("Playlist added at position {0}", position);
        }

        private void PlaylistMoved(IntPtr containerPtr, IntPtr playlistPtr, int oldPosition, int newPosition, IntPtr userDataPtr)
        {
            Console.WriteLine("Playlist moved from {0} to {1}", oldPosition, newPosition);
        }

        private void PlaylistRemoved(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr)
        {
            Console.WriteLine("Playlist Removed from position {0}", position);
        }

    }
}
