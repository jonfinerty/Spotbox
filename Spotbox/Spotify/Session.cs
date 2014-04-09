using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using libspotifydotnet;
using log4net;
using Microsoft.AspNet.SignalR;
using NAudio.Wave;
using Newtonsoft.Json;

namespace Spotbox.Spotify 
{
    public class Session 
    {
        public IntPtr SessionPtr;

        const int _sampleRate = 44100;

        const int _channels = 2;

        private readonly HaltableBufferedWaveProvider _waveProvider;

        private WaveOutEvent _waveOutDevice;

        private readonly EventHandler<StoppedEventArgs> playbackStoppedHandler;

        private readonly WaveFormat waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, _sampleRate * _channels, 1, _sampleRate * 2 * _channels, _channels, 16);

        private EndOfTrackCallbackDelegate endOfTrackCallback;

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Session(byte[] appkey)
        {
            _waveOutDevice = new WaveOutEvent { DesiredLatency = 200 };

            playbackStoppedHandler = (sender, args) =>
            {
                _logger.InfoFormat("Track Playback Stopped: {0}", args.Exception);
                if (endOfTrackCallback != null)
                {
                    endOfTrackCallback();
                }
            };

            var callbacksPtr = AddCallbacks();

            var config = new libspotify.sp_session_config
            {
                api_version = libspotify.SPOTIFY_API_VERSION,
                user_agent = "Spotbox",
                application_key_size = appkey.Length,
                application_key = Marshal.AllocHGlobal(appkey.Length),
                cache_location = Path.Combine(Path.GetTempPath(), "spotify_api_temp"),
                settings_location = Path.Combine(Path.GetTempPath(), "spotify_api_temp"),
                callbacks = callbacksPtr,
                compress_playlists = true,
                dont_save_metadata_for_playlists = false,
                initially_unload_playlists = false
            };

            _logger.DebugFormat("api_version={0}", config.api_version);
            _logger.DebugFormat("application_key_size={0}", config.application_key_size);
            _logger.DebugFormat("cache_location={0}", config.cache_location);
            _logger.DebugFormat("settings_location={0}", config.settings_location);

            Marshal.Copy(appkey, 0, config.application_key, appkey.Length);

            IntPtr sessionPtr;
            var err = libspotify.sp_session_create(ref config, out sessionPtr);

            if (err != libspotify.sp_error.OK)
            {
                throw new ApplicationException(libspotify.sp_error_message(err));
            }

            SessionPtr = sessionPtr;
            libspotify.sp_session_set_connection_type(sessionPtr, libspotify.sp_connection_type.SP_CONNECTION_TYPE_WIRED);

            _waveProvider = new HaltableBufferedWaveProvider(waveFormat);
            _waveOutDevice.Init(_waveProvider);
            _waveOutDevice.PlaybackStopped += playbackStoppedHandler;

        }

        public delegate void EndOfTrackCallbackDelegate();

        public void Login(string username, string password)
        {
            _logger.InfoFormat("Logging into spotify with credentials");
            _logger.InfoFormat("Username: {0}", username);
            _logger.InfoFormat("Password: {0}", new string('*', password.Length));

            libspotify.sp_session_login(SessionPtr, username, password, false, null);

            Wait.For(() => libspotify.sp_session_connectionstate(SessionPtr) == libspotify.sp_connectionstate.LOGGED_IN);
        }

        public void Logout() 
        {
            libspotify.sp_session_logout(SessionPtr);
        }

        public void Unpause()
        {
            if (_waveOutDevice.PlaybackState == PlaybackState.Paused)
            {
                _waveOutDevice.Play();
            }            
        }

        public void Pause()
        {
            if (_waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                _waveOutDevice.Pause();
            }
        }

        public bool IsPlaying()
        {
            return _waveOutDevice.PlaybackState == PlaybackState.Playing;
        }

        public void Play(Track track, EndOfTrackCallbackDelegate endOfTrackCallbackDelegate)
        {
            _waveOutDevice.PlaybackStopped -= playbackStoppedHandler;

            var wasPlaying = _waveOutDevice.PlaybackState != PlaybackState.Paused;

            _waveOutDevice.Stop();
            _waveOutDevice.Dispose();

            endOfTrackCallback = endOfTrackCallbackDelegate;

            _logger.InfoFormat("Playing track: {0} - {1}", track.Name, string.Join(",", track.Artists));
            BroadcastTrackChange(track);

            _waveOutDevice = new WaveOutEvent {DesiredLatency = 200};
            _waveProvider.ClearBuffer();
            _waveProvider.SetBufferFinished(false);

            StartLoadingTrackAudio(track.TrackPtr);

            _waveOutDevice.Init(_waveProvider);

            if (wasPlaying)
            {
                _waveOutDevice.Play();
            }

            _waveOutDevice.PlaybackStopped += playbackStoppedHandler;
        }

