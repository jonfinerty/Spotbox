using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using libspotifydotnet;
using Newtonsoft.Json;
using Spotbox.Player.Libspotifydotnet;

namespace Spotbox.Player.Spotify
{
    public class Track
    {
        [JsonIgnore]
        public IntPtr TrackPtr { get; private set; }
        public string Name { get; private set; }
        public int Length { get; private set; }

        [JsonIgnore]
        public IntPtr AlbumPtr { get; private set; }

        public List<string> Artists { get; private set; }

        public Track(IntPtr trackPtr)
        {
            TrackPtr = trackPtr;
            Wait.For(IsLoaded, 10);

            SetTrackMetaData();
        }

        ~Track()
        {
            //libspotify.sp_track_release(TrackPtr);
        }

        private bool IsLoaded()
        {
            return libspotify.sp_track_is_loaded(TrackPtr);
        }

        private void SetTrackMetaData()
        {
            Name = Functions.PtrToString(libspotify.sp_track_name(TrackPtr));
            Length = (int)(libspotify.sp_track_duration(TrackPtr) / 1000M);

            AlbumPtr = libspotify.sp_track_album(TrackPtr);

            Artists = new List<string>();
            var artistCount = libspotify.sp_track_num_artists(TrackPtr);
            for (var i = 0; i < artistCount; i++)
            {
                var artistPtr = libspotify.sp_track_artist(TrackPtr, i);
                if (artistPtr != IntPtr.Zero)
                {
                    Artists.Add(Functions.PtrToString(libspotify.sp_artist_name(artistPtr)));
                }
            }
        }

        public byte[] GetAlbumArt()
        {
            var coverPtr = libspotify.sp_album_cover(AlbumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_LARGE);

            // NOTE: in API 10 sp_image_is_loaded() always returns true despite empty byte buffer, so using
            // callbacks now to determine when loaded.  Not sure how this will behave with cached images...

            using (var image = Image.Load(libspotify.sp_image_create(Session.GetSessionPtr(), coverPtr)))
            {
                Wait.For(() => image.IsLoaded, 10);

                int bufferSize;
                var imageDataBufferPtr = libspotify.sp_image_data(image.ImagePtr, out bufferSize);
                var buffer = new byte[bufferSize];
                Marshal.Copy(imageDataBufferPtr, buffer, 0, buffer.Length);

                return buffer;
            }
        }
    }
}
