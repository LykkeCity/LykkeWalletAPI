using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LykkeApi2.Controllers
{
    [Route("")]
    public class SwaggerController : Controller
    {

        [HttpGet()]
        public IActionResult Get()
        {
            return Redirect("/swagger/ui");
        }
    }
}
