﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon.Models;
using Aiursoft.Pylon.Services;
using Aiursoft.WWW.Data;
using Aiursoft.WWW.Models;
using Microsoft.Extensions.Localization;

namespace Aiursoft.WWW.Controllers
{
    public class HomeController : Controller
    {
        public readonly SignInManager<WWWUser> _signInManager;
        public readonly ILogger _logger;
        private readonly ServiceLocation _serviceLocation;
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(
            SignInManager<WWWUser> signInManager,
            ILoggerFactory loggerFactory,
            ServiceLocation serviceLocation,
            IStringLocalizer<HomeController> localizer)
        {
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<HomeController>();
            _serviceLocation = serviceLocation;
            _localizer = localizer;
        }

        [AiurForceAuth("", "", justTry: true)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return this.SignoutRootServer(_serviceLocation.API, new AiurUrl(string.Empty, "Home", nameof(HomeController.Index), new { }));
        }
    }
}
