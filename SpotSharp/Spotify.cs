﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using libspotifydotnet;
using log4net;

namespace SpotSharp
{
    public class Spotify
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Session session;

        private PlaylistContainer playlistContainer;

        private bool _shutDown;

        public Spotify(byte[] appkey, string username, string password)
        {
            session = new Session(this, appkey);

            new Task(StartMainSpotifyThread).Start();

            session.Login(username, password);
            
            playlistContainer = new PlaylistContainer(session);
            _logger.InfoFormat("Found {0} playlists", playlistContainer.PlaylistInfos.Count);
        }

        ~Spotify()
        {
            libspotify.sp_session_player_unload(session.SessionPtr);
            libspotify.sp_session_logout(session.SessionPtr);

            playlistContainer = null;

            _shutDown = true;
        }

        public TrackChangedDelegate TrackChanged;

        public delegate void TrackChangedDelegate(Track newTrack);

        public Playlist.PlaylistChangedDelegate PlaylistChanged;

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
            SetCurrentPlaylist(playlistInfo.Link);
            Play();
        }

        public PlaylistInfo GetPlaylistInfo(Link playlistLink)
        {
            return playlistContainer.PlaylistInfos.FirstOrDefault(playlistInfo => playlistInfo.Link.Equals(playlistLink));
        }

        public List<PlaylistInfo> GetAllPlaylists()
        {
            return playlistContainer.PlaylistInfos;
        }

        public List<Track> SearchForTracks(string searchTerms)
        {
            var tracksFound = new List<Track>();
            searchCompleteDelegate = SearchComplete;
            var searchPtr = libspotify.sp_search_create(session.SessionPtr, searchTerms, 0, 10, 0, 0, 0, 0, 0, 0, sp_search_type.SP_SEARCH_STANDARD, Marshal.GetFunctionPointerForDelegate(searchCompleteDelegate), IntPtr.Zero);
            Wait.For(() => libspotify.sp_search_is_loaded(searchPtr) && libspotify.sp_search_error(searchPtr) == libspotify.sp_error.OK);

            var tracksFoundCount = libspotify.sp_search_num_tracks(searchPtr);
            for (var i = 0; i < tracksFoundCount; i++)
            {
                var track = new Track(libspotify.sp_search_track(searchPtr, i), session);
                tracksFound.Add(track);
            }

            return tracksFound;
        }

        public Playlist GetCurrentPlaylist()
        {
            return currentPlaylist;
        }

        public bool SetCurrentPlaylist(Link playlistLink)
        {
            var playlistInfo = GetPlaylistInfo(playlistLink);
            if (playlistInfo != null)
            {
                libspotify.sp_playlistcontainer_add_playlist(playlistContainer.PlaylistContainerPtr, playlistLink.LinkPtr);
                Wait.For(() =>
                {
                    playlistInfo = GetPlaylistInfo(playlistLink);
                    return playlistInfo != null;
                });
                
            }

            if (playlistInfo == null)
            {
                return false;
            }

            if (currentPlaylist != null)
            {
                currentPlaylist.playlistChanged = null;
            }

            currentPlaylist = playlistInfo.GetPlaylist();
            _logger.InfoFormat("Current playlist set to: {0}", currentPlaylist.Metadata.Name);
            currentPlaylist.playlistChanged = PlaylistChanged;
            return true;
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

        public void Play()
        {
            if (currentPlaylist != null)
            {
                currentPlaylist.Play();
            }
        }

        public void Unpause()
        {
            session.Unpause();
        }

        public void Pause()
        {
            session.Pause();
        }

        public void SetCurrentPlaylistPosition(int lastPosition)
        {
            if (currentPlaylist != null)
            {
                currentPlaylist.CurrentPosition = lastPosition;
            }
        }

        public void PlayNextTrack()
        {
            if (currentPlaylist != null)
            {
                currentPlaylist.PlayNextTrack();
            }
        }

        public void PlayPreviousTrack()
        {
            if (currentPlaylist != null)
            {
                currentPlaylist.PlayPreviousTrack();
            }
        }

        public Track GetCurrentTrack()
        {
            if (currentPlaylist != null)
            {
                return currentPlaylist.GetCurrentTrack();
            }
            return null;
        }
    }
}
