using Nancy;

namespace Spotbox.Api
{
    public class Playlists : NancyModule
    {
        public Playlists()
        {
            Get["/playlists"] = x =>
            {
                return 200;
            };
        }
    }
}
