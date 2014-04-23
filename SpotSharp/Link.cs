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
        User = 5
    }

    public class Link
    {
        internal IntPtr LinkPtr;

        public LinkType LinkType { get; private set; }

        internal Link(IntPtr objectPtr, LinkType linkType)
        {
            LinkType = linkType;

            switch (linkType)
            {
                case LinkType.Track:
                {
                    LinkPtr = libspotify.sp_link_create_from_track(objectPtr, 0);
                    break;
                }
                case LinkType.Album:
                {
                    LinkPtr = libspotify.sp_link_create_from_album(objectPtr);
                    break;
                }
                case LinkType.Artist:
                {
                    LinkPtr = libspotify.sp_link_create_from_artist(objectPtr);
                    break;
                }
                case LinkType.Search:
                {
                    LinkPtr = libspotify.sp_link_create_from_search(objectPtr);
                    break;
                }
                case LinkType.User:
                {
                    LinkPtr = libspotify.sp_link_create_from_user(objectPtr);
                    break;
                }
                case LinkType.Playlist:
                {
                    LinkPtr = libspotify.sp_link_create_from_playlist(objectPtr);
                    break;
                }
            }
        }

        private Link(string linkString)
        {
            LinkPtr = libspotify.sp_link_create_from_string(linkString);
            if (LinkPtr == IntPtr.Zero)
            {
                return;
            }

            var spotLinkType = libspotify.sp_link_type(LinkPtr);
            switch (spotLinkType)
            {
                case libspotify.sp_linktype.SP_LINKTYPE_ALBUM:
                    {
                        LinkType = LinkType.Album;
                        break;
                    }
                case libspotify.sp_linktype.SP_LINKTYPE_ARTIST:
                    {
                        LinkType = LinkType.Artist;
                        break;
                    }
                case libspotify.sp_linktype.SP_LINKTYPE_PLAYLIST:
                    {
                        LinkType = LinkType.Playlist;
                        break;
                    }
                case libspotify.sp_linktype.SP_LINKTYPE_SEARCH:
                    {
                        LinkType = LinkType.Search;
                        break;
                    }
                case libspotify.sp_linktype.SP_LINKTYPE_TRACK:
                    {
                        LinkType = LinkType.Track;
                        break;
                    }
                case libspotify.sp_linktype.SP_LINKTYPE_PROFILE:
                    {
                        LinkType = LinkType.User;
                        break;
                    }
            }
        }

        public static Link Create(string linkString)
        {
            var link = new Link(linkString);
            if (link.LinkPtr != IntPtr.Zero)
            {
                return link;
            }

            return null;
        }

        ~Link()
        {
            if (LinkPtr != IntPtr.Zero)
            {
                libspotify.sp_link_release(LinkPtr);
            }
        }

        public override string ToString()
        {
            if (LinkPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            const int BufferLength = 200;
            var bufferPointer = Marshal.AllocHGlobal(BufferLength);

            libspotify.sp_link_as_string(LinkPtr, bufferPointer, BufferLength);

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

            var buffer = new byte[len];
            Marshal.Copy(bufferPointer, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(bufferPointer);

            return Encoding.UTF8.GetString(buffer);
        }

        #region Equality Members

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        protected bool Equals(Link other)
        {
            return ToString().Equals(other.ToString());
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

            return Equals((Link)obj);
        }

        #endregion
    }
}
