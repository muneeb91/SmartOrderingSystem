using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace SmartOrderingSystem.Controllers
{
    public class AuthController : Controller
    {

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(string username, string password)
        {

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username and Password are required.";
                return View();
            }


            string role = username.ToLower() == "admin" ? "Admin" : "User";


            string token = DemoJwt.Generate(username, role);


            ViewBag.Token = token;
            ViewBag.Role = role;
            ViewBag.Username = username;

            return View("LoginSuccess"); 
        }
    }
}
