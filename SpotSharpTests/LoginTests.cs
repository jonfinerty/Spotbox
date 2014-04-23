using System.Configuration;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpotSharpTests
{
    [TestClass]
    public class LoginTests
    {
        [TestMethod]
        public void LoggingIn()
        {
            var spotify = TestSetup.LoggedOutSpotbox();
            spotify.Login(spotifyUsername, spotifyPassword);

            var loggedInUser = spotify.LoggedInUser;
            Assert.AreEqual(loggedInUser.CanonicalName, spotifyUsername, "Logged in user's canonical name should be equal the username logged in with");
            Assert.IsTrue(loggedInUser.DisplayName.Length > 0, "Logged in user should have a display name");
        }

        [TestMethod]
        public void LoggingOut()
        {
            var spotify = TestSetup.LoggedInSpotbox();
            spotify.Logout();

            var loggedInUser = spotify.LoggedInUser;

            Assert.IsNull(loggedInUser, "Should not have a logged in user");            
        }        

        readonly string spotifyUsername = ConfigurationManager.AppSettings["SpotifyUsername"];

        readonly string spotifyPassword = ConfigurationManager.AppSettings["SpotifyPassword"];
    }
}
