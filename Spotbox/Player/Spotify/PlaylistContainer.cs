/*-
 * Copyright (c) 2012 Software Development Solutions, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using libspotifydotnet;

namespace Spotbox.Player.Spotify {

    public class PlaylistContainer : IDisposable {

        private delegate void playlist_added_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);
        private delegate void playlist_removed_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);
        private delegate void playlist_moved_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, int new_position, IntPtr userDataPtr);
        private delegate void container_loaded_delegate(IntPtr containerPtr, IntPtr userDataPtr);
        
        private container_loaded_delegate fn_container_loaded_delegate;
        private playlist_added_delegate fn_playlist_added_delegate;
        private playlist_moved_delegate fn_playlist_moved_delegate;
        private playlist_removed_delegate fn_playlist_removed_delegate;

        private IntPtr _containerPtr;
        private IntPtr _callbacksPtr;
        private bool _disposed;
        
        private static PlaylistContainer _sessionContainer;

        public class PlaylistInfo {
            public IntPtr ContainerPtr;
            public IntPtr Pointer;
            public ulong FolderID;
            public libspotify.sp_playlist_type PlaylistType;
            public string Name;
            public PlaylistInfo Parent;
            public List<PlaylistInfo> Children = new List<PlaylistInfo>();
        }

        private PlaylistContainer(IntPtr containerPtr) {

            _containerPtr = containerPtr;
            initCallbacks();

        }

        #region IDisposable Members

        public void Dispose() {

            dispose(true);
            GC.SuppressFinalize(this);

        }

        ~PlaylistContainer() {

            dispose(false);

        }

        private void dispose(bool disposing) {

            if(!_disposed) {

                if(disposing) {

                    safeRemoveCallbacks();
                    
                }

                _disposed = true;

            }
                        
        }

        #endregion

        public static PlaylistContainer GetSessionContainer() {
            
            if (_sessionContainer == null) {

                if (Session.GetSessionPtr() == IntPtr.Zero)
                    throw new InvalidOperationException("No valid session.");

                _sessionContainer = new PlaylistContainer(libspotify.sp_session_playlistcontainer(Session.GetSessionPtr()));

            }

            return _sessionContainer; 

        }

        public static PlaylistContainer Get(IntPtr containerPtr) {            

            return new PlaylistContainer(containerPtr);
            
        }

        private void initCallbacks() {

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

            libspotify.sp_playlistcontainer_add_callbacks(_containerPtr, _callbacksPtr, IntPtr.Zero);
                        
            return;
                       
        }

        public bool IsLoaded {

            get {

                return libspotify.sp_playlistcontainer_is_loaded(_containerPtr);

            }

        }

        public bool PlaylistsAreLoaded {

            get {

                if (!IsLoaded)
                    return false;

                int count = libspotify.sp_playlistcontainer_num_playlists(_containerPtr);

                for (int i = 0; i < count; i++) {                    

                    if(libspotify.sp_playlistcontainer_playlist_type(_containerPtr, i) == libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST) {

                        using (Playlist p = Playlist.Get(libspotify.sp_playlistcontainer_playlist(_containerPtr, i))) {

                            if (!p.IsLoaded)
                                return false;

                        }

                    }

                }

                return true;

            }

        }

        public List<PlaylistInfo> GetAllPlaylists() {

            if (!GetSessionContainer().IsLoaded)
                throw new InvalidOperationException("Container is not loaded.");

            List<PlaylistInfo> playlists = new List<PlaylistInfo>();

            for (int i = 0; i < libspotify.sp_playlistcontainer_num_playlists(_containerPtr); i++) {                

                if (libspotify.sp_playlistcontainer_playlist_type(_containerPtr, i) == libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST) {

                    IntPtr playlistPtr = libspotify.sp_playlistcontainer_playlist(_containerPtr, i);

                    playlists.Add(new PlaylistInfo() {
                        Pointer = playlistPtr,
                        PlaylistType = libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST,
                        ContainerPtr = _containerPtr,
                        Name = Functions.PtrToString(libspotify.sp_playlist_name(playlistPtr))
                    });

                }

            }

            return playlists;

        }

        private void safeRemoveCallbacks() {

            try {

                if (_containerPtr == IntPtr.Zero)
                    return;

                if (_callbacksPtr == IntPtr.Zero)
                    return;

                libspotify.sp_playlistcontainer_remove_callbacks(_containerPtr, _callbacksPtr, IntPtr.Zero);

            } catch { }

        }

        public List<PlaylistInfo> GetChildren(PlaylistInfo info) {

            if (!GetSessionContainer().IsLoaded)
                throw new InvalidOperationException("Container is not loaded.");

            PlaylistInfo tree = buildTree();

            if (info == null) {

                return tree.Children;

            } else {

                return searchTreeRecursive(tree, info).Children;

            }           

        }

        private PlaylistInfo searchTreeRecursive(PlaylistInfo tree, PlaylistInfo find) {

            if (tree.FolderID == find.FolderID)
                return tree;

            foreach (PlaylistInfo playlist in tree.Children) {

                if (playlist.PlaylistType == libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_START_FOLDER) {

                    if (playlist.FolderID == find.FolderID)
                        return playlist;

                    PlaylistInfo p2 = searchTreeRecursive(playlist, find);

                    if (p2 != null)
                        return p2;

                }
                    
            }

            return null;

        }

        private PlaylistInfo buildTree() {

            PlaylistInfo current = new PlaylistInfo();
            current.FolderID = ulong.MaxValue; //root

            for (int i = 0; i < libspotify.sp_playlistcontainer_num_playlists(_containerPtr); i++) {

                PlaylistInfo playlist = new PlaylistInfo() {
                    PlaylistType = libspotify.sp_playlistcontainer_playlist_type(_containerPtr, i),
                    ContainerPtr = _containerPtr
                };

                switch (playlist.PlaylistType) {

                    case libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_START_FOLDER:

                        playlist.FolderID = libspotify.sp_playlistcontainer_playlist_folder_id(_containerPtr, i);
                        playlist.Name = getFolderName(_containerPtr, i);
                        playlist.Parent = current;
                        current.Children.Add(playlist);
                        current = playlist;

                        break;

                    case libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_END_FOLDER:

                        current = current.Parent;
                        break;

                    case libspotify.sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST:
                        
                        playlist.Pointer = libspotify.sp_playlistcontainer_playlist(_containerPtr, i);
                        playlist.Parent = current;
                        current.Children.Add(playlist);
                                                
                        break;

                }


            }

            while (current.Parent != null) {

                current = current.Parent;

            }

            return current;

        }

        private string getFolderName(IntPtr containerPtr, int index) {

            IntPtr namePtr = Marshal.AllocHGlobal(128);

            try {

                libspotify.sp_error error = libspotify.sp_playlistcontainer_playlist_folder_name(containerPtr, index, namePtr, 128);

                return Functions.PtrToString(namePtr);

            } finally {

                Marshal.FreeHGlobal(namePtr);

            }

        }

        private void container_loaded(IntPtr containerPtr, IntPtr userDataPtr) {

            Console.WriteLine( "container_loaded");                                   
            
        }

        private void playlist_added(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr) {

            Console.WriteLine( "playlist_added at position {0}", position);
            
        }
        
        private void playlist_moved(IntPtr containerPtr, IntPtr playlistPtr, int position, int new_position, IntPtr userDataPtr) {

            Console.WriteLine( "playlist_moved from {0} to {1}", position, new_position);

        }

        private void playlist_removed(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr) {

            Console.WriteLine( "playlist_removed");

        }
               
    }

}
