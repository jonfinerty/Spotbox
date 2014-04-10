using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using libspotifydotnet;
using log4net;

namespace SpotSharp
{
    public class Playlist
    {
        private readonly Session session;

        private int _currentPosition;

        public int CurrentPosition
        {
            get
            {
                return _currentPosition;
            }

            internal set
            {
                if (value >= 0 && value < Tracks.Count)
                {
                    _currentPosition = value;

                    if (playlistChanged != null)
                    {
                        playlistChanged(this);
                    }
                }
            }
        }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IntPtr _callbacksPtr;

        internal PlaylistChangedDelegate playlistChanged;

        public delegate void PlaylistChangedDelegate(Playlist playlist);

        internal Playlist(IntPtr playlistPtr, Session session)
        {
            this.session = session;
            if (playlistPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid playlist pointer.");
            }

            PlaylistPtr = playlistPtr;
            AddCallbacks();

            Wait.For(() => libspotify.sp_playlist_is_loaded(PlaylistPtr));

            Metadata = new PlaylistInfo(playlistPtr, session);
            LoadTracks();
            CurrentPosition = 0;
        }

        ~Playlist()
        {
            libspotify.sp_playlist_remove_callbacks(PlaylistPtr, _callbacksPtr, IntPtr.Zero);
            libspotify.sp_playlist_release(PlaylistPtr);
        }
        
        internal IntPtr PlaylistPtr { get; private set; }

        public PlaylistInfo Metadata { get; private set; }

        public List<Track> Tracks { get; private set; }

        private void LoadTracks()
        {
            Tracks = new List<Track>();

            for (var i = 0; i < Metadata.TrackCount; i++)
            {
                var trackPtr = libspotify.sp_playlist_track(PlaylistPtr, i);
                Tracks.Add(new Track(trackPtr, session));
            }
        }

        public void AddTrack(Track track)
        {
            _logger.InfoFormat("Adding track: {0} to playlist: {1}", track.Name, Metadata.Name);
            var tracksPtr = IntPtr.Zero;
            
            var array = new int[1];
            array[0] = (int)track.TrackPtr;

            var size = Marshal.SizeOf(tracksPtr) * array.Length;
            tracksPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, tracksPtr, array.Length);
            libspotify.sp_playlist_add_tracks(PlaylistPtr, tracksPtr, 1, Metadata.TrackCount, session.SessionPtr);
        }

        internal void Play()
        {
            var track = Tracks[CurrentPosition];
            session.Play(track, PlayNextTrack);
        }

        internal void PlayPreviousTrack()
        {
            CurrentPosition--;
            Play();
        }

        internal void PlayNextTrack()
        {
            CurrentPosition++;
            Play();
        }

        internal Track GetCurrentTrack()
        {
            return Tracks[CurrentPosition];
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

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf((object) callbacks));
            Marshal.StructureToPtr(callbacks, _callbacksPtr, true);

            libspotify.sp_playlist_add_callbacks(PlaylistPtr, _callbacksPtr, IntPtr.Zero);
        }

        private void TracksAdded(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, int position, IntPtr userDataPtr)
        {
            foreach (var trackPtr in tracksPtr)
            {
                var newTrack = new Track(trackPtr, session);

                Tracks.Insert(position, newTrack);
                _logger.InfoFormat("Track sync added: {0}", newTrack.Name);

                if (position <= CurrentPosition)
                {
                    CurrentPosition++;
                }

                Metadata.TrackCount++;
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
                    Metadata.TrackCount--;
                    
                    if (CurrentPosition >= Metadata.TrackCount)
                    {
                        //was playing track at end which is now
                        CurrentPosition = Metadata.TrackCount - 1;
                        session.Pause();
                    }
                    else if (trackIndex <= CurrentPosition && CurrentPosition < trackIndex+trackCount)
                    {
                        _logger.InfoFormat("Currently playing track removed");
                        // both tracks before, and the current track have been removed
                        CurrentPosition = CurrentPosition - (trackCount - 1);
                        if (CurrentPosition >= Metadata.TrackCount)
                        {
                            CurrentPosition = Metadata.TrackCount - 1;
                            session.Pause();
                        }
                        else
                        {
                            Play();
                        }
                    }
                    else if (trackIndex <= CurrentPosition)
                    {
                        // just tracks before removed
                        CurrentPosition = CurrentPosition - trackCount;
                    }
                }
            }
        }

        private void TracksMoved(IntPtr playlistPtr, IntPtr[] tracksPtr, int trackCount, int newPosition, IntPtr userDataPtr)
        {
            _logger.InfoFormat("{0} Tracks moved", trackCount);
            foreach (var trackPtr in tracksPtr)
            {
                var tracksIndex = (int)trackPtr;

                // move currently playing pointer
                if (tracksIndex <= CurrentPosition && CurrentPosition < tracksIndex + trackCount)
                {
                    CurrentPosition = (CurrentPosition - tracksIndex) + newPosition;
                }
                else if (tracksIndex < CurrentPosition)
                {
                    CurrentPosition = CurrentPosition - trackCount;
                }

                // move tracks
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
            Metadata = new PlaylistInfo(playlistPtr, session);
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
