﻿using System;
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
        AlbumCover = 5,
        User = 6
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
                case LinkType.AlbumCover:
                {
                    LinkPtr = libspotify.sp_link_create_from_album_cover(objectPtr);
                    break;
                }
                case LinkType.Playlist:
                {
                    LinkPtr = libspotify.sp_link_create_from_playlist(objectPtr);
                    break;
                }
            }
        }

        public Link(string link)
        {
            LinkPtr = libspotify.sp_link_create_from_string(link);
            var spotLinkType = libspotify.sp_link_type(LinkPtr);
            switch (spotLinkType)
            {
                case libspotify.sp_linktype.SP_LINKTYPE_ALBUM:
                {
                    LinkType = LinkType.Album;
                    break;
                }
                case libspotify.sp_linktype.SP_LINKTYPE_IMAGE:
                {
                    LinkType = LinkType.AlbumCover;
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