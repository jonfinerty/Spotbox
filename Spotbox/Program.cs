using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Nancy.Hosting.Self;
using Spotbox.Player.Spotify;

namespace Spotbox
{
    class Program
    {
        static void Main()
        {
            Spotify.Initialize();

            var spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);
            var spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];
            var spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];

            Spotify.Login(spotifyApiKey, spotifyUsername, spotifyPassword);

            var port = Convert.ToInt32(ConfigurationManager.AppSettings["PortNumber"]);

            StartServer(port);

        }

        private static void StartServer(int port)
        {
            var hostConfiguration = new HostConfiguration
            {
                UrlReservations = new UrlReservations { CreateAutomatically = true }
            };

            var hostUri = new Uri("http://localhost:" + port);
            using (var host = new NancyHost(hostConfiguration, hostUri))
            {
                Console.WriteLine("Hosting Spotbox at: {0}", hostUri);
                host.Start();

                PlayLastPlaying();

                Console.ReadLine();
                
                Spotify.ShutDown();
            }
        }

        private static void PlayLastPlaying()
        {
            var lastPlaylistName = Settings.Default.CurrentPlaylistName;
            if (lastPlaylistName != null)
            {
                var found = Player.Player.SetPlaylist(lastPlaylistName);
                if (found)
                {
                    var lastPosition = Settings.Default.CurrentPlaylistPosition;

                    Player.Player.SetPlaylistPosition(lastPosition);
                    Player.Player.Play();
                }

                return;
            }

            Spotify.PlayDefaultPlaylist();
        }
        
    }
}
