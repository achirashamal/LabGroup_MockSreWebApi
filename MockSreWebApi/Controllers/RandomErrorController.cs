using System;
using System.Web.Http;

namespace MockSreWebApi.Controllers
{
    public class RandomErrorController : ApiController
    {
        [HttpGet]
        [Route("api/random-error")]
        public IHttpActionResult GetRandomError()
        {
            if (new Random().NextDouble() < 0.3)
                throw new Exception("Simulated random error.");
            return Ok(new { result = "Success" });
        }
    }
}
