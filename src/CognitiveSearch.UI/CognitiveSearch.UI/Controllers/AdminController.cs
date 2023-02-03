// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authorization;

namespace CognitiveSearch.UI.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly AppConfig _appConfig;

        public AdminController(IFileProvider fileProvider, IWebHostEnvironment hostingEnvironment, AppConfig appConfig)
        {
            _fileProvider = fileProvider;
            _hostingEnvironment = hostingEnvironment;
            _appConfig = appConfig;
        }

        [HttpGet]
        public IActionResult UploadData()
        {
            return View(_appConfig);
        }

        [HttpGet]
        public IActionResult Deploy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult NotAvailable()
        {
            return View();
        }
    }
}