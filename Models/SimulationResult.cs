namespace UBLSingleServerSimulation.Models;

public class DayResult
{
    public string Day { get; set; } = "";
    public int Customers { get; set; }
    public double MeanInterArrival { get; set; }
    public double MeanService { get; set; }
    public double MeanWaiting { get; set; }
    public double MeanSystem { get; set; }
    public double BusyTime { get; set; }
    public double SimulationDuration { get; set; }
    public double Utilization { get; set; }
    public double IdleTime { get; set; }
    public double MaxWaiting { get; set; }
    public double MinWaiting { get; set; }
    public int MaxQueue { get; set; }
}

public class MM1Result
{
    public string Scope { get; set; } = "";
    public double Lambda { get; set; }
    public double Mu { get; set; }
    public double Rho { get; set; }
    public bool Stable => Rho < 1.0;
    public double IdleProbability => Stable ? 1.0 - Rho : double.NaN;
    public double Lq => Stable ? (Rho * Rho) / (1.0 - Rho) : double.NaN;
    public double Wq => Stable ? Lq / Lambda : double.NaN;
    public double W => Stable ? Wq + (1.0 / Mu) : double.NaN;
    public double L => Stable ? Lambda * W : double.NaN;
}

public class SimulationResult
{
    public List<Customer> Customers { get; set; } = new();
    public List<DayResult> Days { get; set; } = new();

    public DayResult Overall
    {
        get
        {
            int n = Customers.Count;
            if (n == 0) return new DayResult();

            var interArrivals = Customers
                .Where(x => x.InterArrivalTime > 0)
                .Select(x => x.InterArrivalTime)
                .ToList();

            double busy = Customers.Sum(x => x.ServiceTime);
            double duration = Days.Sum(x => x.SimulationDuration);

            return new DayResult
            {
                Day = "Overall",
                Customers = n,
                MeanInterArrival = interArrivals.Count == 0 ? 0 : interArrivals.Average(),
                MeanService = Customers.Average(x => x.ServiceTime),
                MeanWaiting = Customers.Average(x => x.WaitingTime),
                MeanSystem = Customers.Average(x => x.TimeInSystem),
                BusyTime = busy,
                SimulationDuration = duration,
                Utilization = duration == 0 ? 0 : busy / duration * 100,
                IdleTime = duration - busy,
                MaxWaiting = Customers.Max(x => x.WaitingTime),
                MinWaiting = Customers.Min(x => x.WaitingTime),
                MaxQueue = Customers.Max(x => x.QueueLengthAtArrival)
            };
        }
    }
}
