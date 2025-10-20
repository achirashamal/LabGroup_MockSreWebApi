using System;
using System.Threading;
using System.Web.Http;

namespace MockSreWebApi.Controllers
{
    public class WorkController : ApiController
    {
        [HttpGet]
        [Route("api/work")]
        public IHttpActionResult DoWork()
        {
            Console.WriteLine($"Work started at {DateTime.Now}");
            Thread.Sleep(new Random().Next(300, 1000)); // simulate work
            Console.WriteLine($"Work finished at {DateTime.Now}");
            return Ok(new { status = "Completed" });
        }
    }
}
