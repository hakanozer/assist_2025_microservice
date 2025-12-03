using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Order.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class OrderController : ControllerBase
    {

        [HttpGet]
        public ActionResult GetAll()
        {
            var jwt = Request.Headers["Authorization"].ToString();
            Console.WriteLine("Received JWT: " + jwt);
            return Ok("Order Service is running");
        }

    }
}