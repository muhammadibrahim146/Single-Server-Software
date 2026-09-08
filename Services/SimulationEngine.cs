using UBLSingleServerSimulation.Models;

namespace UBLSingleServerSimulation.Services;

public static class SimulationEngine
{
    public static SimulationResult Run(List<Customer> input)
    {
        var result = new SimulationResult();

        foreach (var group in input.GroupBy(x => x.Day).OrderBy(x => x.Key))
        {
            var list = group.OrderBy(x => x.ArrivalTime).ToList();
            if (list.Count == 0) continue;

            DateTime serverAvailable = list[0].ArrivalTime;
            DateTime firstArrival = list[0].ArrivalTime;

            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];

                c.InterArrivalTime = i == 0
                    ? 0
                    : (c.ArrivalTime - list[i - 1].ArrivalTime).TotalMinutes;

                c.ServiceStartTime = c.ArrivalTime > serverAvailable
                    ? c.ArrivalTime
                    : serverAvailable;

                c.WaitingTime = (c.ServiceStartTime - c.ArrivalTime).TotalMinutes;
                c.ServiceFinishTime = c.ServiceStartTime.AddMinutes(c.ServiceTime);
                c.TimeInSystem = (c.ServiceFinishTime - c.ArrivalTime).TotalMinutes;

                // FCFS single-server queue length at the exact arrival instant.
                // Only previously arrived customers whose service has not started
                // are counted. Future customers can never enter this count.
                c.QueueLengthAtArrival = list
                    .Take(i)
                    .Count(x => x.ServiceStartTime > c.ArrivalTime);

                serverAvailable = c.ServiceFinishTime;
                result.Customers.Add(c);
            }

            DateTime lastFinish = list.Max(x => x.ServiceFinishTime);
            double duration = (lastFinish - firstArrival).TotalMinutes;
            double busy = list.Sum(x => x.ServiceTime);

            result.Days.Add(new DayResult
            {
                Day = group.Key,
                Customers = list.Count,
                MeanInterArrival = list.Count <= 1 ? 0 : list.Skip(1).Average(x => x.InterArrivalTime),
                MeanService = list.Average(x => x.ServiceTime),
                MeanWaiting = list.Average(x => x.WaitingTime),
                MeanSystem = list.Average(x => x.TimeInSystem),
                BusyTime = busy,
                SimulationDuration = duration,
                Utilization = duration == 0 ? 0 : busy / duration * 100,
                IdleTime = duration - busy,
                MaxWaiting = list.Max(x => x.WaitingTime),
                MinWaiting = list.Min(x => x.WaitingTime),
                MaxQueue = list.Max(x => x.QueueLengthAtArrival)
            });
        }

        return result;
    }

    public static MM1Result CalculateMM1(string scope, double meanInterArrival, double meanService)
    {
        if (meanInterArrival <= 0 || meanService <= 0)
            return new MM1Result { Scope = scope };

        double lambda = 1.0 / meanInterArrival;
        double mu = 1.0 / meanService;

        return new MM1Result
        {
            Scope = scope,
            Lambda = lambda,
            Mu = mu,
            Rho = lambda / mu
        };
    }
}
