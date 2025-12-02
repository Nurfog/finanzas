using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Services;

public class ExcelReportService
{
    private readonly FinancialDbContext _context;
    private readonly ILogger<ExcelReportService> _logger;

    public ExcelReportService(FinancialDbContext context, ILogger<ExcelReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerateRevenueReport(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddMonths(-6);
        endDate ??= DateTime.Now;

        var transactions = await _context.Transactions
            .Include(t => t.Customer)
            .Include(t => t.Location)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.Status == "Completed")
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        // Sheet 1: Raw Data
        CreateDataSheet(workbook, transactions);

        // Sheet 2: Analysis with Charts
        CreateAnalysisSheet(workbook, transactions);

        // Sheet 3: Insights
        await CreateInsightsSheet(workbook, transactions, startDate.Value, endDate.Value);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateDataSheet(XLWorkbook workbook, List<Transaction> transactions)
    {
        var worksheet = workbook.Worksheets.Add("Datos");

        // Headers
        worksheet.Cell(1, 1).Value = "Fecha";
        worksheet.Cell(1, 2).Value = "Cliente";
        worksheet.Cell(1, 3).Value = "Sede";
        worksheet.Cell(1, 4).Value = "Monto (CLP)";
        worksheet.Cell(1, 5).Value = "Tipo";
        worksheet.Cell(1, 6).Value = "Método de Pago";
        worksheet.Cell(1, 7).Value = "Mes";
        worksheet.Cell(1, 8).Value = "Año";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data
        int row = 2;
        foreach (var transaction in transactions)
        {
            worksheet.Cell(row, 1).Value = transaction.TransactionDate;
            worksheet.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            worksheet.Cell(row, 2).Value = transaction.Customer?.Name ?? "N/A";
            worksheet.Cell(row, 3).Value = transaction.Location?.Name ?? "Sin Sede";
            worksheet.Cell(row, 4).Value = transaction.Amount;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 5).Value = transaction.TransactionType;
            worksheet.Cell(row, 6).Value = transaction.PaymentMethod;
            worksheet.Cell(row, 7).Value = transaction.TransactionDate.Month;
            worksheet.Cell(row, 8).Value = transaction.TransactionDate.Year;
            row++;
        }

        // Create Table
        if (transactions.Any())
        {
            var dataRange = worksheet.Range(1, 1, row - 1, 8);
            var table = dataRange.CreateTable("TransactionsTable");
            table.Theme = XLTableTheme.TableStyleMedium2;
        }

        worksheet.Columns().AdjustToContents();
    }

    private void CreateAnalysisSheet(XLWorkbook workbook, List<Transaction> transactions)
    {
        var worksheet = workbook.Worksheets.Add("Análisis");

        // Title
        worksheet.Cell(1, 1).Value = "📊 Análisis de Ingresos";
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 4).Merge();

