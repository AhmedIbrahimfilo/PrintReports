using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Reporting.NETCore;
using PdfiumViewer;
using Reports.Data;
using Reports.Entities;
using Syncfusion.XlsIO.Implementation.Security;
using System.Drawing;
using System.Drawing.Printing;


namespace Reports.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentReportsController : ControllerBase
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly AppDbContext _dbContext;

    public StudentReportsController(IWebHostEnvironment hostEnvironment, AppDbContext dbContext)
    {
        _hostEnvironment = hostEnvironment;
        _dbContext = dbContext;
    }



    [HttpGet]
    public async Task<ActionResult> StudentReport()
    {
        var reportPath = GetReportPath("Student.rdlc");
        var logoPath = GetLogoPath("Logo.png");

        var students = await GetStudentDataAsync();
        var pdf = GenerateReportPdf(reportPath, logoPath, students);
        PrintPdf(pdf);
        return Ok();
    }

    private string GetReportPath(string reportFileName)
    {
        return Path.Combine(_hostEnvironment.WebRootPath, "Reports", reportFileName);
    }

    private string GetLogoPath(string logoFileName)
    {
        return Path.Combine(_hostEnvironment.WebRootPath, "imgs", logoFileName);
    }

    private async Task<List<Student>> GetStudentDataAsync()
    {
        return await _dbContext.Students.ToListAsync();
    }

    private byte[] GenerateReportPdf(string reportPath, string logoPath, List<Student> students)
    {
        using var reportStream = new FileStream(reportPath, FileMode.Open);

        var dataSource = new ReportDataSource("studentDataSet", students);
        var reportViewer = new LocalReport();

        reportViewer.LoadReportDefinition(reportStream);
        reportViewer.DataSources.Add(dataSource);
        reportViewer.EnableExternalImages = true;

        var parameters = new List<ReportParameter>
        {
            new ReportParameter("ReportName", "Students Report"),
            new ReportParameter("Logo", $"File:{logoPath}")
        };

        reportViewer.SetParameters(parameters);

        string deviceInfo = @"
        <DeviceInfo>
            <OutputFormat>PDF</OutputFormat>
            <PageWidth>80mm</PageWidth>
            <PageHeight>297mm</PageHeight>
            <MarginTop>0in</MarginTop>
            <MarginLeft>0in</MarginLeft>
            <MarginRight>0in</MarginRight>
            <MarginBottom>0in</MarginBottom>
        </DeviceInfo>";


        return reportViewer.Render("PDF", deviceInfo);
    }

    private void PrintPdf(byte[] pdf)
    {
        using var memoryStream = new MemoryStream(pdf);
        using var pdfDocument = PdfiumViewer.PdfDocument.Load(memoryStream);

        var pd = new PrintDocument
        {
            PrinterSettings = { PrinterName = "Bullzip PDF Printer" }
        };

        // Set custom page size for width only (80mm), height will be determined by content
        int pageWidthMm = 80; // Width in mm
        int widthInHundredthsOfMM = pageWidthMm * 10; // Convert to hundredths of mm

        // Create a paper size with a fixed width but height as default
        pd.DefaultPageSettings.PaperSize = new PaperSize("Custom", widthInHundredthsOfMM, 0); // 0 indicates dynamic height

        // Optional: Set margins to 0 to utilize full page
        pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        int currentPage = 0;
        int totalPages = pdfDocument.PageCount;

        pd.PrintPage += (sender, e) =>
        {
            if (currentPage < totalPages)
            {
                // Calculate the scaling factor to fit the PDF within the specified width
                float scaleX = (float)e.MarginBounds.Width / pdfDocument.PageSizes[currentPage].Width;
                float scaleY = scaleX; // Assuming uniform scaling

                // Render the PDF page to the Graphics object with the calculated scaling
                var bounds = new Rectangle(0, 0, (int)(pdfDocument.PageSizes[currentPage].Width * scaleX),
                                            (int)(pdfDocument.PageSizes[currentPage].Height * scaleY));
                pdfDocument.Render(currentPage, e.Graphics, 300, 300, bounds, true);

                currentPage++;
                e.HasMorePages = currentPage < totalPages;
            }
            else
            {
                e.HasMorePages = false;
            }
        };

        // Print the document
        pd.Print();
    }




    [HttpGet("Printer")]
    public ActionResult<IEnumerable<string>> GetInstalledPrinters()
    {
        return Ok(PrinterSettings.InstalledPrinters.Cast<string>());
    }
}