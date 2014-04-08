using System;
using System.Collections.Generic;

using libspotifydotnet;
using Newtonsoft.Json;

namespace Spotbox.Player.Spotify
{
    public class Track
    {
        public Track(IntPtr trackPtr)
        {
            TrackPtr = trackPtr;
            Wait.For(() => libspotify.sp_track_is_loaded(TrackPtr), 10);
            SetTrackMetaData();
        }

        [JsonIgnore]
        public IntPtr TrackPtr { get; private set; }

        public string Name { get; private set; }

        public int Length { get; private set; }

        [JsonIgnore]
        public IntPtr AlbumPtr { get; private set; }

        public List<string> Artists { get; private set; }

        public byte[] GetAlbumArt()
        {
            var cover = new AlbumCover(AlbumPtr);
            return cover.GetImageBytes();
        }

        private void SetTrackMetaData()
        {
            Name = libspotify.sp_track_name(TrackPtr).PtrToString();
            Length = (int)(libspotify.sp_track_duration(TrackPtr) / 1000M);

            AlbumPtr = libspotify.sp_track_album(TrackPtr);

            Artists = new List<string>();
            var artistCount = libspotify.sp_track_num_artists(TrackPtr);
            for (var i = 0; i < artistCount; i++)
            {
                var artistPtr = libspotify.sp_track_artist(TrackPtr, i);
                if (artistPtr != IntPtr.Zero)
                {
                    Artists.Add(libspotify.sp_artist_name(artistPtr).PtrToString());
                }
            }
        }
    }
}
