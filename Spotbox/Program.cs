using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using Nancy.Hosting.Self;
using Spotbox.Player.Libspotifydotnet;

namespace Spotbox
{
    class Program
    {
        static void Main(string[] args)
        {
            LoginToSpotify();
            Thread.Sleep(5000); // wait for playlists to be populated
            var hostConfiguration = new HostConfiguration
            {
                UrlReservations = new UrlReservations { CreateAutomatically = true }
            };

            var hostUri = new Uri("http://localhost:1234");
            using (var host = new NancyHost(hostConfiguration, hostUri))
            {
                Console.WriteLine("Hosting JukeApi at: {0}", hostUri);
                host.Start();
                
                SetDefaultPlaylist();

                Console.ReadLine();
            }
        }

        private static void SetDefaultPlaylist()
        {
            var playlistContainer = Player.Spotify.Spotify.GetSessionUserPlaylists();            
            Console.WriteLine("Found {0} playlists", playlistContainer.PlaylistInfos.Count);
            var playlistInfo = playlistContainer.PlaylistInfos.Where(info => info.Name.Length > 0).Skip(1).FirstOrDefault();
            Console.WriteLine(Session.IsLoggedIn);
            var playlist = Spotify.GetPlaylist(playlistInfo.PlaylistPtr, true);
            Player.Player.SetPlaylist(playlist);
        }

        private static void LoginToSpotify()
        {
            Spotify.Initialize();
            var spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);
            var spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];
            var spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];
            Console.WriteLine("Logging into spotify with credentials");
            Console.WriteLine("Username: {0}", spotifyUsername);
            Console.WriteLine("Password: {0}", new String('*', spotifyPassword.Length));
            Spotify.Login(spotifyApiKey, spotifyUsername, spotifyPassword);
        }
    }
}