        // Monthly Revenue Summary
        var monthlyData = transactions
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(t => (long)t.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        int startRow = 3;
        worksheet.Cell(startRow, 1).Value = "Período";
        worksheet.Cell(startRow, 2).Value = "Ingresos (CLP)";
        worksheet.Cell(startRow, 3).Value = "# Transacciones";

        var headerRange = worksheet.Range(startRow, 1, startRow, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");

        int dataRow = startRow + 1;
        foreach (var data in monthlyData)
        {
            worksheet.Cell(dataRow, 1).Value = data.Period;
            worksheet.Cell(dataRow, 2).Value = data.Revenue;
            worksheet.Cell(dataRow, 2).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(dataRow, 3).Value = data.Count;
            dataRow++;
        }

        if (monthlyData.Any())
        {
            var monthlyTable = worksheet.Range(startRow, 1, dataRow - 1, 3).CreateTable("MonthlyRevenue");
            monthlyTable.Theme = XLTableTheme.TableStyleLight9;

            // Generate line chart with ScottPlot
            var lineChart = new ScottPlot.Plot();
            lineChart.Title("Tendencia de Ingresos Mensuales");
            lineChart.XLabel("Período");
            lineChart.YLabel("Ingresos (CLP)");
            
            var periods = monthlyData.Select(d => d.Period).ToArray();
            var revenues = monthlyData.Select(d => (double)d.Revenue).ToArray();
            var positions = Enumerable.Range(0, periods.Length).Select(i => (double)i).ToArray();
            
            var scatter = lineChart.Add.Scatter(positions, revenues);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 8;
            scatter.Color = ScottPlot.Color.FromHex("#4472C4");
            
            lineChart.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                positions.Select((p, i) => new ScottPlot.Tick(p, periods[i])).ToArray()
            );
            lineChart.Axes.Bottom.TickLabelStyle.Rotation = 45;
            lineChart.Axes.Bottom.TickLabelStyle.Alignment = ScottPlot.Alignment.MiddleRight;
            
            // Save chart as image
            var chartPath = Path.Combine(Path.GetTempPath(), $"revenue_chart_{Guid.NewGuid()}.png");
            lineChart.SavePng(chartPath, 600, 400);
            
            // Insert image into Excel
            var picture = worksheet.AddPicture(chartPath);
            picture.MoveTo(worksheet.Cell(startRow, 5));
            picture.Scale(0.8);
            
            // Clean up temp file
            try { File.Delete(chartPath); } catch { }
        }

        // Payment Methods Summary
        var paymentData = transactions
            .GroupBy(t => t.PaymentMethod)
            .Select(g => new { Method = g.Key, Total = g.Sum(t => (long)t.Amount), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        int paymentRow = dataRow + 3;
        worksheet.Cell(paymentRow, 1).Value = "Método de Pago";
        worksheet.Cell(paymentRow, 2).Value = "Total (CLP)";
        worksheet.Cell(paymentRow, 3).Value = "# Transacciones";

        var paymentHeaderRange = worksheet.Range(paymentRow, 1, paymentRow, 3);
        paymentHeaderRange.Style.Font.Bold = true;
        paymentHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");

        int paymentDataRow = paymentRow + 1;
        foreach (var data in paymentData)
        {
            worksheet.Cell(paymentDataRow, 1).Value = data.Method;
            worksheet.Cell(paymentDataRow, 2).Value = data.Total;
            worksheet.Cell(paymentDataRow, 2).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(paymentDataRow, 3).Value = data.Count;
            paymentDataRow++;
        }

        if (paymentData.Any())
        {
            var paymentTable = worksheet.Range(paymentRow, 1, paymentDataRow - 1, 3).CreateTable("PaymentMethods");
            paymentTable.Theme = XLTableTheme.TableStyleLight9;

            // Generate pie chart with ScottPlot
            var pieChart = new ScottPlot.Plot();
            pieChart.Title("Distribución por Método de Pago");
            
            var pie = pieChart.Add.Pie(paymentData.Select(d => (double)d.Total).ToArray());
            for (int i = 0; i < paymentData.Count; i++)
            {
                pie.Slices[i].Label = paymentData[i].Method;
            }
            
            // Save chart as image
            var pieChartPath = Path.Combine(Path.GetTempPath(), $"payment_chart_{Guid.NewGuid()}.png");
            pieChart.SavePng(pieChartPath, 500, 350);
            
            // Insert image into Excel
            var piePicture = worksheet.AddPicture(pieChartPath);
            piePicture.MoveTo(worksheet.Cell(paymentRow, 5));
            piePicture.Scale(0.8);
            
            // Clean up temp file
            try { File.Delete(pieChartPath); } catch { }
        }

        worksheet.Columns().AdjustToContents();
    }

    private async Task CreateInsightsSheet(XLWorkbook workbook, List<Transaction> transactions, DateTime startDate, DateTime endDate)
    {
        var worksheet = workbook.Worksheets.Add("Insights");

        // Title
        worksheet.Cell(1, 1).Value = "📊 Análisis Financiero - Insights Clave";
        worksheet.Cell(1, 1).Style.Font.FontSize = 18;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#4472C4");
        worksheet.Range(1, 1, 1, 2).Merge();

        worksheet.Cell(2, 1).Value = $"Período: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        worksheet.Range(2, 1, 2, 2).Merge();

        int row = 4;

        // KPIs Section
        worksheet.Cell(row, 1).Value = "📈 Indicadores Clave (KPIs)";
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Range(row, 1, row, 2).Merge();
        row += 2;

        var totalRevenue = transactions.Sum(t => (long)t.Amount);
        var avgTransaction = transactions.Any() ? transactions.Average(t => t.Amount) : 0;
        var transactionCount = transactions.Count;

        AddKPI(worksheet, ref row, "Ingresos Totales", $"${totalRevenue:N0} CLP");
        AddKPI(worksheet, ref row, "Número de Transacciones", transactionCount.ToString("N0"));
        AddKPI(worksheet, ref row, "Ticket Promedio", $"${avgTransaction:N0} CLP");

        row += 2;

        // Trends Section
        worksheet.Cell(row, 1).Value = "📊 Tendencias y Patrones";
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Range(row, 1, row, 2).Merge();
        row += 2;

        // Monthly trend analysis
        var monthlyRevenue = transactions
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                Revenue = g.Sum(t => (long)t.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Period)
            .ToList();

        if (monthlyRevenue.Count >= 2)
        {
            var lastMonth = monthlyRevenue.Last();
            var previousMonth = monthlyRevenue[monthlyRevenue.Count - 2];
            var growth = ((double)(lastMonth.Revenue - previousMonth.Revenue) / previousMonth.Revenue) * 100;

            string trendIcon = growth >= 0 ? "📈" : "📉";
            string trendText = growth >= 0 ? "crecimiento" : "decrecimiento";

            AddInsight(worksheet, ref row, $"{trendIcon} Tendencia Mensual",
                $"Se observa un {trendText} del {Math.Abs(growth):F1}% en los ingresos del último mes respecto al anterior.");
        }

        // Best performing location
        var locationRevenue = transactions
            .Where(t => t.Location != null)
            .GroupBy(t => t.Location!.Name)
            .Select(g => new { Location = g.Key, Revenue = g.Sum(t => (long)t.Amount) })
            .OrderByDescending(x => x.Revenue)
            .FirstOrDefault();

        if (locationRevenue != null)
        {
            AddInsight(worksheet, ref row, "🏆 Sede Destacada",
                $"La sede '{locationRevenue.Location}' lidera con ${locationRevenue.Revenue:N0} CLP en ingresos.");
        }

        // Payment method analysis
        var paymentMethods = transactions
            .GroupBy(t => t.PaymentMethod)
            .Select(g => new { Method = g.Key, Count = g.Count(), Revenue = g.Sum(t => (long)t.Amount) })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        if (paymentMethods.Any())
        {
            var topMethod = paymentMethods.First();
            var percentage = (double)topMethod.Count / transactionCount * 100;
            AddInsight(worksheet, ref row, "💳 Método de Pago Preferido",
                $"'{topMethod.Method}' representa el {percentage:F1}% de las transacciones con ${topMethod.Revenue:N0} CLP.");
        }

        row += 2;

        // Recommendations Section
        worksheet.Cell(row, 1).Value = "💡 Recomendaciones";
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Range(row, 1, row, 2).Merge();
        row += 2;

        // Generate recommendations based on data
        if (monthlyRevenue.Count >= 3)
        {
            var last3Months = monthlyRevenue.TakeLast(3).ToList();
            var avgLast3 = last3Months.Average(m => m.Revenue);
            var trend = last3Months.Last().Revenue > avgLast3 ? "positiva" : "estable";

            AddRecommendation(worksheet, ref row,
                $"La tendencia de los últimos 3 meses es {trend}. Considere mantener las estrategias actuales y explorar oportunidades de expansión.");
        }

        if (paymentMethods.Count > 1)
        {
            AddRecommendation(worksheet, ref row,
                "Diversificar los métodos de pago disponibles puede mejorar la experiencia del cliente y aumentar las conversiones.");
        }

        AddRecommendation(worksheet, ref row,
            "Monitorear regularmente las métricas clave permite identificar oportunidades de mejora y tomar decisiones informadas.");

        worksheet.Columns().AdjustToContents();
        worksheet.Column(2).Width = 80;
    }

    private void AddKPI(IXLWorksheet sheet, ref int row, string label, string value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = value;
        sheet.Cell(row, 2).Style.Font.FontSize = 12;
        sheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        row++;
    }

    private void AddInsight(IXLWorksheet sheet, ref int row, string title, string description)
    {
        sheet.Cell(row, 1).Value = title;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = description;
        sheet.Cell(row, 2).Style.Alignment.WrapText = true;
        row++;
    }

    private void AddRecommendation(IXLWorksheet sheet, ref int row, string recommendation)
    {
        sheet.Cell(row, 1).Value = "✓";
        sheet.Cell(row, 2).Value = recommendation;
        sheet.Cell(row, 2).Style.Alignment.WrapText = true;
        sheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
        row++;
    }
}
