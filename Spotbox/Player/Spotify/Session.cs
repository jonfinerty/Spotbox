using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using libspotifydotnet;

using log4net;

namespace Spotbox.Player.Spotify 
{
    public class Session 
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IntPtr SessionPtr;      

        public static event Action<IntPtr> OnNotifyMainThread;        
        public static event Action<IntPtr> OnLoggedIn;
        public static event Action<byte[]> OnAudioDataArrived;
        public static event Action<object> OnAudioStreamComplete;

        public Session(byte[] appkey)
        {
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
        }

        public void Login(string username, string password)
        {
            libspotify.sp_session_login(SessionPtr, username, password, false, null);

            Wait.For(() => libspotify.sp_session_connectionstate(SessionPtr) == libspotify.sp_connectionstate.LOGGED_IN);
        }

        public void Logout() 
        {
            libspotify.sp_session_logout(SessionPtr);
        }
        
        public libspotify.sp_error LoadPlayer(IntPtr trackPtr) 
        {
            try
            {
                return libspotify.sp_session_player_load(SessionPtr, trackPtr);
            }
            catch
            {
                return libspotify.sp_error.OK;
            }
        }

        public void Play() 
        {
            libspotify.sp_session_player_play(SessionPtr, true);
        }

        public void Pause() 
        {
            libspotify.sp_session_player_play(SessionPtr, false);
        }

        public void UnloadPlayer() 
        {
            libspotify.sp_session_player_unload(SessionPtr);
        }

        private delegate void connection_error_delegate(IntPtr sessionPtr, libspotify.sp_error error);
        private delegate void end_of_track_delegate(IntPtr sessionPtr);
        private delegate void get_audio_buffer_stats_delegate(IntPtr sessionPtr, IntPtr statsPtr);
        private delegate void log_message_delegate(IntPtr sessionPtr, string message);
        private delegate void logged_in_delegate(IntPtr sessionPtr, libspotify.sp_error error);
        private delegate void logged_out_delegate(IntPtr sessionPtr);
        private delegate void message_to_user_delegate(IntPtr sessionPtr, string message);
        private delegate void metadata_updated_delegate(IntPtr sessionPtr);
        private delegate int music_delivery_delegate(IntPtr sessionPtr, IntPtr formatPtr, IntPtr framesPtr, int num_frames);
        private delegate void notify_main_thread_delegate(IntPtr sessionPtr);
        private delegate void offline_status_updated_delegate(IntPtr sessionPtr);
        private delegate void play_token_lost_delegate(IntPtr sessionPtr);
        private delegate void start_playback_delegate(IntPtr sessionPtr);
        private delegate void stop_playback_delegate(IntPtr sessionPtr);
        private delegate void streaming_error_delegate(IntPtr sessionPtr, libspotify.sp_error error);
        private delegate void userinfo_updated_delegate(IntPtr sessionPtr);

        private static connection_error_delegate fn_connection_error_delegate = ConnectionError;
        private static end_of_track_delegate fn_end_of_track_delegate = EndOfTrack;
        private static get_audio_buffer_stats_delegate fn_get_audio_buffer_stats_delegate = GetAudioBufferStats;
        private static log_message_delegate fn_log_message = LogMessage;
        private static logged_in_delegate fn_logged_in_delegate = LoggedIn;
        private static logged_out_delegate fn_logged_out_delegate = LoggedOut;
        private static message_to_user_delegate fn_message_to_user_delegate = MessageToUser;
        private static metadata_updated_delegate fn_metadata_updated_delegate = MetadataUpdated;
        private static music_delivery_delegate fn_music_delivery_delegate = MusicDelivery;
        private static notify_main_thread_delegate fn_notify_main_thread_delegate = NotifyMainThread;
        private static offline_status_updated_delegate fn_offline_status_updated_delegate = OfflineStatusUpdated;
        private static play_token_lost_delegate fn_play_token_lost_delegate = PlayTokenLost;
        private static start_playback_delegate fn_start_playback = StartPlayback;
        private static stop_playback_delegate fn_stop_playback = StopPlayback;
        private static streaming_error_delegate fn_streaming_error_delegate = StreamingError;
        private static userinfo_updated_delegate fn_userinfo_updated_delegate = UserinfoUpdated;

