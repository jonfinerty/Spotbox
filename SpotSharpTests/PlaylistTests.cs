using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpotSharp;

namespace SpotSharpTests
{
    [TestClass]
    public class PlaylistTests
    {
        [TestMethod]
        public void AllPlaylistInfos()
        {
            var playlistInfos = spotSharp.GetAllPlaylists();

            Assert.IsTrue(playlistInfos.Any(), "Should have atleast one playlist");
        }

        [TestMethod]
        public void SetCurrentPlaylist()
        {
            var currentPlaylist = spotSharp.GetCurrentPlaylist();
            Assert.IsNull(currentPlaylist, "Should not have a current playlist");

            var playlistChangedDelegateCalled = false;

            Playlist.PlaylistChangedDelegate playlistChangedDelegate = delegate { playlistChangedDelegateCalled = true; };

            spotSharp.PlaylistChanged = playlistChangedDelegate;

            var playlist = spotSharp.GetAllPlaylists().FirstOrDefault();
            spotSharp.SetCurrentPlaylist(playlist.Link);

            var newCurrentPlaylist = spotSharp.GetCurrentPlaylist();

            Assert.AreEqual(newCurrentPlaylist, playlist, "Current playlist should be set");
            Assert.AreEqual(newCurrentPlaylist.CurrentPosition, 0, "Current playlist should be set to position 0");
            Assert.IsTrue(playlistChangedDelegateCalled, "Should have called the playlist changed delegate");
        }

        readonly SpotSharp.SpotSharp spotSharp = TestSetup.LoggedInSpotbox();
    }
}
