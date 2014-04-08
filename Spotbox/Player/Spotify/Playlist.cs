using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using libspotifydotnet;
using log4net;
using Newtonsoft.Json;

namespace Spotbox.Player.Spotify
{
    public class Playlist
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IntPtr _callbacksPtr;

        public Playlist(IntPtr playlistPtr)
        {
            if (playlistPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid playlist pointer.");
            }

            PlaylistPtr = playlistPtr;
            AddCallbacks();

            Wait.For(() => libspotify.sp_playlist_is_loaded(PlaylistPtr));

            PlaylistInfo = new PlaylistInfo(playlistPtr);
            LoadTracks();
        }

        ~Playlist()
        {
            libspotify.sp_playlist_remove_callbacks(PlaylistPtr, _callbacksPtr, IntPtr.Zero);
            libspotify.sp_playlist_release(PlaylistPtr);
        }

        [JsonIgnore]
        public IntPtr PlaylistPtr { get; private set; }

        public PlaylistInfo PlaylistInfo { get; private set; }

        public List<Track> Tracks { get; private set; }

        public void LoadTracks()
        {
            Tracks = new List<Track>();

            for (var i = 0; i < PlaylistInfo.TrackCount; i++)
            {
                var trackPtr = libspotify.sp_playlist_track(PlaylistPtr, i);
                Tracks.Add(new Track(trackPtr));
            }                
        }