        private void BroadcastTrackChange(Track track)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<PushHub>();
            hubContext.Clients.All.newTrack(JsonConvert.SerializeObject(track));
        }

        private void StartLoadingTrackAudio(IntPtr trackPtr)
        {
            libspotify.sp_session_player_play(SessionPtr, false);
            libspotify.sp_session_player_unload(SessionPtr);

            var avail = libspotify.sp_track_get_availability(SessionPtr, trackPtr);

            if (avail != libspotify.sp_availability.SP_TRACK_AVAILABILITY_AVAILABLE)
            {
                _logger.ErrorFormat("Track is unavailable ({0}).", avail);
                if (endOfTrackCallback != null)
                {
                    endOfTrackCallback();
                }

                return;
            }

            var error = libspotify.sp_session_player_load(SessionPtr, trackPtr);

            if (error != libspotify.sp_error.OK)
            {
                _logger.ErrorFormat("[Spotify] {0}", libspotify.sp_error_message(error));
                if (endOfTrackCallback != null)
                {
                    endOfTrackCallback();
                }

                return;
            }

            libspotify.sp_session_player_play(SessionPtr, true);
        }

        #region Callbacks

        private delegate void ConnectionErrorDelegate(IntPtr sessionPtr, libspotify.sp_error error);
        private delegate void EndOfTrackDelegate(IntPtr sessionPtr);
        private delegate void GetAudioBufferStatsDelegate(IntPtr sessionPtr, IntPtr statsPtr);
        private delegate void LogMessageDelegate(IntPtr sessionPtr, string message);
        private delegate void LoggedInDelegate(IntPtr sessionPtr, libspotify.sp_error error);
        private delegate void LoggedOutDelegate(IntPtr sessionPtr);
        private delegate void MessageToUserDelegate(IntPtr sessionPtr, string message);
        private delegate void MetadataUpdatedDelegate(IntPtr sessionPtr);
        private delegate int MusicDeliveryDelegate(IntPtr sessionPtr, IntPtr formatPtr, IntPtr framesPtr, int numberOfFrames);
        private delegate void NotifyMainThreadDelegate(IntPtr sessionPtr);
        private delegate void OfflineStatusUpdatedDelegate(IntPtr sessionPtr);
        private delegate void PlayTokenLostDelegate(IntPtr sessionPtr);
        private delegate void StartPlaybackDelegate(IntPtr sessionPtr);
        private delegate void StopPlaybackDelegate(IntPtr sessionPtr);
        private delegate void StreamingErrorDelegate(IntPtr sessionPtr, libspotify.sp_error error);
        private delegate void UserinfoUpdatedDelegate(IntPtr sessionPtr);

        private ConnectionErrorDelegate connectionErrorDelegate;
        private EndOfTrackDelegate endOfTrackDelegate;
        private GetAudioBufferStatsDelegate getAudioBufferStatsDelegate;
        private LogMessageDelegate logMessageDelegate;
        private LoggedInDelegate loggedInDelegate;
        private LoggedOutDelegate loggedOutDelegate;
        private MessageToUserDelegate messageToUserDelegate;
        private MetadataUpdatedDelegate metadataUpdatedDelegate;
        private MusicDeliveryDelegate musicDeliveryDelegate;
        private NotifyMainThreadDelegate notifyMainThreadDelegate;
        private OfflineStatusUpdatedDelegate offlineStatusUpdatedDelegate;
        private PlayTokenLostDelegate playTokenLostDelegate;
        private StartPlaybackDelegate startPlaybackDelegate;
        private StopPlaybackDelegate stopPlaybackDelegate;
        private StreamingErrorDelegate streamingErrorDelegate;
        private UserinfoUpdatedDelegate userinfoUpdatedDelegate;

        private IntPtr AddCallbacks()
        {
            connectionErrorDelegate = ConnectionError;
            endOfTrackDelegate = EndOfTrack;
            getAudioBufferStatsDelegate = GetAudioBufferStats;
            logMessageDelegate = LogMessage;
            loggedInDelegate = LoggedIn;
            loggedOutDelegate = LoggedOut;
            messageToUserDelegate = MessageToUser;
            metadataUpdatedDelegate = MetadataUpdated;
            musicDeliveryDelegate = MusicDelivery;
            notifyMainThreadDelegate = NotifyMainThread;
            offlineStatusUpdatedDelegate = OfflineStatusUpdated;
            playTokenLostDelegate = PlayTokenLost;
            startPlaybackDelegate = StartPlayback;
            stopPlaybackDelegate = StopPlayback;
            streamingErrorDelegate = StreamingError;
            userinfoUpdatedDelegate = UserinfoUpdated;

            var callbacks = new libspotify.sp_session_callbacks
                            {
                                connection_error = connectionErrorDelegate.GetFunctionPtr(),
                                end_of_track = endOfTrackDelegate.GetFunctionPtr(),
                                get_audio_buffer_stats = getAudioBufferStatsDelegate.GetFunctionPtr(),
                                log_message = logMessageDelegate.GetFunctionPtr(),
                                logged_in = loggedInDelegate.GetFunctionPtr(),
                                logged_out = loggedOutDelegate.GetFunctionPtr(),
                                message_to_user = messageToUserDelegate.GetFunctionPtr(),
                                metadata_updated = metadataUpdatedDelegate.GetFunctionPtr(),
                                music_delivery = musicDeliveryDelegate.GetFunctionPtr(),
                                notify_main_thread = notifyMainThreadDelegate.GetFunctionPtr(),
                                offline_status_updated = offlineStatusUpdatedDelegate.GetFunctionPtr(),
                                play_token_lost = playTokenLostDelegate.GetFunctionPtr(),
                                start_playback = startPlaybackDelegate.GetFunctionPtr(),
                                stop_playback = stopPlaybackDelegate.GetFunctionPtr(),
                                streaming_error = streamingErrorDelegate.GetFunctionPtr(),
                                userinfo_updated = userinfoUpdatedDelegate.GetFunctionPtr()
                            };

            var callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, callbacksPtr, true);

            return callbacksPtr;
        }

        private void ConnectionError(IntPtr sessionPtr, libspotify.sp_error error)
        {
            _logger.ErrorFormat("Connection error: {0}", libspotify.sp_error_message(error));
        }

        private void EndOfTrack(IntPtr sessionPtr)
        {
            _waveProvider.SetBufferFinished(true);
        }

        private void GetAudioBufferStats(IntPtr sessionPtr, IntPtr statsPtr)
        {
        }

        private void LogMessage(IntPtr sessionPtr, string message)
        {
            if (message.EndsWith("\n"))
            {
                message = message.Substring(0, message.Length - 1);
            }

            _logger.DebugFormat("Libspotify Message: {0}", message);
        }

        private void LoggedIn(IntPtr sessionPtr, libspotify.sp_error error)
        {   
        }

        private void LoggedOut(IntPtr sessionPtr)
        {
        }

        private void MessageToUser(IntPtr sessionPtr, string message)
        {
            _logger.DebugFormat("Message from Libspotify: {0}", message);
        }

        private void MetadataUpdated(IntPtr sessionPtr)
        {
            _logger.DebugFormat("Metadata Updated");
        }

        private int MusicDelivery(IntPtr sessionPtr, IntPtr formatPtr, IntPtr framesPtr, int numberOfFrames)
        {
            if (numberOfFrames == 0)
            {
                return 0;
            }

            var format = (libspotify.sp_audioformat)Marshal.PtrToStructure(formatPtr, typeof(libspotify.sp_audioformat));
            var bufferLength = numberOfFrames * sizeof(short) * format.channels;

            var byteBuffer = new byte[bufferLength];
            Marshal.Copy(framesPtr, byteBuffer, 0, byteBuffer.Length);

            _waveProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);

            return numberOfFrames;
        }

        private void NotifyMainThread(IntPtr sessionPtr)
        {
        }

        private void OfflineStatusUpdated(IntPtr sessionPtr)
        {
        }

        private void PlayTokenLost(IntPtr sessionPtr)
        {
            _logger.WarnFormat("Play Token Lost");
        }

        private void StartPlayback(IntPtr sessionPtr)
        {
            _logger.DebugFormat("Playback Started");
        }

        private void StopPlayback(IntPtr sessionPtr)
        {
            _logger.DebugFormat("Playback Stopped");
        }

        private void StreamingError(IntPtr sessionPtr, libspotify.sp_error error)
        {
            _logger.ErrorFormat("Streaming error: {0}", libspotify.sp_error_message(error));
        }

        private void UserinfoUpdated(IntPtr sessionPtr)
        {
            _logger.DebugFormat("Userinfo Updated");
        }

        #endregion
    }
}
