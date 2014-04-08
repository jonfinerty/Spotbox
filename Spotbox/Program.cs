using System;
using System.Configuration;
using System.IO;
using System.Reflection;

using Microsoft.Owin.Hosting;
using Spotbox.Player;
using Spotbox.Player.Spotify;

using log4net;

namespace Spotbox
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main()
        {
            var spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);
            var spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];
            var spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];

            Spotify.Login(spotifyApiKey, spotifyUsername, spotifyPassword);

            StartServer();
        }

        private static void StartServer()
        {
            const string hostUri = "http://+:80/";

            using (WebApp.Start<Startup>(hostUri))
            {
                _logger.InfoFormat("Hosting Spotbox at: {0}", hostUri);

                PlayLastPlaying();

                Console.ReadLine();
                
                Spotify.ShutDown();
            }
        }

        private static void PlayLastPlaying()
        {
            var lastPlaylistName = Settings.Default.CurrentPlaylistName;
            if (lastPlaylistName != "")
            {
                var found = Audio.SetPlaylist(lastPlaylistName);
                if (found)
                {
                    var lastPosition = Settings.Default.CurrentPlaylistPosition;

                    Audio.SetPlaylistPosition(lastPosition);
                    Audio.Play();
                }

                return;
            }
            Spotify.PlayDefaultPlaylist();
        }

    }
}
