using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using libspotifydotnet;
using Newtonsoft.Json;

namespace Spotbox.Player.Spotify
{
    public class PlaylistContainer : IDisposable
    {
        [JsonIgnore]
        public IntPtr PlaylistContainerPtr { get; private set; }
        [JsonProperty("Playlists")]
        public List<PlaylistInfo> PlaylistInfos { get; private set; }

        private delegate void playlist_added_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);
        private delegate void playlist_removed_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);
        private delegate void playlist_moved_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, int new_position, IntPtr userDataPtr);
        private delegate void container_loaded_delegate(IntPtr containerPtr, IntPtr userDataPtr);

        private container_loaded_delegate fn_container_loaded_delegate;
        private playlist_added_delegate fn_playlist_added_delegate;
        private playlist_moved_delegate fn_playlist_moved_delegate;
        private playlist_removed_delegate fn_playlist_removed_delegate;
        
        private IntPtr _callbacksPtr;
        private bool _disposed;

        public PlaylistContainer(IntPtr playlistContainerPtr)
        {
            PlaylistContainerPtr = playlistContainerPtr;
            InitCallbacks();
            Wait.For(() => IsLoaded() && PlaylistInfosAreLoaded(), 10);            
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PlaylistContainer()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                SafeRemoveCallbacks();
            }

            _disposed = true;
        }

        #endregion

        private void InitCallbacks()
        {
            fn_container_loaded_delegate = new container_loaded_delegate(container_loaded);
            fn_playlist_added_delegate = new playlist_added_delegate(playlist_added);
            fn_playlist_moved_delegate = new playlist_moved_delegate(playlist_moved);
            fn_playlist_removed_delegate = new playlist_removed_delegate(playlist_removed);

            libspotify.sp_playlistcontainer_callbacks callbacks = new libspotify.sp_playlistcontainer_callbacks();
            callbacks.container_loaded = Marshal.GetFunctionPointerForDelegate(fn_container_loaded_delegate);
            callbacks.playlist_added = Marshal.GetFunctionPointerForDelegate(fn_playlist_added_delegate);
            callbacks.playlist_moved = Marshal.GetFunctionPointerForDelegate(fn_playlist_moved_delegate);
            callbacks.playlist_removed = Marshal.GetFunctionPointerForDelegate(fn_playlist_removed_delegate);

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, _callbacksPtr, true);

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

        private void SafeRemoveCallbacks()
        {
            try
            {
                if (PlaylistContainerPtr == IntPtr.Zero)
                    return;

                if (_callbacksPtr == IntPtr.Zero)
                    return;

                libspotify.sp_playlistcontainer_remove_callbacks(PlaylistContainerPtr, _callbacksPtr, IntPtr.Zero);
            }
            catch { }

        }

        private void container_loaded(IntPtr containerPtr, IntPtr userDataPtr)
        {
            Console.WriteLine("container_loaded");
        }

        private void playlist_added(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr)
        {
            Console.WriteLine("playlist_added at position {0}", position);
        }

        private void playlist_moved(IntPtr containerPtr, IntPtr playlistPtr, int position, int new_position, IntPtr userDataPtr)
        {
            Console.WriteLine("playlist_moved from {0} to {1}", position, new_position);
        }

        private void playlist_removed(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr)
        {
            Console.WriteLine("playlist_removed");
        }

    }

}
