using System;
using System.Linq;
using System.Threading;
using libspotifydotnet;
using NAudio.Wave;
using Spotbox.Player.Spotify;
using Playlist = Spotbox.Player.Spotify.Playlist;
using Track = Spotbox.Player.Spotify.Track;

namespace Spotbox.Player
{
    static class Player
    {
        public static Track CurrentlyPlayingTrack { get; private set; }
        public static Playlist CurrentPlaylist { get; private set; }
        private static int playlistPosition = 0;
        private static HaltableBufferedWaveProvider _waveProvider;
        private static WaveOut _waveOutDevice;
        private static readonly EventHandler<StoppedEventArgs> PlaybackStoppedHandler = (sender, args) => Next();


        private static void InitialisePlayer()
        {            
            const int sampleRate = 44100;
            const int channels = 2;
            _waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, sampleRate * channels, 1, sampleRate * 2 * channels, channels, 16);        

            _waveOutDevice = new WaveOut { DesiredLatency = 200 };

            _waveProvider = new HaltableBufferedWaveProvider(_waveFormat)
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
            _waveOutDevice.Play();
        }

        public static void Pause()
        {            
            _waveOutDevice.Pause(); 
        }

        public static void Next()
        {
            playlistPosition++;
            var nextTrack = CurrentPlaylist.Tracks[playlistPosition];
            Play(nextTrack);
        }

        public static void Previous()
        {
            playlistPosition--;
            var prevTrack = CurrentPlaylist.Tracks[playlistPosition];
            Play(prevTrack);
        }

        public static void SetPlaylist(Playlist playlist)
        {
            CurrentPlaylist = playlist;
            Console.WriteLine("Playing playlist: {0}", playlist.PlaylistInfo.Name);
            playlistPosition = 0;
            Play(playlist.Tracks.First());
        }

        private static void Play(Track track)
        {
            Console.WriteLine("Playing track: {0} - {1}", track.Name, track.Artists.First());
            CurrentlyPlayingTrack = track;            
            _newTrack = true;
            Action<IntPtr> action = FetchTrackData;
            action.BeginInvoke(track.TrackPtr, null, null);            
        }

        private static bool _complete;

        private static bool _interrupt;

        private static object _syncObj = new object();        

        private static bool _newTrack = false;
        private static WaveFormat _waveFormat;

        private static void FetchTrackData(IntPtr trackPtr)
        {
            _interrupt = true;

            lock (_syncObj)
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
