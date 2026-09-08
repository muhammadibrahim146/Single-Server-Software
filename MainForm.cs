using UBLSingleServerSimulation.Models;
using UBLSingleServerSimulation.Services;

namespace UBLSingleServerSimulation;

public class MainForm : Form
{
    private readonly Button btnImport = new();
    private readonly Button btnRun = new();
    private readonly Button btnExport = new();
    private readonly Label lblFile = new();
    private readonly Label lblSummary = new();
    private readonly DataGridView grid = new();
    private readonly TabControl tabs = new();
    private readonly DataGridView mmGrid = new();

    private List<Customer> customers = new();
    private SimulationResult? result;

    public MainForm()
    {
        Text = "UBL Gulshan Chowrangi - Single Server M/M/1 Simulation";
        Width = 1400;
        Height = 850;
        StartPosition = FormStartPosition.CenterScreen;

        btnImport.Text = "Import Excel";
        btnImport.SetBounds(20, 20, 140, 40);
        btnImport.Click += ImportExcel;

        btnRun.Text = "Run Simulation";
        btnRun.SetBounds(175, 20, 150, 40);
        btnRun.Click += RunSimulation;

        btnExport.Text = "Export Results";
        btnExport.SetBounds(340, 20, 150, 40);
        btnExport.Click += ExportResults;

        lblFile.Text = "No Excel file selected.";
        lblFile.SetBounds(510, 25, 800, 30);

        lblSummary.Text = "Import the UBL Excel file, then click Run Simulation.";
        lblSummary.SetBounds(20, 80, 1300, 45);
        lblSummary.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        tabs.SetBounds(20, 140, 1340, 650);

        var tracePage = new TabPage("Part A - Trace-Driven Simulation");
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        tracePage.Controls.Add(grid);

        var mmPage = new TabPage("M/M/1 Analytical Results");
        mmGrid.Dock = DockStyle.Fill;
        mmGrid.ReadOnly = true;
        mmGrid.AllowUserToAddRows = false;
        mmGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        mmGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        mmPage.Controls.Add(mmGrid);

        tabs.TabPages.Add(tracePage);
        tabs.TabPages.Add(mmPage);

        Controls.AddRange(new Control[]
        {
            btnImport, btnRun, btnExport, lblFile, lblSummary, tabs
        });
    }

    private void ImportExcel(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            Title = "Select UBL Excel Data"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            customers = ExcelService.Import(dialog.FileName);
            lblFile.Text = $"Loaded: {Path.GetFileName(dialog.FileName)}";
            lblSummary.Text = $"Imported {customers.Count} customers. Click Run Simulation.";
            grid.DataSource = null;
            mmGrid.DataSource = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RunSimulation(object? sender, EventArgs e)
    {
        if (customers.Count == 0)
        {
            MessageBox.Show("Pehle Excel import karo.",
                "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            result = SimulationEngine.Run(customers);
            var o = result.Overall;

            lblSummary.Text =
                $"Customers: {o.Customers} | Mean IAT: {o.MeanInterArrival:F2} min | " +
                $"Mean Service: {o.MeanService:F2} min | Mean Wait: {o.MeanWaiting:F2} min | " +
                $"Mean System: {o.MeanSystem:F2} min | Utilization: {o.Utilization:F2}%";

            grid.DataSource = result.Customers.Select(x => new
            {
                x.Day,
                x.CustomerId,
                Arrival = x.ArrivalTime.ToString("HH:mm"),
                InterArrival = x.InterArrivalTime.ToString("F2"),
                ServiceStart = x.ServiceStartTime.ToString("HH:mm"),
                ServiceTime = x.ServiceTime.ToString("F2"),
                ServiceFinish = x.ServiceFinishTime.ToString("HH:mm"),
                Waiting = x.WaitingTime.ToString("F2"),
                TimeInSystem = x.TimeInSystem.ToString("F2"),
                x.QueueLengthAtArrival
            }).ToList();

            mmGrid.DataSource = result.Days.Append(result.Overall)
                .Select(d =>
                {
                    var m = SimulationEngine.CalculateMM1(d.Day, d.MeanInterArrival, d.MeanService);
                    return new
                    {
                        Scope = d.Day,
                        Lambda = m.Lambda.ToString("F4"),
                        Mu = m.Mu.ToString("F4"),
                        Rho = m.Rho.ToString("F4"),
                        Idle = m.Stable ? $"{m.IdleProbability * 100:F2}%" : "N/A",
                        Lq = m.Stable ? m.Lq.ToString("F2") : "Undefined",
                        Wq = m.Stable ? $"{m.Wq:F2} min" : "Undefined",
                        W = m.Stable ? $"{m.W:F2} min" : "Undefined",
                        L = m.Stable ? m.L.ToString("F2") : "Undefined",
                        Status = m.Stable ? "Stable" : "Not Stable (ρ ≥ 1)"
                    };
                }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Simulation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportResults(object? sender, EventArgs e)
    {
        if (result == null)
        {
            MessageBox.Show("Pehle Run Simulation karo.",
                "No Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = "UBL_Single_Server_MM1_Final_Results.xlsx"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            ExcelService.Export(dialog.FileName, result);
            MessageBox.Show("Results successfully export ho gaye!",
                "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
