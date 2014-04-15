using System.Configuration;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpotSharp;

namespace SpotSharpTests
{
    [TestClass]
    public class LoginTests
    {
        [TestMethod]
        public void SuccessfulLogin()
        {
            using (var spotify = new Spotify(spotifyApiKey))
            {
                spotify.Login(spotifyUsername, spotifyPassword);

                var loggedInUser = spotify.LoggedInUser;
                Assert.AreEqual(loggedInUser.CanonicalName, spotifyUsername, "Logged in user's canonical name should be equal the username logged in with");
                Assert.IsTrue(loggedInUser.DisplayName.Length > 0, "Logged in user should have a display name");
            }
        }

        [TestMethod]
        public void IncorrectPassword()
        {
            using (var spotify = new Spotify(spotifyApiKey))
            {
                var loggedIn = spotify.Login(spotifyUsername, "wrong-password");

                Assert.IsFalse(loggedIn, "Should not have logged in");
                Assert.IsNull(spotify.LoggedInUser, "should not have a logged in user");
            }
        }

        byte[] spotifyApiKey = File.ReadAllBytes(ConfigurationManager.AppSettings["SpotifyApiKeyPath"]);
        string spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];
        string spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];
    }
}