        private IntPtr AddCallbacks()
        {
            var callbacks = new libspotify.sp_session_callbacks();
            callbacks.connection_error = Marshal.GetFunctionPointerForDelegate(fn_connection_error_delegate);
            callbacks.end_of_track = Marshal.GetFunctionPointerForDelegate(fn_end_of_track_delegate);
            callbacks.get_audio_buffer_stats = Marshal.GetFunctionPointerForDelegate(fn_get_audio_buffer_stats_delegate);
            callbacks.log_message = Marshal.GetFunctionPointerForDelegate(fn_log_message);
            callbacks.logged_in = Marshal.GetFunctionPointerForDelegate(fn_logged_in_delegate);
            callbacks.logged_out = Marshal.GetFunctionPointerForDelegate(fn_logged_out_delegate);
            callbacks.message_to_user = Marshal.GetFunctionPointerForDelegate(fn_message_to_user_delegate);
            callbacks.metadata_updated = Marshal.GetFunctionPointerForDelegate(fn_metadata_updated_delegate);
            callbacks.music_delivery = Marshal.GetFunctionPointerForDelegate(fn_music_delivery_delegate);
            callbacks.notify_main_thread = Marshal.GetFunctionPointerForDelegate(fn_notify_main_thread_delegate);
            callbacks.offline_status_updated = Marshal.GetFunctionPointerForDelegate(fn_offline_status_updated_delegate);
            callbacks.play_token_lost = Marshal.GetFunctionPointerForDelegate(fn_play_token_lost_delegate);
            callbacks.start_playback = Marshal.GetFunctionPointerForDelegate(fn_start_playback);
            callbacks.stop_playback = Marshal.GetFunctionPointerForDelegate(fn_stop_playback);
            callbacks.streaming_error = Marshal.GetFunctionPointerForDelegate(fn_streaming_error_delegate);
            callbacks.userinfo_updated = Marshal.GetFunctionPointerForDelegate(fn_userinfo_updated_delegate);

            IntPtr callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, callbacksPtr, true);

            return callbacksPtr;
        }

        private static void ConnectionError(IntPtr sessionPtr, libspotify.sp_error error) 
        {
            _logger.ErrorFormat("Connection error: {0}", libspotify.sp_error_message(error));
        }

        private static void EndOfTrack(IntPtr sessionPtr) 
        {
            if (OnAudioStreamComplete != null)
            {
                OnAudioStreamComplete(null);
            }
        }

        private static void GetAudioBufferStats(IntPtr sessionPtr, IntPtr statsPtr) { }

        private static void LogMessage(IntPtr sessionPtr, string message) 
        {
            if (message.EndsWith("\n"))
            {
                message = message.Substring(0, message.Length - 1);
            }

            _logger.DebugFormat("Libspotify Message: {0}", message);

        }

        private static void LoggedIn(IntPtr sessionPtr, libspotify.sp_error error) 
        {
            if (OnLoggedIn != null)
            {
                OnLoggedIn(sessionPtr);
            }
        }

        private static void LoggedOut(IntPtr sessionPtr) { }

        private static void MessageToUser(IntPtr sessionPtr, string message) 
        {
            _logger.DebugFormat("Message from Libspotify: {0}", message);
        }

        private static void MetadataUpdated(IntPtr sessionPtr) 
        {
            _logger.DebugFormat("Metadata Updated");
        }

        private static int MusicDelivery(IntPtr sessionPtr, IntPtr formatPtr, IntPtr framesPtr, int numberOfFrames) 
        {
            if (numberOfFrames == 0)
            {
                return 0;
            }
                
            var format = (libspotify.sp_audioformat) Marshal.PtrToStructure(formatPtr, typeof(libspotify.sp_audioformat));
            var bufferLength = numberOfFrames * sizeof(short) * format.channels;

            var byteBuffer = new byte[bufferLength];
            Marshal.Copy(framesPtr, byteBuffer, 0, byteBuffer.Length);

            if (OnAudioDataArrived != null)
            {
                OnAudioDataArrived(byteBuffer);
            }

            return numberOfFrames;
        }        

        private static void NotifyMainThread(IntPtr sessionPtr)
        {
            if (OnNotifyMainThread != null && Spotify.GetSession() != null)
            {
                OnNotifyMainThread(Spotify.GetSessionPtr());
            }
        }

        private static void OfflineStatusUpdated(IntPtr sessionPtr) { }

        private static void PlayTokenLost(IntPtr sessionPtr)
        {
            _logger.WarnFormat("Play Token Lost");
        }

        private static void StartPlayback(IntPtr sessionPtr) 
        {
            _logger.DebugFormat("Start Playback");
        }

        private static void StopPlayback(IntPtr sessionPtr)
        {
            _logger.DebugFormat("Stop Playback");
        }

        private static void StreamingError(IntPtr sessionPtr, libspotify.sp_error error)
        {
            _logger.ErrorFormat( "Streaming error: {0}", libspotify.sp_error_message(error));
        }

        private static void UserinfoUpdated(IntPtr sessionPtr)
        {
            _logger.DebugFormat("Userinfo Updated");
        }

    }

}
