using ClosedXML.Excel;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.FinancialReports.Exports;

namespace Sanes.Infrastructure.FinancialReports.Exports;

public class ExcelFinancialReportExportService
    : IFinancialReportExportService
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string MoneyFormat =
        "#,##0.00";

    private const string PercentageFormat =
        "0.00\"%\"";

    private const string DateFormat =
        "dd/MM/yyyy";

    private const string DateTimeFormat =
        "dd/MM/yyyy HH:mm";

    public FinancialReportExportFile ExportPortfolioToExcel(
        FinancialPortfolioReportResponse report,
        FinancialReportExportContext context)
    {
        using var workbook =
            new XLWorkbook();

        var summarySheet =
            workbook.Worksheets.Add(
                "Resumen");

        var detailSheet =
            workbook.Worksheets.Add(
                "Cartera");

        WriteReportHeader(
            summarySheet,
            "Reporte de Cartera Activa",
            context,
            4);

        var summaryRow =
            WriteFilters(
                summarySheet,
                context,
                6);

        summaryRow += 1;

        WritePortfolioSummary(
            summarySheet,
            report.Summary,
            summaryRow);

        WriteReportHeader(
            detailSheet,
            "Detalle de Cartera Activa",
            context,
            8);

        var detailHeaderRow =
            6;

        WritePortfolioDetail(
            detailSheet,
            report.Items,
            detailHeaderRow);

        FinalizeWorksheet(
            summarySheet);

        FinalizeWorksheet(
            detailSheet);

        return BuildFile(
            workbook,
            $"cartera-activa-{BuildTimestamp()}.xlsx");
    }

    public FinancialReportExportFile ExportDelinquencyToExcel(
        FinancialDelinquencyReportResponse report,
        FinancialReportExportContext context)
    {
        using var workbook =
            new XLWorkbook();

        var summarySheet =
            workbook.Worksheets.Add(
                "Resumen y Aging");

        var detailSheet =
            workbook.Worksheets.Add(
                "Morosidad");

        WriteReportHeader(
            summarySheet,
            "Reporte de Morosidad",
            context,
            6);

        var currentRow =
            WriteFilters(
                summarySheet,
                context,
                6);

        currentRow += 1;

        currentRow =
            WriteDelinquencySummary(
                summarySheet,
                report.Summary,
                currentRow);

        currentRow += 2;

        WriteAging(
            summarySheet,
            report.Aging,
            currentRow);

        WriteReportHeader(
            detailSheet,
            "Detalle de Morosidad",
            context,
            8);

        WriteDelinquencyDetail(
            detailSheet,
            report.Items,
            6);

        FinalizeWorksheet(
            summarySheet);

        FinalizeWorksheet(
            detailSheet);

        return BuildFile(
            workbook,
            $"morosidad-{BuildTimestamp()}.xlsx");
    }

    public FinancialReportExportFile ExportCollectionsToExcel(
        FinancialCollectionsReportResponse report,
        FinancialReportExportContext context)
    {
        using var workbook =
            new XLWorkbook();

        var summarySheet =
            workbook.Worksheets.Add(
                "Resumen");

        var detailSheet =
            workbook.Worksheets.Add(
                "Cobros");

        WriteReportHeader(
            summarySheet,
            "Reporte de Cobros",
            context,
            4);

        var currentRow =
            WriteFilters(
                summarySheet,
                context,
                6);

        summarySheet.Cell(
            currentRow,
            1).Value =
            "Período";

        summarySheet.Cell(
            currentRow,
            2).Value =
            $"{report.From:dd/MM/yyyy} - " +
            $"{report.To:dd/MM/yyyy}";

        currentRow += 2;

        WriteCollectionsSummary(
            summarySheet,
            report.Summary,
            currentRow);

        WriteReportHeader(
            detailSheet,
            "Detalle de Cobros",
            context,
            8);

        detailSheet.Cell(
            5,
            1).Value =
            "Período";

        detailSheet.Cell(
            5,
            2).Value =
            $"{report.From:dd/MM/yyyy} - " +
            $"{report.To:dd/MM/yyyy}";

        WriteCollectionsDetail(
            detailSheet,
            report.Items,
            7);

        FinalizeWorksheet(
            summarySheet);

        FinalizeWorksheet(
            detailSheet);

        return BuildFile(
            workbook,
            $"cobros-{report.From:yyyyMMdd}-" +
            $"{report.To:yyyyMMdd}.xlsx");
    }

    public FinancialReportExportFile ExportInvestorStatementToExcel(
        FinancialInvestorStatementResponse report,
        FinancialReportExportContext context)
    {
        using var workbook =
            new XLWorkbook();

        var statementSheet =
            workbook.Worksheets.Add(
                "Estado");

        var loansSheet =
            workbook.Worksheets.Add(
                "Prestamos activos");

        WriteReportHeader(
            statementSheet,
            "Estado Financiero del Inversionista",
            context,
            4);

        statementSheet.Cell(
            6,
            1).Value =
            "Inversionista";

        statementSheet.Cell(
            6,
            2).Value =
            report.InvestorName;

        statementSheet.Cell(
            7,
            1).Value =
            "Estado";

        statementSheet.Cell(
            7,
            2).Value =
            report.IsActive
                ? "Activo"
                : "Inactivo";

        var currentRow =
            9;

        currentRow =
            WriteInvestorHistoricalSummary(
                statementSheet,
                report,
                currentRow);

        currentRow += 2;

        WriteInvestorCurrentSummary(
            statementSheet,
            report,
            currentRow);

        WriteReportHeader(
            loansSheet,
            $"Préstamos Activos - {report.InvestorName}",
            context,
            8);

        WritePortfolioDetail(
            loansSheet,
            report.ActiveLoans,
            6);

        FinalizeWorksheet(
            statementSheet);

        FinalizeWorksheet(
            loansSheet);

        return BuildFile(
            workbook,
            $"estado-inversionista-" +
            $"{SanitizeFileName(report.InvestorName)}-" +
            $"{BuildTimestamp()}.xlsx");
    }

    private static void WriteReportHeader(
        IXLWorksheet worksheet,
        string reportTitle,
        FinancialReportExportContext context,
        int lastColumn)
    {
        worksheet.Cell(
            1,
            1).Value =
            context.TenantName;

        worksheet.Range(
                1,
                1,
                1,
                lastColumn)
            .Merge();

        worksheet.Cell(
            1,
            1).Style.Font.Bold =
            true;

        worksheet.Cell(
            1,
            1).Style.Font.FontSize =
            16;

        worksheet.Cell(
            2,
            1).Value =
            reportTitle;

        worksheet.Range(
                2,
                1,
                2,
                lastColumn)
            .Merge();

        worksheet.Cell(
            2,
            1).Style.Font.Bold =
            true;

        worksheet.Cell(
            2,
            1).Style.Font.FontSize =
            13;

        worksheet.Cell(
            3,
            1).Value =
            "Moneda";

        worksheet.Cell(
            3,
            2).Value =
            BuildCurrencyLabel(
                context);

        worksheet.Cell(
            4,
            1).Value =
            "Generado";

        worksheet.Cell(
            4,
            2).Value =
            context.GeneratedAtUtc;

        worksheet.Cell(
            4,
            2).Style.NumberFormat.Format =
            DateTimeFormat;

        worksheet.Range(
                1,
                1,
                4,
                lastColumn)
            .Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;
    }

    private static int WriteFilters(
        IXLWorksheet worksheet,
        FinancialReportExportContext context,
        int startRow)
    {
        worksheet.Cell(
            startRow,
            1).Value =
            "Filtros aplicados";

        worksheet.Cell(
            startRow,
            1).Style.Font.Bold =
            true;

        var row =
            startRow + 1;

        if (context.Filters.Count == 0)
        {
            worksheet.Cell(
                row,
                1).Value =
                "Sin filtros adicionales";

            return row + 1;
        }

        foreach (var filter in context.Filters)
        {
            worksheet.Cell(
                row,
                1).Value =
                filter;

            row++;
        }

        return row;
    }

    private static void WritePortfolioSummary(
        IXLWorksheet worksheet,
        FinancialPortfolioReportSummaryResponse summary,
        int startRow)
    {
        var rows =
            new (string Label, object Value, bool Money)[]
            {
                (
                    "Préstamos activos",
                    summary.LoansCount,
                    false
                ),
                (
                    "Clientes activos",
                    summary.ClientsCount,
                    false
                ),
                (
                    "Capital original",
                    summary.PrincipalAmount,
                    true
                ),
                (
                    "Monto contractual original",
                    summary.ContractualAmount,
                    true
                ),
                (
                    "Saldo contractual pendiente",
                    summary.ContractualBalanceOutstanding,
                    true
                ),
                (
                    "Mora pendiente",
                    summary.LateFeeBalanceOutstanding,
                    true
                ),
                (
                    "Total pendiente",
                    summary.TotalOutstanding,
                    true
                ),
                (
                    "Préstamos vencidos",
                    summary.OverdueLoansCount,
                    false
                ),
                (
                    "Monto contractual vencido",
                    summary.OverdueContractualAmount,
                    true
                ),
                (
                    "Total vencido",
                    summary.TotalOverdueAmountDue,
                    true
                )
            };

        WriteMetricSection(
            worksheet,
            "Resumen",
            rows,
            startRow);

        var rateRow =
            startRow +
            rows.Length +
            1;

        worksheet.Cell(
            rateRow,
            1).Value =
            "Tasa de morosidad";

        worksheet.Cell(
            rateRow,
            2).Value =
            summary.DelinquencyRate;

        worksheet.Cell(
            rateRow,
            2).Style.NumberFormat.Format =
            PercentageFormat;
    }

    private static int WriteDelinquencySummary(
        IXLWorksheet worksheet,
        FinancialDelinquencyReportSummaryResponse summary,
        int startRow)
    {
        var rows =
            new (string Label, object Value, bool Money)[]
            {
                (
                    "Préstamos en cartera",
                    summary.LoansCount,
                    false
                ),
                (
                    "Clientes en cartera",
                    summary.ClientsCount,
                    false
                ),
                (
                    "Saldo contractual pendiente",
                    summary.ContractualBalanceOutstanding,
                    true
                ),
                (
                    "Mora pendiente",
                    summary.LateFeeBalanceOutstanding,
                    true
                ),
                (
                    "Total pendiente",
                    summary.TotalOutstanding,
                    true
                ),
                (
                    "Monto contractual vencido",
                    summary.OverdueContractualAmount,
                    true
                ),
                (
                    "Total vencido",
                    summary.TotalOverdueAmountDue,
                    true
                )
            };

        WriteMetricSection(
            worksheet,
            "Resumen",
            rows,
            startRow);

        var rateRow =
            startRow +
            rows.Length +
            1;

        worksheet.Cell(
            rateRow,
            1).Value =
            "Tasa de morosidad";

        worksheet.Cell(
            rateRow,
            2).Value =
            summary.DelinquencyRate;

        worksheet.Cell(
            rateRow,
            2).Style.NumberFormat.Format =
            PercentageFormat;

        return rateRow;
    }

    private static void WriteCollectionsSummary(
        IXLWorksheet worksheet,
        FinancialCollectionsReportSummaryResponse summary,
        int startRow)
    {
        var rows =
            new (string Label, object Value, bool Money)[]
            {
                (
                    "Pagos",
                    summary.PaymentsCount,
                    false
                ),
                (
                    "Clientes",
                    summary.ClientsCount,
                    false
                ),
                (
                    "Efectivo cobrado",
                    summary.CashCollected,
                    true
                ),
                (
                    "Cobrado a saldo contractual",
                    summary.ContractualCashCollected,
                    true
                ),
                (
                    "Mora cobrada",
                    summary.LateFeesCollected,
                    true
                )
            };

        WriteMetricSection(
            worksheet,
            "Resumen",
            rows,
            startRow);
    }

    private static int WriteInvestorHistoricalSummary(
        IXLWorksheet worksheet,
        FinancialInvestorStatementResponse report,
        int startRow)
    {
        var rows =
            new (string Label, object Value, bool Money)[]
            {
                (
                    "Capital originado",
                    report.PrincipalOriginated,
                    true
                ),
                (
                    "Monto contractual originado",
                    report.ContractualAmountOriginated,
                    true
                ),
                (
                    "Interés contractual bruto",
                    report.GrossContractualInterest,
                    true
                ),
                (
                    "Descuentos por liquidación anticipada",
                    report.EarlySettlementDiscounts,
                    true
                ),
                (
                    "Interés contractual neto",
                    report.NetContractualInterest,
                    true
                ),
                (
                    "Efectivo cobrado",
                    report.CashCollected,
                    true
                ),
                (
                    "Cobrado a saldo contractual",
                    report.ContractualCashCollected,
                    true
                ),
                (
                    "Mora cobrada",
                    report.LateFeesCollected,
                    true
                )
            };

        WriteMetricSection(
            worksheet,
            "Histórico",
            rows,
            startRow);

        return startRow +
            rows.Length;
    }

    private static void WriteInvestorCurrentSummary(
        IXLWorksheet worksheet,
        FinancialInvestorStatementResponse report,
        int startRow)
    {
        var rows =
            new (string Label, object Value, bool Money)[]
            {
                (
                    "Préstamos activos",
                    report.ActiveLoansCount,
                    false
                ),
                (
                    "Clientes activos",
                    report.ActiveClientsCount,
                    false
                ),
                (
                    "Saldo contractual pendiente",
                    report.ContractualBalanceOutstanding,
                    true
                ),
                (
                    "Mora pendiente",
                    report.LateFeeBalanceOutstanding,
                    true
                ),
                (
                    "Total pendiente",
                    report.TotalOutstanding,
                    true
                ),
                (
                    "Préstamos vencidos",
                    report.OverdueLoansCount,
                    false
                ),
                (
                    "Monto contractual vencido",
                    report.OverdueContractualAmount,
                    true
                )
            };

        WriteMetricSection(
            worksheet,
            "Cartera actual",
            rows,
            startRow);

        var rateRow =
            startRow +
            rows.Length +
            1;

        worksheet.Cell(
            rateRow,
            1).Value =
            "Tasa de morosidad";

        worksheet.Cell(
            rateRow,
            2).Value =
            report.DelinquencyRate;

        worksheet.Cell(
            rateRow,
            2).Style.NumberFormat.Format =
            PercentageFormat;
    }

    private static void WriteMetricSection(
        IXLWorksheet worksheet,
        string title,
        IEnumerable<
            (string Label, object Value, bool Money)> rows,
        int startRow)
    {
        worksheet.Cell(
            startRow,
            1).Value =
            title;

        worksheet.Range(
                startRow,
                1,
                startRow,
                2)
            .Merge();

        worksheet.Cell(
            startRow,
            1).Style.Font.Bold =
            true;

        var row =
            startRow + 1;

        foreach (var metric in rows)
        {
            worksheet.Cell(
                row,
                1).Value =
                metric.Label;

            SetCellValue(
                worksheet.Cell(
                    row,
                    2),
                metric.Value);

            if (metric.Money)
            {
                worksheet.Cell(
                    row,
                    2).Style.NumberFormat.Format =
                    MoneyFormat;
            }

            row++;
        }
    }

    private static void WriteAging(
        IXLWorksheet worksheet,
        IReadOnlyCollection<
            FinancialDelinquencyAgingResponse> aging,
        int startRow)
    {
        worksheet.Cell(
            startRow,
            1).Value =
            "Aging de Morosidad";

        worksheet.Cell(
            startRow,
            1).Style.Font.Bold =
            true;

        var headerRow =
            startRow + 1;

        var headers =
            new[]
            {
                "Rango",
                "Préstamos",
                "Clientes",
                "Monto contractual vencido",
                "Mora pendiente",
                "Total vencido"
            };

        WriteHeaders(
            worksheet,
            headerRow,
            headers);

        var row =
            headerRow + 1;

        foreach (var item in aging)
        {
            worksheet.Cell(row, 1).Value =
                item.AgingBucket;

            worksheet.Cell(row, 2).Value =
                item.LoansCount;

            worksheet.Cell(row, 3).Value =
                item.ClientsCount;

            worksheet.Cell(row, 4).Value =
                item.OverdueContractualAmount;

            worksheet.Cell(row, 5).Value =
                item.LateFeeBalanceOutstanding;

            worksheet.Cell(row, 6).Value =
                item.TotalOverdueAmountDue;

            worksheet.Range(
                    row,
                    4,
                    row,
                    6)
                .Style.NumberFormat.Format =
                MoneyFormat;

            row++;
        }

        ApplyAutoFilter(
            worksheet,
            headerRow,
            Math.Max(
                headerRow,
                row - 1),
            headers.Length);
    }

    private static void WritePortfolioDetail(
        IXLWorksheet worksheet,
        IReadOnlyCollection<
            FinancialPortfolioReportItemResponse> items,
        int headerRow)
    {
        var headers =
            new[]
            {
                "Cliente",
                "Teléfono",
                "Inversionista",
                "Ruta",
                "Capital",
                "Monto contractual",
                "Saldo contractual",
                "Mora pendiente",
                "Total pendiente",
                "Cuota",
                "Próxima cuota",
                "Próxima fecha",
                "Vencido",
                "Días vencidos",
                "Cuotas vencidas",
                "Monto contractual vencido",
                "Total vencido",
                "% pagado",
                "Frecuencia"
            };

        WriteHeaders(
            worksheet,
            headerRow,
            headers);

        var row =
            headerRow + 1;

        foreach (var item in items)
        {
            worksheet.Cell(row, 1).Value =
                item.ClientName;

            worksheet.Cell(row, 2).Value =
                item.ClientPhone;

            worksheet.Cell(row, 3).Value =
                item.InvestorName;

            worksheet.Cell(row, 4).Value =
                item.CollectionRouteName ??
                "Sin ruta";

            worksheet.Cell(row, 5).Value =
                item.PrincipalAmount;

            worksheet.Cell(row, 6).Value =
                item.ContractualAmount;

            worksheet.Cell(row, 7).Value =
                item.ContractualBalanceOutstanding;

            worksheet.Cell(row, 8).Value =
                item.LateFeeBalanceOutstanding;

            worksheet.Cell(row, 9).Value =
                item.TotalOutstanding;

            worksheet.Cell(row, 10).Value =
                item.InstallmentAmount;

            worksheet.Cell(row, 11).Value =
                item.NextInstallmentAmountDue;

            worksheet.Cell(row, 12).Value =
                item.NextPaymentDate;

            worksheet.Cell(row, 12)
                .Style.NumberFormat.Format =
                DateFormat;

            worksheet.Cell(row, 13).Value =
                item.IsOverdue
                    ? "Sí"
                    : "No";

            worksheet.Cell(row, 14).Value =
                item.DaysOverdue;

            worksheet.Cell(row, 15).Value =
                item.OverdueInstallments;

            worksheet.Cell(row, 16).Value =
                item.OverdueContractualAmount;

            worksheet.Cell(row, 17).Value =
                item.TotalOverdueAmountDue;

            worksheet.Cell(row, 18).Value =
                item.PercentagePaid;

            worksheet.Cell(row, 18)
                .Style.NumberFormat.Format =
                PercentageFormat;

            worksheet.Cell(row, 19).Value =
                item.PaymentFrequency.ToString();

            worksheet.Range(
                    row,
                    5,
                    row,
                    11)
                .Style.NumberFormat.Format =
                MoneyFormat;

            worksheet.Range(
                    row,
                    16,
                    row,
                    17)
                .Style.NumberFormat.Format =
                MoneyFormat;

            row++;
        }

        ApplyAutoFilter(
            worksheet,
            headerRow,
            Math.Max(
                headerRow,
                row - 1),
            headers.Length);

        worksheet.SheetView
            .FreezeRows(
                headerRow);
    }

    private static void WriteDelinquencyDetail(
        IXLWorksheet worksheet,
        IReadOnlyCollection<
            FinancialDelinquencyReportItemResponse> items,
        int headerRow)
    {
        var headers =
            new[]
            {
                "Cliente",
                "Teléfono",
                "Inversionista",
                "Ruta",
                "Saldo contractual",
                "Mora pendiente",
                "Total pendiente",
                "Cuota",
                "Próxima cuota",
                "Próxima fecha",
                "Días vencidos",
                "Cuotas vencidas",
                "Monto contractual vencido",
                "Total vencido",
                "Aging",
                "Frecuencia"
            };

        WriteHeaders(
            worksheet,
            headerRow,
            headers);

        var row =
            headerRow + 1;

        foreach (var item in items)
        {
            worksheet.Cell(row, 1).Value =
                item.ClientName;

            worksheet.Cell(row, 2).Value =
                item.ClientPhone;

            worksheet.Cell(row, 3).Value =
                item.InvestorName;

            worksheet.Cell(row, 4).Value =
                item.CollectionRouteName ??
                "Sin ruta";

            worksheet.Cell(row, 5).Value =
                item.ContractualBalanceOutstanding;

            worksheet.Cell(row, 6).Value =
                item.LateFeeBalanceOutstanding;

            worksheet.Cell(row, 7).Value =
                item.TotalOutstanding;

            worksheet.Cell(row, 8).Value =
                item.InstallmentAmount;

            worksheet.Cell(row, 9).Value =
                item.NextInstallmentAmountDue;

            worksheet.Cell(row, 10).Value =
                item.NextPaymentDate;

            worksheet.Cell(row, 10)
                .Style.NumberFormat.Format =
                DateFormat;

            worksheet.Cell(row, 11).Value =
                item.DaysOverdue;

            worksheet.Cell(row, 12).Value =
                item.OverdueInstallments;

            worksheet.Cell(row, 13).Value =
                item.OverdueContractualAmount;

            worksheet.Cell(row, 14).Value =
                item.TotalOverdueAmountDue;

            worksheet.Cell(row, 15).Value =
                item.AgingBucket;

            worksheet.Cell(row, 16).Value =
                item.PaymentFrequency.ToString();

            worksheet.Range(
                    row,
                    5,
                    row,
                    9)
                .Style.NumberFormat.Format =
                MoneyFormat;

            worksheet.Range(
                    row,
                    13,
                    row,
                    14)
                .Style.NumberFormat.Format =
                MoneyFormat;

            row++;
        }

        ApplyAutoFilter(
            worksheet,
            headerRow,
            Math.Max(
                headerRow,
                row - 1),
            headers.Length);

        worksheet.SheetView
            .FreezeRows(
                headerRow);
    }

    private static void WriteCollectionsDetail(
        IXLWorksheet worksheet,
        IReadOnlyCollection<
            FinancialCollectionsReportItemResponse> items,
        int headerRow)
    {
        var headers =
            new[]
            {
                "Fecha",
                "Recibo",
                "Cliente",
                "Inversionista",
                "Cobrador",
                "Ruta",
                "Tipo",
                "Efectivo cobrado",
                "Aplicado a contrato",
                "Mora cobrada"
            };

        WriteHeaders(
            worksheet,
            headerRow,
            headers);

        var row =
            headerRow + 1;

        foreach (var item in items)
        {
            worksheet.Cell(row, 1).Value =
                item.PaymentDate;

            worksheet.Cell(row, 1)
                .Style.NumberFormat.Format =
                DateTimeFormat;

            worksheet.Cell(row, 2).Value =
                item.ReceiptNumber ??
                string.Empty;

            worksheet.Cell(row, 3).Value =
                item.ClientName;

            worksheet.Cell(row, 4).Value =
                item.InvestorName;

            worksheet.Cell(row, 5).Value =
                item.CollectorName ??
                "Administrativo";

            worksheet.Cell(row, 6).Value =
                item.CollectionRouteName ??
                "Sin ruta";

            worksheet.Cell(row, 7).Value =
                item.PaymentType.ToString();

            worksheet.Cell(row, 8).Value =
                item.CashCollected;

            worksheet.Cell(row, 9).Value =
                item.ContractualCashCollected;

            worksheet.Cell(row, 10).Value =
                item.LateFeesCollected;

            worksheet.Range(
                    row,
                    8,
                    row,
                    10)
                .Style.NumberFormat.Format =
                MoneyFormat;

            row++;
        }

        ApplyAutoFilter(
            worksheet,
            headerRow,
            Math.Max(
                headerRow,
                row - 1),
            headers.Length);

        worksheet.SheetView
            .FreezeRows(
                headerRow);
    }

    private static void WriteHeaders(
        IXLWorksheet worksheet,
        int row,
        IReadOnlyList<string> headers)
    {
        for (
            var column = 0;
            column < headers.Count;
            column++)
        {
            worksheet.Cell(
                row,
                column + 1).Value =
                headers[column];
        }

        var range =
            worksheet.Range(
                row,
                1,
                row,
                headers.Count);

        range.Style.Font.Bold =
            true;

        range.Style.Alignment.WrapText =
            true;

        range.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;
    }

    private static void ApplyAutoFilter(
        IXLWorksheet worksheet,
        int headerRow,
        int lastRow,
        int lastColumn)
    {
        if (lastRow <= headerRow)
        {
            return;
        }

        worksheet.Range(
                headerRow,
                1,
                lastRow,
                lastColumn)
            .SetAutoFilter();
    }

    private static void FinalizeWorksheet(
        IXLWorksheet worksheet)
    {
        var usedRange =
            worksheet.RangeUsed();

        if (usedRange is null)
        {
            return;
        }

        worksheet.ColumnsUsed()
            .AdjustToContents();

        /*
         * Evitamos columnas exageradamente anchas
         * por nombres o textos largos.
         */
        foreach (
            var column in
            worksheet.ColumnsUsed())
        {
            if (column.Width > 40)
            {
                column.Width =
                    40;
            }
        }

        worksheet.RowsUsed()
            .Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;
    }

    private static FinancialReportExportFile BuildFile(
        XLWorkbook workbook,
        string fileName)
    {
        using var stream =
            new MemoryStream();

        workbook.SaveAs(
            stream);

        return new FinancialReportExportFile
        {
            Content =
                stream.ToArray(),

            ContentType =
                ExcelContentType,

            FileName =
                fileName
        };
    }

    private static string BuildCurrencyLabel(
        FinancialReportExportContext context)
    {
        if (
            string.IsNullOrWhiteSpace(
                context.CurrencySymbol))
        {
            return context.CurrencyCode;
        }

        return
            $"{context.CurrencyCode} " +
            $"({context.CurrencySymbol})";
    }

    private static string BuildTimestamp()
    {
        return DateTime.UtcNow
            .ToString(
                "yyyyMMdd-HHmmss");
    }

    private static string SanitizeFileName(
        string value)
    {
        var invalidCharacters =
            Path.GetInvalidFileNameChars();

        var cleaned =
            new string(
                value
                    .Select(character =>
                        invalidCharacters.Contains(
                            character)
                            ? '-'
                            : character)
                    .ToArray());

        return string.Join(
            "-",
            cleaned
                .Split(
                    ' ',
                    StringSplitOptions
                        .RemoveEmptyEntries));
    }

    private static void SetCellValue(
        IXLCell cell,
        object value)
    {
        switch (value)
        {
            case int intValue:
                cell.Value =
                    intValue;
                break;

            case decimal decimalValue:
                cell.Value =
                    decimalValue;
                break;

            case long longValue:
                cell.Value =
                    longValue;
                break;

            case double doubleValue:
                cell.Value =
                    doubleValue;
                break;

            case DateTime dateTimeValue:
                cell.Value =
                    dateTimeValue;
                break;

            default:
                cell.Value =
                    value.ToString() ??
                    string.Empty;
                break;
        }
    }
}