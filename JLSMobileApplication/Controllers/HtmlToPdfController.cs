using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace JLSApplicationBackend.Controllers;

/* Todo remove: migration to export controller, please teste */
[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class HtmlToPdfController : Controller
{
    private readonly IExportService _exportService;

    public HtmlToPdfController(IExportService exportService)
    {
        _exportService = exportService;
    }

    public async Task<ActionResult> ExportPdf()
    {
        // Use the centralized export service
        var filePath = await _exportService.ExportPdf(26, "fr");

        /* Download file function */
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "application/pdf", "Invoice.pdf");
    }
}