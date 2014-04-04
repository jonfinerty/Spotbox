/*-
 * Copyright (c) 2012 Software Development Solutions, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using libspotifydotnet;

namespace Spotbox.Player.Libspotifydotnet
{

    public class Spotify
    {

        private delegate bool Test();
        public delegate void MainThreadMessageDelegate(object[] args);

        private static AutoResetEvent _programSignal;
        private static AutoResetEvent _mainSignal;
        private static Queue<MainThreadMessage> _mq = new Queue<MainThreadMessage>();
        private static bool _shutDown = false;
        private static object _syncObj = new object();
        private static object _initSync = new object();
        private static bool _initted = false;
        private static bool _isRunning = false;
        private static Action<IntPtr> d_notify = new Action<IntPtr>(Session_OnNotifyMainThread);
        private static Action<IntPtr> d_on_logged_in = new Action<IntPtr>(Session_OnLoggedIn);
        private static Thread _t;

        private static readonly int REQUEST_TIMEOUT = 30;

        private class MainThreadMessage
        {
            public MainThreadMessageDelegate d;
            public object[] payload;
        }

        public static bool IsRunning
        {

            get { return _isRunning; }

        }
        public static bool Login(byte[] appkey, string username, string password)
        {

            postMessage(Session.Login, new object[] { appkey, username, password });

            _programSignal.WaitOne();

            if (Session.LoginError != libspotify.sp_error.OK)
            {

                Console.WriteLine("Login failed: {0}", libspotify.sp_error_message(Session.LoginError));
                return false;
            }

            return true;

        }

        public static void Initialize()
        {

            if (_initted)
                return;

            lock (_initSync)
            {

                try
                {

                    Session.OnNotifyMainThread += d_notify;
                    Session.OnLoggedIn += d_on_logged_in;

                    _programSignal = new AutoResetEvent(false);

                    _t = new Thread(new ThreadStart(mainThread));
                    _t.Start();

                    _programSignal.WaitOne();

                    Console.WriteLine("Main thread running...");

                    _initted = true;

                }
                catch
                {

                    Session.OnNotifyMainThread -= d_notify;
                    Session.OnLoggedIn -= d_on_logged_in;

                    if (_t != null)
                    {

                        try
                        {

                            _t.Abort();

                        }
                        catch { }
                        finally
                        {

                            _t = null;

                        }

                    }

                }

            }

        }

        public static int GetUserCountry()
        {

            return Session.GetUserCountry();

        }

        public static List<PlaylistContainer.PlaylistInfo> GetAllSessionPlaylists()
        {

            waitFor(delegate
            {
                return PlaylistContainer.GetSessionContainer().IsLoaded
                    && PlaylistContainer.GetSessionContainer().PlaylistsAreLoaded;
            }, REQUEST_TIMEOUT);

            return PlaylistContainer.GetSessionContainer().GetAllPlaylists();

        }

        public static List<PlaylistContainer.PlaylistInfo> GetPlaylists(PlaylistContainer.PlaylistInfo playlist)
        {

            waitFor(delegate
            {
                return PlaylistContainer.GetSessionContainer().IsLoaded
                    && PlaylistContainer.GetSessionContainer().PlaylistsAreLoaded;
            }, REQUEST_TIMEOUT);

            return PlaylistContainer.GetSessionContainer().GetChildren(playlist);

        }

        public static Playlist GetPlaylist(IntPtr playlistPtr, bool needTracks)
        {

            Playlist playlist = Playlist.Get(playlistPtr);

            if (playlist == null)
                return null;

            bool success = waitFor(delegate
            {
                return playlist.IsLoaded && needTracks ? playlist.TracksAreLoaded : true;
            }, REQUEST_TIMEOUT);

            return playlist;

        }

        public static Playlist GetInboxPlaylist()
        {

            IntPtr inboxPtr = IntPtr.Zero;

            try
            {

                inboxPtr = libspotify.sp_session_inbox_create(Session.GetSessionPtr());

                Playlist p = Playlist.Get(inboxPtr);

                bool success = waitFor(delegate
                {
                    return p.IsLoaded;
                }, REQUEST_TIMEOUT);

                return p;

            }
            finally
            {

                try
                {

                    if (inboxPtr != IntPtr.Zero)
                        libspotify.sp_playlist_release(inboxPtr);

                }
                catch { }

            }

        }

        public static Playlist GetStarredPlaylist()
        {

            IntPtr starredPtr = IntPtr.Zero;

            try
            {

                starredPtr = libspotify.sp_session_starred_create(Session.GetSessionPtr());

                Playlist p = Playlist.Get(starredPtr);

                bool success = waitFor(delegate
                {
                    return p.IsLoaded;
                }, REQUEST_TIMEOUT);

                return p;

            }
            finally
            {

                try
                {

                    if (starredPtr != IntPtr.Zero)
                        libspotify.sp_playlist_release(starredPtr);

                }
                catch { }

            }

        }

        public static TopList GetToplist(string data)
        {

            string[] parts = data.Split("|".ToCharArray());

            int region = parts[0].Equals("ForMe") ? (int)libspotify.sp_toplistregion.SP_TOPLIST_REGION_USER : parts[0].Equals("Everywhere") ? (int)libspotify.sp_toplistregion.SP_TOPLIST_REGION_EVERYWHERE : Convert.ToInt32(parts[0]);
            libspotify.sp_toplisttype type = parts[1].Equals("Artists") ? libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ARTISTS : parts[1].Equals("Albums") ? libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ALBUMS : libspotify.sp_toplisttype.SP_TOPLIST_TYPE_TRACKS;

            TopList toplist = TopList.BeginBrowse(type, region);

            bool success = waitFor(delegate
            {
                return toplist.IsLoaded;
            }, REQUEST_TIMEOUT);

            return toplist;

        }

        public static byte[] GetAlbumArt(IntPtr albumPtr)
        {

            IntPtr coverPtr = libspotify.sp_album_cover(albumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_LARGE);

            // NOTE: in API 10 sp_image_is_loaded() always returns true despite empty byte buffer, so using
            // callbacks now to determine when loaded.  Not sure how this will behave with cached images...

            using (Image img = Image.Load(libspotify.sp_image_create(Session.GetSessionPtr(), coverPtr)))
            {

                waitFor(delegate()
                {
                    return img.IsLoaded;
                }, REQUEST_TIMEOUT);

                int bytes = 0;
                IntPtr bufferPtr = libspotify.sp_image_data(img.ImagePtr, out bytes);
                byte[] buffer = new byte[bytes];
                Marshal.Copy(bufferPtr, buffer, 0, buffer.Length);

                Console.WriteLine("{0}, {1}", buffer.Length, libspotify.sp_image_is_loaded(img.ImagePtr));

                return buffer;
            }

        }

        public static IntPtr[] GetAlbumTracks(IntPtr albumPtr)
        {

            using (Album album = new Album(albumPtr))
            {

                if (!waitFor(delegate
                {
                    return libspotify.sp_album_is_loaded(album.AlbumPtr);
                }, REQUEST_TIMEOUT))
                    Console.WriteLine("GetAlbumTracks() TIMEOUT waiting for album to load");

                if (album.BeginBrowse())
                {

                    if (!waitFor(delegate()
                    {
                        return album.IsBrowseComplete;
                    }, REQUEST_TIMEOUT))
                        Console.WriteLine("GetAlbumTracks() TIMEOUT waiting for browse to complete");

                }

                if (album.TrackPtrs == null)
                    return null;

                return album.TrackPtrs.ToArray();

            }

        }

        public static IntPtr[] GetArtistAlbums(IntPtr artistPtr)
        {

            using (Artist artist = new Artist(artistPtr))
            {

                if (!waitFor(delegate
                {
                    return libspotify.sp_artist_is_loaded(artist.ArtistPtr);
                }, REQUEST_TIMEOUT))
                    Console.WriteLine("GetArtistAlbums() TIMEOUT waiting for artist to load");

                if (artist.BeginBrowse())
                {

                    if (!waitFor(delegate()
                    {
                        return artist.IsBrowseComplete;
                    }, REQUEST_TIMEOUT))
                        Console.WriteLine("GetArtistAlbums() TIMEOUT waiting for browse to complete");

                }

                if (artist.AlbumPtrs == null)
                    return null;

                return artist.AlbumPtrs.ToArray();

            }

        }

        public static PlaylistContainer GetUserPlaylists(IntPtr userPtr)
        {

            IntPtr ptr = IntPtr.Zero;

            try
            {

                ptr = libspotify.sp_session_publishedcontainer_for_user_create(Session.GetSessionPtr(), GetUserCanonicalNamePtr(userPtr));

                PlaylistContainer c = PlaylistContainer.Get(ptr);

                waitFor(delegate
                {
                    return c.IsLoaded
                        && c.PlaylistsAreLoaded;
                }, REQUEST_TIMEOUT);

                return c;

            }
            finally
            {

                //try {

                //    if (ptr != IntPtr.Zero)
                //        libspotify.sp_playlistcontainer_release(ptr);

                //} catch { }

            }

        }

        public static IntPtr GetUserCanonicalNamePtr(IntPtr userPtr)
        {

            waitFor(delegate()
            {
                return libspotify.sp_user_is_loaded(userPtr);
            }, REQUEST_TIMEOUT);

            return libspotify.sp_user_canonical_name(userPtr);

        }

        public static string GetUserDisplayName(IntPtr userPtr)
        {

            waitFor(delegate()
            {
                return libspotify.sp_user_is_loaded(userPtr);
            }, REQUEST_TIMEOUT);

            return Functions.PtrToString(libspotify.sp_user_full_name(userPtr));

        }

        public static User GetUser()
        {
            var userPtr = libspotify.sp_session_user(Session.GetSessionPtr());
            waitFor(delegate()
            {
                return libspotify.sp_user_is_loaded(userPtr);
            }, REQUEST_TIMEOUT);
            return new User(userPtr);
        }

        private static bool waitFor(Test t, int timeout)
        {

            DateTime start = DateTime.Now;

            while (DateTime.Now.Subtract(start).Seconds < timeout)
            {

                if (t.Invoke())
                {

                    return true;

                }

                Thread.Sleep(10);

            }

            return false;
        }

        public static void ShutDown()
        {

            lock (_syncObj)
            {

                libspotify.sp_session_player_unload(Session.GetSessionPtr());
                libspotify.sp_session_logout(Session.GetSessionPtr());

                try
                {

                    if (PlaylistContainer.GetSessionContainer() != null)
                    {

                        PlaylistContainer.GetSessionContainer().Dispose();

                    }

                }
                catch { }

                if (_mainSignal != null)
                    _mainSignal.Set();
                _shutDown = true;

            }

            _programSignal.WaitOne(2000, false);

        }

        private static void mainThread()
        {

            try
            {

                _mainSignal = new AutoResetEvent(false);

                int timeout = Timeout.Infinite;
                DateTime lastEvents = DateTime.MinValue;

                _isRunning = true;
                _programSignal.Set(); // this signals to program thread that loop is running   

                while (true)
                {

                    if (_shutDown)
                        break;

                    _mainSignal.WaitOne(timeout, false);

                    if (_shutDown)
                        break;

                    lock (_syncObj)
                    {

                        try
                        {

                            if (Session.GetSessionPtr() != IntPtr.Zero)
                            {

                                do
                                {

                                    libspotify.sp_session_process_events(Session.GetSessionPtr(), out timeout);

                                } while (timeout == 0);

                            }

                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine("Exception invoking sp_session_process_events", ex);

                        }

                        while (_mq.Count > 0)
                        {

                            MainThreadMessage m = _mq.Dequeue();
                            m.d.Invoke(m.payload);

                        }

                    }

                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("mainThread() unhandled exception", ex);

            }
            finally
            {

                _isRunning = false;
                if (_programSignal != null)
                    _programSignal.Set();

            }

        }

        public static void Session_OnLoggedIn(IntPtr obj)
        {

            _programSignal.Set();

        }

        public static void Session_OnNotifyMainThread(IntPtr sessionPtr)
        {

            _mainSignal.Set();

        }

        private static void postMessage(MainThreadMessageDelegate d, object[] payload)
        {

            _mq.Enqueue(new MainThreadMessage() { d = d, payload = payload });

            lock (_syncObj)
            {

                _mainSignal.Set();

            }

        }

    }

}