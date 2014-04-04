using Nancy;

namespace Spotbox.Api
{
    public class Controls : NancyModule
    {
        public Controls()
        {
            Post["/play"] = x =>
            {
                Player.Player.Play();
                return HttpStatusCode.OK;
            };

            Post["/pause"] = x =>
            {
                Player.Player.Pause();
                return HttpStatusCode.OK;
            };

            Post["/next"] = x =>
            {
                Player.Player.Next();
                return HttpStatusCode.OK;
            };

            Post["/prev"] = x =>
            {
                Player.Player.Previous();
                return HttpStatusCode.OK;
            };
        }   
    }
}
