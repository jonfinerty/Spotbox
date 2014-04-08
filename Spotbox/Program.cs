using System;
using System.Configuration;
using System.IO;
using Microsoft.Owin.Hosting;
using Spotbox.Player;
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

            StartServer();
        }

        private static void StartServer()
        {
            const string hostUri = "http://+:80/";

            using (WebApp.Start<Startup>(hostUri))
            {
                Console.WriteLine("Hosting Spotbox at: {0}", hostUri);

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
