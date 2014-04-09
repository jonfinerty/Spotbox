using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using libspotifydotnet;

using log4net;

namespace Spotbox.Player.Spotify
{
    public class Spotify
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Session session;

        private PlaylistContainer playlistContainer;

        private bool _shutDown;

        public Spotify(byte[] appkey, string username, string password)
        {
            session = new Session(appkey);

            new Task(StartMainSpotifyThread).Start();

            session.Login(username, password);
        }

        ~Spotify()
        {
            libspotify.sp_session_player_unload(session.SessionPtr);
            libspotify.sp_session_logout(session.SessionPtr);

            playlistContainer = null;

            _shutDown = true;
        }

        public IntPtr GetSessionPtr()
        {
            return session.SessionPtr;
        }

        public Session GetSession()
        {
            return session;
        }

        public void PlayDefaultPlaylist()
        {
            var playlistInfos = GetAllPlaylists();
            var playlistInfo = playlistInfos.FirstOrDefault(info => info.TrackCount > 0);
            if (playlistInfo == null)
            {
                _logger.WarnFormat("No playlists found with any tracks");
                return;
            }

            _logger.InfoFormat("Playing first playlist found");
            var playlist = new Playlist(playlistInfo.PlaylistPtr, session);
            currentPlaylist = playlist;
            playlist.Play();
        }

        public void PlayLastPlayingPlaylist()
        {
            var lastPlaylistName = Settings.Default.CurrentPlaylistName;
            if (lastPlaylistName != string.Empty)
            {
                var foundPlaylistInfo = GetPlaylistInfo(lastPlaylistName);
                if (foundPlaylistInfo != null)
                {
                    var lastPosition = Settings.Default.CurrentPlaylistPosition;

                    var playlist = foundPlaylistInfo.GetPlaylist();
                    currentPlaylist = playlist;
                    playlist.SetPlaylistPosition(lastPosition);
                    playlist.Play();
                    return;
                }
            }

            PlayDefaultPlaylist();
        }

        private PlaylistInfo GetPlaylistInfo(string playlistName)
        {
            var playlistInfos = GetAllPlaylists();
            var matchingPlaylistInfo = playlistInfos.FirstOrDefault(info => info.Name.ToLower() == playlistName.ToLower());
            return matchingPlaylistInfo;
        }

        public List<PlaylistInfo> GetAllPlaylists()
        {
            if (session.SessionPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("No valid session.");
            }

            if (playlistContainer == null)
            {
                playlistContainer = new PlaylistContainer(session);
                _logger.InfoFormat("Found {0} playlists", playlistContainer.PlaylistInfos.Count);
            }
            
            return playlistContainer.PlaylistInfos;
        }

        public Track SearchForTrack(string trackSearch)
        {
            searchCompleteDelegate = SearchComplete;
            var searchPtr = libspotify.sp_search_create(session.SessionPtr, trackSearch, 0, 1, 0, 0, 0, 0, 0, 0, sp_search_type.SP_SEARCH_STANDARD, Marshal.GetFunctionPointerForDelegate(searchCompleteDelegate), IntPtr.Zero);
            Wait.For(() => libspotify.sp_search_is_loaded(searchPtr) && libspotify.sp_search_error(searchPtr) == libspotify.sp_error.OK);

            if (libspotify.sp_search_num_tracks(searchPtr) > 0)
            {
                var track = new Track(libspotify.sp_search_track(searchPtr, 0), session);
                return track;
            }

            return null;
        }

        public Playlist GetCurrentPlaylist()
        {
            return currentPlaylist;
        }

        public bool SetPlaylist(string playlistName)
        {
            var playlistInfo = GetPlaylistInfo(playlistName);            
            if (playlistInfo != null)
            {
                var playlist = playlistInfo.GetPlaylist();
                playlist.Play();
                return true;
            }

            _logger.InfoFormat("No playlist found with name: {0}", playlistName);
            return false;
        }

        private void StartMainSpotifyThread()
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

        private delegate void SearchCompleteDelegate(IntPtr searchPtr, IntPtr userDataPtr);

        private SearchCompleteDelegate searchCompleteDelegate;

        private Playlist currentPlaylist;

        private static void SearchComplete(IntPtr searchPtr, IntPtr userDataPtr)
        {
        }
    }
}
