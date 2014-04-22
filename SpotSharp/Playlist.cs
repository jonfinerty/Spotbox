using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using libspotifydotnet;
using log4net;

namespace SpotSharp
{
    public class Playlist : IDisposable
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
                if (value >= 0 && value < TrackCount)
                {
                    _logger.InfoFormat("Playlist: {0} position set to: {1}", Name, value);
                    _currentPosition = value;

                    if (playlistChanged != null)
                    {
                        playlistChanged(this);
                    }
                }
            }
        }

        public string Name { get; private set; }

        public int TrackCount { get; set; }

        public string Description { get; private set; }

        public int SubscriberCount { get; private set; }

        public libspotify.sp_playlist_type PlaylistType { get; private set; }

        public bool IsInRam { get; private set; }

        public libspotify.sp_playlist_offline_status OfflineStatus { get; private set; }

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private IntPtr _callbacksPtr = IntPtr.Zero;

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

            Wait.For(() => libspotify.sp_playlist_is_loaded(PlaylistPtr));

            SetPlaylistInfo(playlistPtr);
        }

        public Link Link { get; private set; }

        public void Dispose()
        {
            if (_callbacksPtr != IntPtr.Zero)
            {
                libspotify.sp_playlist_remove_callbacks(PlaylistPtr, _callbacksPtr, IntPtr.Zero);
            }
            libspotify.sp_playlist_release(PlaylistPtr);
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

        internal IntPtr PlaylistPtr { get; private set; }

        private List<Track> _tracks;

        public IEnumerable<Track> Tracks
        {
            get
            {
                if (_tracks != null)
                {
                    return _tracks.AsReadOnly();
                }

                return null;
            }
        }

        private void LoadTracks()
        {
            if (_tracks != null)
            {
                return;
            }

            AddCallbacks();

            _tracks = new List<Track>();

            for (var i = 0; i < TrackCount; i++)
            {
                var trackPtr = libspotify.sp_playlist_track(PlaylistPtr, i);
                _tracks.Add(new Track(trackPtr, session));
            }
        }

        public bool AddTrack(Link trackLink)
        {
            LoadTracks();

            var trackPtr = libspotify.sp_link_as_track(trackLink.LinkPtr);
            if (trackPtr == IntPtr.Zero)
            {
                return false;
            }
            var track = new Track(trackPtr, session);
            _logger.InfoFormat("Adding track: {0} to playlist: {1}", track.Name, Name);
            var tracksPtr = IntPtr.Zero;
            
            var array = new int[1];
            array[0] = (int)track.TrackPtr;

            var size = Marshal.SizeOf(tracksPtr) * array.Length;
            tracksPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, tracksPtr, array.Length);
            libspotify.sp_playlist_add_tracks(PlaylistPtr, tracksPtr, 1, TrackCount, session.SessionPtr);
            return true;
        }

        internal void Play()
        {
            LoadTracks();
            var track = _tracks[CurrentPosition];
            session.Play(track, PlayNextTrack);
        }

        internal void PlayPreviousTrack()
        {
            LoadTracks();
            CurrentPosition--;
            Play();
        }

        internal void PlayNextTrack()
        {
            LoadTracks();
            CurrentPosition++;
            Play();
        }

        internal Track GetCurrentTrack()
        {
            LoadTracks();
            return _tracks[CurrentPosition];
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
                if (libspotify.sp_track_get_availability(session.SessionPtr, trackPtr) !=
                    libspotify.sp_availability.SP_TRACK_AVAILABILITY_AVAILABLE)
                {
                    return;
                }

                var newTrack = new Track(trackPtr, session);

                _tracks.Insert(position, newTrack);
                _logger.InfoFormat("Track sync added: {0}", newTrack.Name);


                if (newTrack.Name == string.Empty)
                {
                    Console.WriteLine("blah");
                    return;
                }

                if (position <= CurrentPosition)
                {
                    CurrentPosition++;
                }

                TrackCount++;
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
                    _logger.InfoFormat("Track sync removed: {0}", _tracks[trackIndex].Name);
                    _tracks.RemoveAt(trackIndex);
                    TrackCount--;
                    
                    if (CurrentPosition >= TrackCount)
                    {
                        //was playing track at end which is now
                        CurrentPosition = TrackCount - 1;
                        session.Pause();
                    }
                    else if (trackIndex <= CurrentPosition && CurrentPosition < trackIndex+trackCount)
                    {
                        _logger.InfoFormat("Currently playing track removed");
                        // both tracks before, and the current track have been removed
                        CurrentPosition = CurrentPosition - (trackCount - 1);
                        if (CurrentPosition >= TrackCount)
                        {
                            CurrentPosition = TrackCount - 1;
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
                var movingTracks = _tracks.GetRange(tracksIndex, trackCount);
                _tracks.RemoveRange(tracksIndex, trackCount);
                if (newPosition > tracksIndex)
                {
                    newPosition = newPosition - trackCount;
                }

                _tracks.InsertRange(newPosition, movingTracks);
            }
        }

        private void PlaylistRenamed(IntPtr playlistPtr, IntPtr userDataPtr)
        {
            SetPlaylistInfo(playlistPtr);
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

        #region Equality Members

        protected bool Equals(Playlist other)
        {
            return Equals(Link, other.Link);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Playlist)obj);
        }

        public override int GetHashCode()
        {
            return Link != null ? Link.GetHashCode() : 0;
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
