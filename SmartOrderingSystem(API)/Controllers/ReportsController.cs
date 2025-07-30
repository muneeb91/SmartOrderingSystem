using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using SmartOrderingSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly PdfService _pdfService;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly string ordersFilePath = Path.Combine("App_Data", "orders.json");

        public ReportsController(PdfService pdfService, ICompositeViewEngine viewEngine)
        {
            _pdfService = pdfService;
            _viewEngine = viewEngine;
        }

        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
            using var sw = new StringWriter();
            var viewResult = _viewEngine.FindView(actionContext, viewName, false);
            if (viewResult.View == null)
                throw new ArgumentNullException($"View \"{viewName}\" was not found.");

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                TempData,
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(DailySummaryReport));
        }

        public async Task<IActionResult> DailySummaryReport()
        {
            List<Order> orders = new List<Order>();

            string fullPath = Path.GetFullPath(ordersFilePath);
            Console.WriteLine("Looking for file at: " + fullPath);

            if (System.IO.File.Exists(ordersFilePath))
            {
                try
                {
                    var jsonData = await System.IO.File.ReadAllTextAsync(ordersFilePath);
                    Console.WriteLine("Raw JSON:\n" + jsonData);

                    orders = JsonConvert.DeserializeObject<List<Order>>(jsonData) ?? new List<Order>();
                    Console.WriteLine($"Parsed {orders.Count} orders from JSON.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading/parsing JSON: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("File not found at path: " + fullPath);
            }

            var reportModel = new DailySummaryReportModel
            {
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalPrice)
            };

            string html = await RenderViewToStringAsync("DailySummary", reportModel);
            byte[] pdf = _pdfService.GeneratePdfFromHtml(html);

            return File(pdf, "application/pdf", "DailySummaryReport.pdf");
        }
    }
}
