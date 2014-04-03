using Nancy;
using Newtonsoft.Json;

namespace JukeApi.api
{
    public class Current : NancyModule
    {
        public Current()
        {
            Get["/current"] = x =>
            {
                var track = Player.CurrentlyPlayingTrack;
                var response = (Response) JsonConvert.SerializeObject(track);
                response.ContentType = "application/json";
                return response;
            };

            Post["/current/play"] = x =>
            {
                Player.Play();
                return HttpStatusCode.OK;
            };

            Post["/current/pause"] = x =>
            {
                Player.Pause();
                return HttpStatusCode.OK;
            };

            Post["/current/next"] = x =>
            {
                Player.Next();
                return HttpStatusCode.OK;
            };

            Post["/current/prev"] = x =>
            {
                Player.Previous();
                return HttpStatusCode.OK;
            };
        }   
    }
}
