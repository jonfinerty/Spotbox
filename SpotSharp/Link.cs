using System;
using System.Runtime.InteropServices;
using System.Text;
using libspotifydotnet;

namespace SpotSharp
{
    public enum LinkType
    {
        Track = 0,
        Album = 1,
        Artist = 2,
        Search = 3,
        Playlist = 4,
        Profile = 5,
        Starred = 6,
        Localtrack = 7,
        Image = 8,
        AlbumCover = 9,
    }

    public class Link
    {
        internal IntPtr LinkPtr;

        internal Link(IntPtr objectPtr, LinkType linkType)
        {
            switch (linkType)
            {
                case LinkType.Track:
                {
                    LinkPtr = libspotify.sp_link_create_from_track(objectPtr, 0);
                    break;
                }
                case LinkType.AlbumCover :
                {
                    LinkPtr = libspotify.sp_link_create_from_album_cover(objectPtr);
                    break;
                }
            }
        }

        public Link(string link)
        {
            LinkPtr = libspotify.sp_link_create_from_string(link);
        }

        public override string ToString()
        {
            const int bufferLength = 200;
            var bufferPointer = Marshal.AllocHGlobal(bufferLength);

            libspotify.sp_link_as_string(LinkPtr, bufferPointer, bufferLength);

            if (bufferPointer == IntPtr.Zero)
            {
                Marshal.FreeHGlobal(bufferPointer);
                return string.Empty;
            }

            var len = 0;
            while (Marshal.ReadByte(bufferPointer, len) != 0)
            {
                len++;
            }

            if (len == 0)
            {
                Marshal.FreeHGlobal(bufferPointer);
                return string.Empty;
            }

            var buffer = new byte[len - 1];
            Marshal.Copy(bufferPointer, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(bufferPointer);

            return Encoding.UTF8.GetString(buffer);
        }

        ~Link()
        {
            if (LinkPtr != IntPtr.Zero)
            {
                libspotify.sp_link_release(LinkPtr);
            }
        }
    }
}
