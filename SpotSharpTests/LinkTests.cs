using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpotSharp;

namespace SpotSharpTests
{
    [TestClass]
    public class LinkTests
    {
        [TestMethod]
        public void PlaylistFromLink()
        {
            const string playlistString = "spotify:user:bandrew:playlist:5TLIReJHInYXWEuDqt7Par";
            var playlistLink = Link.Create(playlistString);

            Assert.AreEqual(playlistLink.ToString(), playlistString, "Converting back into a string should work");
            Assert.AreEqual(playlistLink.LinkType, LinkType.Playlist, "Should be a playlist link");
        }
    }
}
