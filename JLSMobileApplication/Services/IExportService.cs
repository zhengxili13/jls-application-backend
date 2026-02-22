using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JLSApplicationBackend.Services;

public interface IExportService
{
    MemoryStream ExportExcel(List<dynamic> List, string ExportName);

    Task<string> ExportPdf(long OrderId, string Lang);
}