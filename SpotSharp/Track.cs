using System;
using System.Collections.Generic;
using System.Reflection;
using libspotifydotnet;
using log4net;

namespace SpotSharp
{
    public class Track
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Session session;

        internal Track(IntPtr trackPtr, Session session)
        {
            this.session = session;
            TrackPtr = trackPtr;
            Wait.For(IsLoaded);
            SetTrackMetaData();
        }
        
        internal IntPtr TrackPtr { get; private set; }

        public string Name { get; private set; }

        public int Length { get; private set; }

        internal IntPtr AlbumPtr { get; private set; }

        public List<string> Artists { get; private set; }

        public byte[] GetAlbumArt()
        {
            var cover = new AlbumCover(AlbumPtr, session);
            return cover.ImageBytes;
        }

        private void SetTrackMetaData()
        {
            Name = Extensions.PtrToString(libspotify.sp_track_name(TrackPtr));
            Length = (int)(libspotify.sp_track_duration(TrackPtr) / 1000M);

            AlbumPtr = libspotify.sp_track_album(TrackPtr);

            Artists = new List<string>();
            var artistCount = libspotify.sp_track_num_artists(TrackPtr);
            for (var i = 0; i < artistCount; i++)
            {
                var artistPtr = libspotify.sp_track_artist(TrackPtr, i);
                if (artistPtr != IntPtr.Zero)
                {
                    Artists.Add(Extensions.PtrToString(libspotify.sp_artist_name(artistPtr)));
                }
            }
        }

        private bool IsLoaded()
        {
            if (libspotify.sp_track_get_availability(session.SessionPtr, TrackPtr) != libspotify.sp_availability.SP_TRACK_AVAILABILITY_AVAILABLE)
            {
                _logger.WarnFormat("Unavailable Track Created");
                return true;
            }

            return libspotify.sp_track_is_loaded(TrackPtr);
        }
    }
}