        public void AddTrack(Track track)
        {
            _logger.InfoFormat("Adding track: {0} to playlist: {1}", track.Name, PlaylistInfo.Name);
            var tracksPtr = IntPtr.Zero;
            
            var array = new int[1];
            array[0] = (int)track.TrackPtr;

            var size = Marshal.SizeOf(tracksPtr) * array.Length;
            tracksPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, tracksPtr, array.Length);
            libspotify.sp_playlist_add_tracks(PlaylistPtr, tracksPtr, 1, PlaylistInfo.TrackCount, Spotify.GetSessionPtr());
        }

        #region Callbacks

        private delegate void TracksAddedDelegate(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, int position, IntPtr userDataPtr);
        private delegate void TracksRemovedDelegate(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, IntPtr userDataPtr);
        private delegate void TracksMovedDelegate(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, int newPosition, IntPtr userDataPtr);
        private delegate void PlaylistRenamedDelegate(IntPtr playlistPtr, IntPtr userDataPtr);
        private delegate void PlaylistStateChangedDelegate(IntPtr playlistPtr, IntPtr userDataPtr);
        private delegate void PlaylistUpdateInProgressDelegate(IntPtr playlistPtr, bool done, IntPtr userDataPtr);
        private delegate void PlaylistMetadataUpdatedDelegate(IntPtr playlistPtr, IntPtr userDataPtr);
        private delegate void TrackCreatedChangedDelegate(IntPtr playlistPtr, int position, IntPtr userPtr, int when, IntPtr userDataPtr);
        private delegate void TrackSeenChangedDelegate(IntPtr playlistPtr, int position, bool seen, IntPtr userDataPtr);
        private delegate void DescriptionChangedDelegate(IntPtr playlistPtr, IntPtr descPtr, IntPtr userDataPtr);
        private delegate void ImageChangedDelegate(IntPtr playlistPtr, IntPtr imagePtr, IntPtr userDataPtr);
        private delegate void TrackMessageChangedDelegate(IntPtr playlistPtr, int position, IntPtr messagePtr, IntPtr userDataPtr);
        private delegate void SubscribersChangedDelegate(IntPtr playlistPtr, IntPtr userDataPtr);

        private TracksAddedDelegate _tracksAddedDelegate;
        private TracksRemovedDelegate _tracksRemovedDelegate;
        private TracksMovedDelegate _tracksMovedDelegate;
        private PlaylistRenamedDelegate _playlistRenamedDelegate;
        private PlaylistStateChangedDelegate _playlistStateChangedDelegate;
        private PlaylistUpdateInProgressDelegate _playlistUpdateInProgressDelegate;
        private PlaylistMetadataUpdatedDelegate _playlistMetadataUpdatedDelegate;
        private TrackCreatedChangedDelegate _trackCreatedChangedDelegate;
        private TrackSeenChangedDelegate _trackSeenChangedDelegate;
        private DescriptionChangedDelegate _descriptionChangedDelegate;
        private ImageChangedDelegate _imageChangedDelegate;
        private TrackMessageChangedDelegate _trackMessageChangedDelegate;
        private SubscribersChangedDelegate _subscribersChangedDelegate;

        private void AddCallbacks()
        {
            _tracksAddedDelegate = TracksAdded;
            _tracksRemovedDelegate = TracksRemoved;
            _tracksMovedDelegate = TracksMoved;
            _playlistRenamedDelegate = PlaylistRenamed;
            _playlistStateChangedDelegate = StateChanged;
            _playlistUpdateInProgressDelegate = PlaylistUpdateInProgress;
            _playlistMetadataUpdatedDelegate = MetadataUpdated;
            _trackCreatedChangedDelegate = TrackCreatedChanged;
            _trackSeenChangedDelegate = TrackSeenChanged;
            _descriptionChangedDelegate = DescriptionChanged;
            _imageChangedDelegate = ImageChanged;
            _trackMessageChangedDelegate = TrackMessageChanged;
            _subscribersChangedDelegate = SubscribersChanged;

            var callbacks = new libspotify.sp_playlist_callbacks
            {
                tracks_added = Marshal.GetFunctionPointerForDelegate(_tracksAddedDelegate),
                tracks_removed = Marshal.GetFunctionPointerForDelegate(_tracksRemovedDelegate),
                tracks_moved = Marshal.GetFunctionPointerForDelegate(_tracksMovedDelegate),
                playlist_renamed = Marshal.GetFunctionPointerForDelegate(_playlistRenamedDelegate),
                playlist_state_changed = Marshal.GetFunctionPointerForDelegate(_playlistStateChangedDelegate),
                playlist_update_in_progress = Marshal.GetFunctionPointerForDelegate(_playlistUpdateInProgressDelegate),
                playlist_metadata_updated = Marshal.GetFunctionPointerForDelegate(_playlistMetadataUpdatedDelegate),
                track_created_changed = Marshal.GetFunctionPointerForDelegate(_trackCreatedChangedDelegate),
                track_seen_changed = Marshal.GetFunctionPointerForDelegate(_trackSeenChangedDelegate),
                description_changed = Marshal.GetFunctionPointerForDelegate(_descriptionChangedDelegate),
                image_changed = Marshal.GetFunctionPointerForDelegate(_imageChangedDelegate),
                track_message_changed = Marshal.GetFunctionPointerForDelegate(_trackMessageChangedDelegate),
                subscribers_changed = Marshal.GetFunctionPointerForDelegate(_subscribersChangedDelegate)
            };

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, _callbacksPtr, true);

            libspotify.sp_playlist_add_callbacks(PlaylistPtr, _callbacksPtr, IntPtr.Zero);
        }

        private void TracksAdded(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, int position, IntPtr userDataPtr)
        {
            foreach (var trackPtr in tracksPtr)
            {
                var newTrack = new Track(trackPtr);
                Tracks.Insert(position, newTrack);
                _logger.InfoFormat("Track sync added: {0}", newTrack.Name);
                PlaylistInfo.TrackCount++;
                position++;
            }
        }

        private void TracksRemoved(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, IntPtr userDataPtr)
        {
            foreach (var trackPtr in tracksPtr)
            {
                var trackIndex = (int)trackPtr;
                for (var i = 0; i < trackCount; i++)
                {
                    _logger.InfoFormat("Track sync removed: {0}", Tracks[trackIndex].Name);
                    Tracks.RemoveAt(trackIndex);
                    PlaylistInfo.TrackCount--;
                }
            }
        }

        private void TracksMoved(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, int newPosition, IntPtr userDataPtr)
        {
            _logger.InfoFormat("{0} Tracks moved", trackCount);
            foreach (var trackPtr in tracksPtr)
            {
                var tracksIndex = (int)trackPtr;
                var movingTracks = Tracks.GetRange(tracksIndex, trackCount);
                Tracks.RemoveRange(tracksIndex, trackCount);
                if (newPosition > tracksIndex)
                {
                    newPosition = newPosition - trackCount;
                }

                Tracks.InsertRange(newPosition, movingTracks);
            }
        }

        private void PlaylistRenamed(IntPtr playlistPtr, IntPtr userDataPtr)
        {
            PlaylistInfo = new PlaylistInfo(playlistPtr);
        }

        private void StateChanged(IntPtr playlistPtr, IntPtr userDataPtr){ }

        private void PlaylistUpdateInProgress(IntPtr playlistPtr, bool done, IntPtr userDataPtr){ }

        private void MetadataUpdated(IntPtr playlistPtr, IntPtr userDataPtr){ }

        private void TrackCreatedChanged(IntPtr playlistPtr, int position, IntPtr userPtr, int when, IntPtr userDataPtr){ }

        private void TrackSeenChanged(IntPtr playlistPtr, int position, bool seen, IntPtr userDataPtr){ }

        private void DescriptionChanged(IntPtr playlistPtr, IntPtr descPtr, IntPtr userDataPtr){ }

        private void ImageChanged(IntPtr playlistPtr, IntPtr imagePtr, IntPtr userDataPtr){ }

        private void TrackMessageChanged(IntPtr playlistPtr, int position, IntPtr messagePtr, IntPtr userDataPtr){ }

        private void SubscribersChanged(IntPtr playlistPtr, IntPtr userDataPtr){ }


        #endregion
    }
}
