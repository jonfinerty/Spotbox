using System;
using System.Configuration;
using System.IO;
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

                Spotify.PlayDefaultPlaylist();

                Console.ReadLine();
            }
        }

        
    }
}
