using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using libspotifydotnet;

using log4net;

namespace Spotbox.Player.Spotify
{
    public class Spotify
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static AutoResetEvent _programSignal;
        private static AutoResetEvent _mainSignal;

        private static bool _shutDown;
        private static object _syncObj = new object();

        private static Thread _libSpotifyThread;

        public delegate void LibSpotifyThreadMessageDelegate(object[] args);

        public static bool Login(byte[] appkey, string username, string password)
        {
            session = new Session(appkey);

            _libSpotifyThread = new Thread(StartMainSpotifyThread);
            _libSpotifyThread.Start();

            _logger.InfoFormat("Logging into spotify with credentials");
            _logger.InfoFormat("Username: {0}", username);
            _logger.InfoFormat("Password: {0}", new string('*', password.Length));

            session.Login(username, password);

            return true;
        }

        public static void PlayDefaultPlaylist()
        {
            var playlistInfos = GetAllPlaylists();
            var playlistInfo = playlistInfos.Where(info => info.TrackCount > 0).Skip(1).FirstOrDefault();
            _logger.InfoFormat("Playing first playlist found");
            var playlist = new Playlist(playlistInfo.PlaylistPtr);

            Audio.SetPlaylist(playlist);
            Audio.Play();
        }

        public static List<PlaylistInfo> GetAllPlaylists()
        {
            if (session.SessionPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("No valid session.");
            }

            if (playlistContainer == null)
            {
                playlistContainer = new PlaylistContainer(libspotify.sp_session_playlistcontainer(session.SessionPtr));
                _logger.InfoFormat("Found {0} playlists", playlistContainer.PlaylistInfos.Count);
            }
            
            return playlistContainer.PlaylistInfos;
        }

        public static void ShutDown()
        {
            lock (_syncObj)
            {
                libspotify.sp_session_player_unload(session.SessionPtr);
                libspotify.sp_session_logout(session.SessionPtr);
    
                playlistContainer = null;

                if (_mainSignal != null)
                {
                    _mainSignal.Set();
                }

                _shutDown = true;
            }

            _programSignal.WaitOne(2000, false);
        }

        private static void StartMainSpotifyThread()
        {
            while (true)
            {
                if (_shutDown)
                {
                    break;
                }

                int timeout;
                libspotify.sp_session_process_events(session.SessionPtr, out timeout);

                timeout = Math.Min(100, timeout);

                Thread.Sleep(timeout);
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

        private delegate void SearchCompleteDelegate(IntPtr searchPtr, IntPtr userDataPtr);
        private static SearchCompleteDelegate searchCompleteDelegate;

        private static PlaylistContainer playlistContainer;

        private static Session session;

        public static Track SearchForTrack(string trackSearch)
        {
            searchCompleteDelegate = SearchComplete;
            var searchPtr = libspotify.sp_search_create(session.SessionPtr, trackSearch, 0, 1, 0, 0, 0, 0, 0, 0, sp_search_type.SP_SEARCH_STANDARD, Marshal.GetFunctionPointerForDelegate(searchCompleteDelegate), IntPtr.Zero);
            Wait.For(() => libspotify.sp_search_is_loaded(searchPtr) && libspotify.sp_search_error(searchPtr) == libspotify.sp_error.OK);
                
            if (libspotify.sp_search_num_tracks(searchPtr) > 0)
            {
                var track = new Track(libspotify.sp_search_track(searchPtr, 0));
                return track;
            }

            return null;
        }

        public static void SearchComplete(IntPtr searchPtr, IntPtr userDataPtr) {}

        public static IntPtr GetSessionPtr()
        {
            return session.SessionPtr;
        }

        public static Session GetSession()
        {
            return session;
        }
    }
}
