using System.Configuration;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpotSharpTests
{
    [TestClass]
    public class TestSetup
    {
        private static SpotSharp.SpotSharp spotSharp;

        static readonly byte[] _spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);

        static readonly string _spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];

        static readonly string _spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];

        [AssemblyInitialize]        
        public static void StartupSpotSharp(TestContext context)
        {
            // Ok, so.. turns out libspotify has issues shutting down/restart in the same process.
            // This means in order to test SpotSharp we will start and login to one instance at
            // the start of testing.
            spotSharp = new SpotSharp.SpotSharp(_spotifyApiKey);
        }

        [AssemblyCleanup]
        public static void FlushSpotSharp()
        {
            if (spotSharp != null)
            {
                spotSharp.Logout();
            }

            // sigh, playlist don't seem to want to release
            /*spotSharp.Dispose();*/
        }

        public static SpotSharp.SpotSharp LoggedInSpotbox()
        {
            if (spotSharp.LoggedIn == false)
            {
                spotSharp.Login(_spotifyUsername, _spotifyPassword);
            }

            return spotSharp;
        }

        public static SpotSharp.SpotSharp LoggedOutSpotbox()
        {
            if (spotSharp.LoggedIn)
            {
                spotSharp.Logout();
            }

            return spotSharp;
        }
    }
}
