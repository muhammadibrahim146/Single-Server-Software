using ClosedXML.Excel;
using UBLSingleServerSimulation.Models;
using System.Globalization;

namespace UBLSingleServerSimulation.Services;

public static class ExcelService
{
    public static List<Customer> Import(string filePath)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();
        var customers = new List<Customer>();
        string currentDay = "Day 1";

        foreach (var row in ws.RowsUsed())
        {
            string id = row.Cell(1).GetString().Trim();
            string arrivalText = row.Cell(2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(id) ||
                id.Equals("Customer ID", StringComparison.OrdinalIgnoreCase))
                continue;

            if (id.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
            {
                currentDay = id;
                continue;
            }

            // The workbook may use a blank row between observation days.
            if (string.IsNullOrWhiteSpace(arrivalText))
                continue;

            if (!TryReadTime(row.Cell(2), out DateTime arrival))
                continue;

            double service = ReadDouble(row.Cell(5));

            if (string.IsNullOrWhiteSpace(currentDay))
                currentDay = "Day 1";

            customers.Add(new Customer
            {
                Day = currentDay,
                CustomerId = id,
                ArrivalTime = arrival,
                ServiceTime = service
            });
        }

        // If the workbook has no explicit Day labels, infer three blocks
        // of 30 customers for the supplied UBL observation workbook.
        if (customers.Count > 0 && customers.All(x => x.Day == "Day 1"))
        {
            for (int i = 0; i < customers.Count; i++)
                customers[i].Day = $"Day {(i / 30) + 1}";
        }

        Validate(customers);
        return customers;
    }

    private static void Validate(List<Customer> customers)
    {
        if (customers.Count == 0)
            throw new InvalidOperationException("No valid customer records were found.");

        var bad = customers.Where(x => x.ServiceTime < 0).ToList();
        if (bad.Count > 0)
            throw new InvalidOperationException(
                "Invalid data found: negative service time for " +
                string.Join(", ", bad.Select(x => x.CustomerId)));
    }

    private static bool TryReadTime(IXLCell cell, out DateTime value)
    {
        if (cell.TryGetValue<DateTime>(out value))
            return true;

        string s = cell.GetString().Trim();
        return DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out value)
            || DateTime.TryParse(s, out value);
    }

    private static double ReadDouble(IXLCell cell)
    {
        if (cell.TryGetValue<double>(out double d))
            return d;

        return double.TryParse(cell.GetString(), NumberStyles.Any,
            CultureInfo.InvariantCulture, out d) ? d : 0;
    }

    public static void Export(string filePath, SimulationResult result)
    {
        using var wb = new XLWorkbook();

        var ws = wb.Worksheets.Add("FINAL RESULTS");
        ws.Cell(1, 1).Value = "UBL Single Server Simulation - M/M/1";
        ws.Range(1, 1, 1, 8).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        ws.Cell(3, 1).Value = "Trace-Driven Simulation Results";
        ws.Cell(3, 1).Style.Font.Bold = true;

        string[] headers = { "Scope", "Customers", "Mean Inter-Arrival (min)", "Mean Service (min)",
            "Mean Waiting (min)", "Mean Time in System (min)", "Utilization (%)", "Max Queue" };

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(4, c + 1).Value = headers[c];

        int r = 5;
        foreach (var d in result.Days.Append(result.Overall))
        {
            ws.Cell(r, 1).Value = d.Day;
            ws.Cell(r, 2).Value = d.Customers;
            ws.Cell(r, 3).Value = d.MeanInterArrival;
            ws.Cell(r, 4).Value = d.MeanService;
            ws.Cell(r, 5).Value = d.MeanWaiting;
            ws.Cell(r, 6).Value = d.MeanSystem;
            ws.Cell(r, 7).Value = d.Utilization;
            ws.Cell(r, 8).Value = d.MaxQueue;
            r++;
        }

        int mmStart = r + 2;
        ws.Cell(mmStart, 1).Value = "M/M/1 Analytical Results";
        ws.Cell(mmStart, 1).Style.Font.Bold = true;

        string[] mmHeaders = { "Scope", "Lambda (cust/min)", "Mu (cust/min)", "Rho", "Idle Probability",
            "Lq (cust)", "Wq (min)", "W (min)", "L (cust)", "Status" };

        for (int c = 0; c < mmHeaders.Length; c++)
            ws.Cell(mmStart + 1, c + 1).Value = mmHeaders[c];

        r = mmStart + 2;
        foreach (var d in result.Days.Append(result.Overall))
        {
            var mm = SimulationEngine.CalculateMM1(d.Day, d.MeanInterArrival, d.MeanService);
            ws.Cell(r, 1).Value = mm.Scope;
            ws.Cell(r, 2).Value = mm.Lambda;
            ws.Cell(r, 3).Value = mm.Mu;
            ws.Cell(r, 4).Value = mm.Rho;
            ws.Cell(r, 5).Value = mm.Stable ? mm.IdleProbability : "N/A";
            ws.Cell(r, 6).Value = mm.Stable ? mm.Lq : "Undefined";
            ws.Cell(r, 7).Value = mm.Stable ? mm.Wq : "Undefined";
            ws.Cell(r, 8).Value = mm.Stable ? mm.W : "Undefined";
            ws.Cell(r, 9).Value = mm.Stable ? mm.L : "Undefined";
            ws.Cell(r, 10).Value = mm.Stable ? "Stable (rho < 1)" : "Not stable (rho >= 1)";
            r++;
        }

        ws.Cell(r + 1, 1).Value =
            "Note: M/M/1 formulas are steady-state analytical formulas and require rho < 1. " +
            "The trace-driven simulation remains valid even when rho >= 1.";
        ws.Range(r + 1, 1, r + 1, 10).Merge();
        ws.Cell(r + 1, 1).Style.Alignment.WrapText = true;

        ws.Columns().AdjustToContents();

        var detail = wb.Worksheets.Add("Customer Calculations");
        string[] dh = { "Day", "Customer ID", "Arrival Time", "Inter-Arrival (min)",
            "Service Start", "Service Time (min)", "Service Finish", "Waiting (min)",
            "Time in System (min)", "Queue Length at Arrival" };

        for (int c = 0; c < dh.Length; c++)
            detail.Cell(1, c + 1).Value = dh[c];

        int dr = 2;
        foreach (var x in result.Customers)
        {
            detail.Cell(dr, 1).Value = x.Day;
            detail.Cell(dr, 2).Value = x.CustomerId;
            detail.Cell(dr, 3).Value = x.ArrivalTime.ToString("HH:mm");
            detail.Cell(dr, 4).Value = x.InterArrivalTime;
            detail.Cell(dr, 5).Value = x.ServiceStartTime.ToString("HH:mm");
            detail.Cell(dr, 6).Value = x.ServiceTime;
            detail.Cell(dr, 7).Value = x.ServiceFinishTime.ToString("HH:mm");
            detail.Cell(dr, 8).Value = x.WaitingTime;
            detail.Cell(dr, 9).Value = x.TimeInSystem;
            detail.Cell(dr, 10).Value = x.QueueLengthAtArrival;
            dr++;
        }
        detail.Columns().AdjustToContents();

        wb.SaveAs(filePath);
    }
}
