using System;
using System.Runtime.InteropServices;

using libspotifydotnet;

namespace Spotbox.Player.Spotify 
{
    public class AlbumCover 
    {
        public byte[] ImageBytes { get; private set; }

        public AlbumCover(IntPtr albumPtr, Session session)
        {
            var coverPtr = libspotify.sp_album_cover(albumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_LARGE);
            var ptr = libspotify.sp_image_create(session.SessionPtr, coverPtr);
            ImagePtr = ptr;

            // sp_image_loaded seems to always be returning true, check for bytes returned
            Wait.For(LoadImageBytes);
        }

        private bool LoadImageBytes()
        {
            int bufferSize;
            var imageDataBufferPtr = libspotify.sp_image_data(ImagePtr, out bufferSize);
            if (bufferSize > 0)
            {
                ImageBytes = new byte[bufferSize];
                Marshal.Copy(imageDataBufferPtr, ImageBytes, 0, ImageBytes.Length);
                return true;
            }

            return false;
        }

        ~AlbumCover()
        {
            libspotify.sp_image_release(ImagePtr);
        }

        public IntPtr ImagePtr { get; private set; }
    }
}
