using System;
using System.Linq;
using System.Threading;
using libspotifydotnet;
using Microsoft.AspNet.SignalR;
using NAudio.Wave;
using Newtonsoft.Json;
using Spotbox.Player.Spotify;

namespace Spotbox.Player
{
    static class Audio
    {
        public static Track CurrentlyPlayingTrack { get; private set; }
        public static Playlist CurrentPlaylist { get; private set; }
        private static int _playlistPosition;
        private static HaltableBufferedWaveProvider _waveProvider;
        private static WaveOut _waveOutDevice;
        private static readonly EventHandler<StoppedEventArgs> PlaybackStoppedHandler = (sender, args) =>
        {
            playingState = PlayingState.Stopped;
            if (_loop != true && _playlistPosition == CurrentPlaylist.PlaylistInfo.TrackCount - 1)
            {
                return;
            }

            Next();
            Play();
        };

        private static bool _complete;
        private static bool _interrupt;
        private static readonly object SyncObj = new object();
        private static bool _newTrack;

        const int SampleRate = 44100;
        const int Channels = 2;
        private static readonly WaveFormat WaveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, SampleRate * Channels, 1, SampleRate * 2 * Channels, Channels, 16);

        private enum PlayingState
        {
            Playing,
            Paused,
            Stopped
        };

        private static PlayingState playingState = PlayingState.Stopped;

        private static bool _loop = false;

        private static void InitialisePlayer()
        {
            if (_waveOutDevice != null)
            {
                _waveOutDevice.Dispose();
            }

            _waveOutDevice = new WaveOut { DesiredLatency = 200 };

            _waveProvider = new HaltableBufferedWaveProvider(WaveFormat)
            {
                BufferDuration = new TimeSpan(0, 0, CurrentlyPlayingTrack.Length),
                DiscardOnBufferOverflow = true
            };
            _waveOutDevice.Init(_waveProvider);
            
            _waveOutDevice.PlaybackStopped += PlaybackStoppedHandler;
        }        

        private static void AddBuffer(byte[] buffer)
        {
            if (_newTrack)
            {
                if (_waveOutDevice != null)
                {
                    _waveOutDevice.PlaybackStopped -= PlaybackStoppedHandler;
                    _waveOutDevice.Dispose();                    
                }                

                InitialisePlayer();
                _newTrack = false;
                Play();
            }

            _waveProvider.AddSamples(buffer, 0, buffer.Length);
        }

        public static void Play()
        {
            if (CurrentlyPlayingTrack == null)
            {
                Play(CurrentPlaylist.Tracks[_playlistPosition]);
            }
            else
            {
                _waveOutDevice.Play();
            }

            playingState = PlayingState.Playing;
        }

        public static void Pause()
        {
            if (_waveOutDevice != null && playingState == PlayingState.Playing)
            {
                _waveOutDevice.Pause();
                playingState = PlayingState.Paused;
            }
        }

        public static bool IsPlaying()
        {
            if (_waveOutDevice == null)
            {
                return false;
            }

            return _waveOutDevice.PlaybackState == PlaybackState.Playing;
        }

        public static void Next()
        {
            SetPlaylistPosition(_playlistPosition + 1);
        }

        public static void Previous()
        {
            SetPlaylistPosition(_playlistPosition - 1);
        }

        public static void SetPlaylist(Playlist playlist)
        {
            CurrentPlaylist = playlist;
            Console.WriteLine("Setting playlist: {0}", playlist.PlaylistInfo.Name);
            _playlistPosition = 0;
        }

        public static bool SetPlaylist(String playlistName)
        {
            var playlistInfos = Spotify.Spotify.GetAllPlaylists();
            var matchingPlaylistInfo = playlistInfos.FirstOrDefault(info => info.Name == playlistName);
            if (matchingPlaylistInfo != null)
            {
                SetPlaylist(matchingPlaylistInfo.GetPlaylist());
                return true;
            }

            Console.WriteLine("No playlist found with name: {0}", playlistName);
            return false;
        }

        public static void SetPlaylistPosition(int position)
        {
            var wasPlaying = IsPlaying();
            Pause();
            if (position < 0)
            {
                position = 0;
            } else if (position >= CurrentPlaylist.PlaylistInfo.TrackCount)
            {
                position = CurrentPlaylist.PlaylistInfo.TrackCount - 1;
            }
            CurrentlyPlayingTrack = null;
            _playlistPosition = position;
            Console.WriteLine("Setting playlist position: {0}", _playlistPosition);
            if (wasPlaying)
            {
                Play();
            }
        }

        private static void Play(Track track)
        {
            Console.WriteLine("Playing track: {0} - {1}", track.Name, Enumerable.First<string>(track.Artists));            
            CurrentlyPlayingTrack = track;
            SaveTrackToSettings();
            _newTrack = true;
            BroadcastTrack();
            Action<IntPtr> action = FetchTrackData;
            action.BeginInvoke(track.TrackPtr, null, null);            
        }

        private static void BroadcastTrack()
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<PushHub>();
            hubContext.Clients.All.newTrack(JsonConvert.SerializeObject(CurrentlyPlayingTrack));
        }

        private static void SaveTrackToSettings()
        {
            Settings.Default.CurrentPlaylistName = CurrentPlaylist.PlaylistInfo.Name;
            Settings.Default.CurrentPlaylistPosition = _playlistPosition;
            Settings.Default.Save();
        }

        private static void FetchTrackData(IntPtr trackPtr)
        {
            _interrupt = true;

            lock (SyncObj)
            {
                _interrupt = false;
                _complete = false;

                var avail = libspotify.sp_track_get_availability(Session.GetSessionPtr(), trackPtr);

                if (avail != libspotify.sp_availability.SP_TRACK_AVAILABILITY_AVAILABLE)
                {
                    Console.WriteLine((String.Format("Track is unavailable ({0}).", avail)));
                    return;                    
                }

                Session.OnAudioDataArrived += Session_OnAudioDataArrived;
                Session.OnAudioStreamComplete += Session_OnAudioStreamComplete;

                var error = Session.LoadPlayer(trackPtr);

                if (error != libspotify.sp_error.OK)
                {
                    throw new Exception(String.Format("[Spotify] {0}", libspotify.sp_error_message(error)));
                }

                Session.Play();

                while (!_interrupt && !_complete)
                {
                    Thread.Sleep(10);
                }                

                Session.OnAudioDataArrived -= Session_OnAudioDataArrived;
                Session.OnAudioStreamComplete -= Session_OnAudioStreamComplete;

                Session.Pause();
                Session.UnloadPlayer();
            }
        }

        private static void Session_OnAudioStreamComplete(object obj)
        {
            _waveProvider.SetBufferFinished();
            _complete = true;
        }

        private static void Session_OnAudioDataArrived(byte[] buffer)
        {
            if (!_interrupt && !_complete)
            {
                AddBuffer(buffer);
            }
        }
    }
}
