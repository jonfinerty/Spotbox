using Nancy;
using Spotbox.Player;

namespace Spotbox.Api
{
    public class Controls : NancyModule
    {
        public Controls()
        {
            Post["/play"] = x =>
            {
                Audio.Play();
                return HttpStatusCode.OK;
            };

            Post["/pause"] = x =>
            {
                Audio.Pause();
                return HttpStatusCode.OK;
            };

            Post["/next"] = x =>
            {
                Audio.Next();
                return HttpStatusCode.OK;
            };

            Post["/prev"] = x =>
            {
                Audio.Previous();
                return HttpStatusCode.OK;
            };
        }   
    }
}
