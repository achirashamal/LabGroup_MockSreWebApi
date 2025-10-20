using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;

namespace MockSreWebApi.Controllers
{
    public class DataController : ApiController
    {
        [HttpGet]
        [Route("api/data")]
        public IHttpActionResult GetData()
        {
            Thread.Sleep(new Random().Next(100, 500)); // simulate delay
            var data = new List<string> { "Item1", "Item2", "Item3" };
            return Ok(data);
        }
    }
}
