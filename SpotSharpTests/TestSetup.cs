using System.Configuration;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpotSharp;

namespace SpotSharpTests
{
    [TestClass]
    public class TestSetup
    {
        private static Spotify spotify;

        static readonly byte[] _spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);

        static readonly string _spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];

        static readonly string _spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];

        [AssemblyInitialize]        
        public static void StartupSpotSharp(TestContext context)
        {
            // Ok, so.. turns out libspotify has issues shutting down/restart in the same process.
            // This means in order to test SpotSharp we will start and login to one instance at
            // the start of testing.
            spotify = new Spotify(_spotifyApiKey);
        }

        public static Spotify LoggedInSpotbox()
        {
            if (spotify.LoggedIn == false)
            {
                spotify.Login(_spotifyUsername, _spotifyPassword);
            }

            return spotify;
        }

        public static Spotify LoggedOutSpotbox()
        {
            if (spotify.LoggedIn)
            {
                spotify.Logout();
            }

            return spotify;
        }
    }
}
