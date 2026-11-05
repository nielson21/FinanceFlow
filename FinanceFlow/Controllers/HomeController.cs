using FinanceFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FinanceFlow.Controllers
{
    public class HomeController(ILogger<HomeController> logger) : Controller
    {
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature != null)
            {
                logger.LogError(exceptionFeature.Error, "Unhandled exception occurred while processing {Path}",
                    exceptionFeature.Path);
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize(Policy = "CanApproveLargeAmount")]
        public IActionResult LargeAmountTest()
        {
            return Content("You are allowed to approve large amounts.");
        }
    }
}
