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

    public class Playlist : IDisposable {             
        
        private delegate void tracks_added_delegate(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int position, IntPtr userDataPtr);
        private delegate void tracks_removed_delegate(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, IntPtr userDataPtr);
        private delegate void tracks_moved_delegate(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int new_position, IntPtr userDataPtr);
        private delegate void playlist_renamed_delegate(IntPtr playlistPtr, IntPtr userDataPtr);
        private delegate void playlist_state_changed_delegate(IntPtr playlistPtr, IntPtr userDataPtr);
        private delegate void playlist_update_in_progress_delegate(IntPtr playlistPtr, bool done, IntPtr userDataPtr);
        private delegate void playlist_metadata_updated_delegate(IntPtr playlistPtr, IntPtr userDataPtr);
        private delegate void track_created_changed_delegate(IntPtr playlistPtr, int position, IntPtr userPtr, int when, IntPtr userDataPtr);
        private delegate void track_seen_changed_delegate(IntPtr playlistPtr, int position, bool seen, IntPtr userDataPtr);
        private delegate void description_changed_delegate(IntPtr playlistPtr, IntPtr descPtr, IntPtr userDataPtr);
        private delegate void image_changed_delegate(IntPtr playlistPtr, IntPtr imagePtr, IntPtr userDataPtr);
        private delegate void track_message_changed_delegate(IntPtr playlistPtr, int position, IntPtr messagePtr, IntPtr userDataPtr);
        private delegate void subscribers_changed_delegate(IntPtr playlistPtr, IntPtr userDataPtr);

        private tracks_added_delegate fn_tracks_added;
        private tracks_removed_delegate fn_tracks_removed;
        private tracks_moved_delegate fn_tracks_moved;
        private playlist_renamed_delegate fn_playlist_renamed;        
        private playlist_state_changed_delegate fn_playlist_state_changed;
        private playlist_update_in_progress_delegate fn_playlist_update_in_progress;
        private playlist_metadata_updated_delegate fn_playlist_metadata_updated;
        private track_created_changed_delegate fn_track_created_changed;
        private track_seen_changed_delegate fn_track_seen_changed;
        private description_changed_delegate fn_description_changed;
        private image_changed_delegate fn_image_changed;
        private track_message_changed_delegate fn_track_message_changed;
        private subscribers_changed_delegate fn_subscribers_changed;
                        
        private IntPtr _callbacksPtr;
        private bool _disposed;

        public string Name { get; private set; }
        public int TrackCount { get; private set; }
        public string Description { get; private set; }
        public int SubscriberCount { get; private set; }
        public bool IsInRAM { get; private set; }
        public libspotify.sp_playlist_offline_status OfflineStatus { get; private set; }       
        public IntPtr Pointer { get; private set; }

        private List<Track> _tracks;

        #region IDisposable Members

        public void Dispose() {

            dispose(true);
            GC.SuppressFinalize(this);

        }

        ~Playlist() {

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

        public static Playlist Get(IntPtr playlistPtr) {

            Playlist p = new Playlist((IntPtr)playlistPtr);           
            return p;

        }
               
        private Playlist(IntPtr playlistPtr) {

            Pointer = playlistPtr;

            initCallbacks();

            if (IsLoaded) {

                populateMetadata();
                return;

            }
            
        }

        public bool IsLoaded {

            get { return Pointer != IntPtr.Zero && libspotify.sp_playlist_is_loaded(Pointer); }

        }

        public bool TracksAreLoaded {

            get {

                if (Pointer == IntPtr.Zero
                    || !IsLoaded)
                    return false;

                for (int i = 0; i < TrackCount; i++) {

                    IntPtr trackPtr = libspotify.sp_playlist_track(Pointer, i);
                    if (!libspotify.sp_track_is_loaded(trackPtr))
                        return false;

                }

                return true;

            }

        }

        private void initCallbacks() {

            if (Pointer == IntPtr.Zero)
                throw new InvalidOperationException("Invalid playlist pointer.");            

            fn_tracks_added = new tracks_added_delegate(tracks_added);
            fn_tracks_removed = new tracks_removed_delegate(tracks_removed);
            fn_tracks_moved = new tracks_moved_delegate(tracks_moved);
            fn_playlist_renamed = new playlist_renamed_delegate(playlist_renamed);
            fn_playlist_state_changed = new playlist_state_changed_delegate(state_changed);
            fn_playlist_update_in_progress = new playlist_update_in_progress_delegate(playlist_update_in_progress);
            fn_playlist_metadata_updated = new playlist_metadata_updated_delegate(metadata_updated);
            fn_track_created_changed = new track_created_changed_delegate(track_created_changed);
            fn_track_seen_changed = new track_seen_changed_delegate(track_seen_changed);
            fn_description_changed = new description_changed_delegate(description_changed);
            fn_image_changed = new image_changed_delegate(image_changed);
            fn_track_message_changed = new track_message_changed_delegate(track_message_changed);
            fn_subscribers_changed = new subscribers_changed_delegate(subscribers_changed);

            libspotify.sp_playlist_callbacks callbacks = new libspotify.sp_playlist_callbacks();

            callbacks.tracks_added = Marshal.GetFunctionPointerForDelegate(fn_tracks_added);
            callbacks.tracks_removed = Marshal.GetFunctionPointerForDelegate(fn_tracks_removed);
            callbacks.tracks_moved = Marshal.GetFunctionPointerForDelegate(fn_tracks_moved);
            callbacks.playlist_renamed = Marshal.GetFunctionPointerForDelegate(fn_playlist_renamed);
            callbacks.playlist_state_changed = Marshal.GetFunctionPointerForDelegate(fn_playlist_state_changed);
            callbacks.playlist_update_in_progress = Marshal.GetFunctionPointerForDelegate(fn_playlist_update_in_progress);
            callbacks.playlist_metadata_updated = Marshal.GetFunctionPointerForDelegate(fn_playlist_metadata_updated);
            callbacks.track_created_changed = Marshal.GetFunctionPointerForDelegate(fn_track_created_changed);
            callbacks.track_seen_changed = Marshal.GetFunctionPointerForDelegate(fn_track_seen_changed);
            callbacks.description_changed = Marshal.GetFunctionPointerForDelegate(fn_description_changed);
            callbacks.image_changed = Marshal.GetFunctionPointerForDelegate(fn_image_changed);
            callbacks.track_message_changed = Marshal.GetFunctionPointerForDelegate(fn_track_message_changed);
            callbacks.subscribers_changed = Marshal.GetFunctionPointerForDelegate(fn_subscribers_changed);

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, _callbacksPtr, true);

            libspotify.sp_playlist_add_callbacks(Pointer, _callbacksPtr, IntPtr.Zero);

        }

        private void safeRemoveCallbacks() {

            try {

                if (Pointer == IntPtr.Zero)
                    return;

                if (_callbacksPtr == IntPtr.Zero)
                    return;

                libspotify.sp_playlist_remove_callbacks(Pointer, _callbacksPtr, IntPtr.Zero);

            } catch { }

        }

        public List<Track> GetTracks() {

            if (_tracks == null) {

                _tracks = new List<Track>();

                for (int i = 0; i < TrackCount; i++) {

                    IntPtr trackPtr = libspotify.sp_playlist_track(Pointer, i);

                    _tracks.Add(new Track(trackPtr));

                }

            }

            return _tracks;
            
        }

        private void populateMetadata() {

            Name = Functions.PtrToString(libspotify.sp_playlist_name(Pointer));
            TrackCount = libspotify.sp_playlist_num_tracks(Pointer);
            Description = Functions.PtrToString(libspotify.sp_playlist_get_description(Pointer));
            SubscriberCount = (int)libspotify.sp_playlist_num_subscribers(Pointer);
            IsInRAM = libspotify.sp_playlist_is_in_ram(Session.GetSessionPtr(), Pointer);
            OfflineStatus = libspotify.sp_playlist_get_offline_status(Session.GetSessionPtr(), Pointer);
            TrackCount = libspotify.sp_playlist_num_tracks(Pointer);

        }

        private void tracks_added(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int position, IntPtr userDataPtr) {

        }

        private void tracks_removed(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, IntPtr userDataPtr) {
        
        }

        private void tracks_moved(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int new_position, IntPtr userDataPtr) {
            
        }

        private void playlist_renamed(IntPtr playlistPtr, IntPtr userDataPtr) {

            populateMetadata();

        }
                       
        private void state_changed(IntPtr playlistPtr, IntPtr userDataPtr) {
                        
        }

        private void playlist_update_in_progress(IntPtr playlistPtr, bool done, IntPtr userDataPtr) {
            
        }

        private void metadata_updated(IntPtr playlistPtr, IntPtr userDataPtr) {
            
        }

        private void track_created_changed(IntPtr playlistPtr, int position, IntPtr userPtr, int when, IntPtr userDataPtr) {

        }

        private void track_seen_changed(IntPtr playlistPtr, int position, bool seen, IntPtr userDataPtr) {

        }

        private void description_changed(IntPtr playlistPtr, IntPtr descPtr, IntPtr userDataPtr) {

        }
        
        private void image_changed(IntPtr playlistPtr, IntPtr imagePtr, IntPtr userDataPtr) {

        }

        private void track_message_changed(IntPtr playlistPtr, int position, IntPtr messagePtr, IntPtr userDataPtr) {

        }

        private void subscribers_changed(IntPtr playlistPtr, IntPtr userDataPtr) {

        }        

    }

}
