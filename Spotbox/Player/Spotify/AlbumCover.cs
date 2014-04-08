using System;
using System.Runtime.InteropServices;

using libspotifydotnet;

namespace Spotbox.Player.Spotify 
{
    public class AlbumCover 
    {
        public AlbumCover(IntPtr albumPtr)
        {
            var coverPtr = libspotify.sp_album_cover(albumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_LARGE);
            var ptr = libspotify.sp_image_create(Session.GetSessionPtr(), coverPtr);
            ImagePtr = ptr;            
            Wait.For(() => libspotify.sp_image_is_loaded(ImagePtr), 10);
        }

        ~AlbumCover()
        {
            libspotify.sp_image_release(ImagePtr);
        }

        public IntPtr ImagePtr { get; private set; }

        public byte[] GetImageBytes()
        {
            int bufferSize;
            var imageDataBufferPtr = libspotify.sp_image_data(ImagePtr, out bufferSize);
            var buffer = new byte[bufferSize];
            Marshal.Copy(imageDataBufferPtr, buffer, 0, buffer.Length);

            return buffer;
        }
    }
}
