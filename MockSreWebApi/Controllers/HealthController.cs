using System.Web.Http;

namespace MockSreWebApi.Controllers
{
    public class HealthController : ApiController
    {
        [HttpGet]
        [Route("api/health")]
        public IHttpActionResult GetHealth() => Ok(new { status = "Healthy" });
    }
}
