using Nancy;

namespace Spotbox.Api
{
    public class Playlist : NancyModule
    {
        public Playlist()
        {
            Get["/playlist"] = x =>
            {
                return 200;
            };

            Post["/playlist"] = x =>
            {
                return 200;
            };

            Put["/playlist"] = x =>
            {
                return 200;
            };
        }
    }
}
