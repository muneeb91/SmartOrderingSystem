using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("daily-summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDailySummary()
        {
            try
            {
                var report = await _reportService.GetDailySummaryAsync(DateTime.UtcNow);
                Console.WriteLine($"GET /api/Report/daily-summary Success: {JsonSerializer.Serialize(report)}");
                return Ok(report);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Failed to fetch daily summary: {ex.Message}", innerException = ex.InnerException?.Message };
                Console.WriteLine($"GET /api/Report/daily-summary Error: {JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("daily-summary/pdf")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadDailySummaryPdf()
        {
            try
            {
                var pdfBytes = await _reportService.GenerateDailySummaryPdfAsync(DateTime.UtcNow);
                Console.WriteLine($"GET /api/Report/daily-summary/pdf Success: PDF size={pdfBytes.Length} bytes");
                return File(pdfBytes, "application/pdf", "daily-summary.pdf");
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Failed to generate daily summary PDF: {ex.Message}", innerException = ex.InnerException?.Message };
                Console.WriteLine($"GET /api/Report/daily-summary/pdf Error: {JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("order-invoice/{orderId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadOrderInvoicePdf(int orderId)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateOrderInvoicePdfAsync(orderId);
                Console.WriteLine($"GET /api/Report/order-invoice/{orderId} Success: PDF size={pdfBytes.Length} bytes");
                return File(pdfBytes, "application/pdf", $"order-{orderId}.pdf");
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Failed to generate invoice PDF: {ex.Message}", innerException = ex.InnerException?.Message };
                Console.WriteLine($"GET /api/Report/order-invoice/{orderId} Error: {JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("test-pdf")]
        public IActionResult TestPdf()
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new PdfWriter(stream))
                    {
                        using (var pdf = new PdfDocument(writer))
                        {
                            var document = new Document(pdf);
                            PdfFont font;
                            try
                            {
                                font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                                PdfFontFactory.RegisterSystemDirectories();
                            }
                            catch (Exception fontEx)
                            {
                                Console.WriteLine($"TestPdf Font Error: Message={fontEx.Message}, StackTrace={fontEx.StackTrace}");
                                throw new Exception("Failed to load Helvetica font.", fontEx);
                            }
                            document.Add(new Paragraph("Test PDF").SetFontSize(16).SetFont(font));
                            document.Close();
                        }
                    }
                    var result = stream.ToArray();
                    Console.WriteLine($"GET /api/Report/test-pdf Success: PDF size={result.Length} bytes");
                    return File(result, "application/pdf", "test.pdf");
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Test PDF failed: {ex.Message}", innerException = ex.InnerException?.Message };
                Console.WriteLine($"GET /api/Report/test-pdf Error: {JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }
    }
}