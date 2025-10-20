using System;
using System.Threading;
using System.Web.Http;

namespace MockSreWebApi.Controllers
{
    public class ExternalController : ApiController
    {
        [HttpGet]
        [Route("api/external")]
        public IHttpActionResult GetExternal()
        {
            var random = new Random();
            Thread.Sleep(random.Next(1000, 2000)); // simulate network delay
            if (random.NextDouble() < 0.2)
                return InternalServerError(new Exception("Simulated external service failure."));
            return Ok(new { message = "External API call successful." });
        }
    }
}
