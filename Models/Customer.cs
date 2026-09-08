namespace UBLSingleServerSimulation.Models;

public class Customer
{
    public string Day { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public DateTime ArrivalTime { get; set; }
    public double InterArrivalTime { get; set; }
    public DateTime ServiceStartTime { get; set; }
    public double ServiceTime { get; set; }
    public DateTime ServiceFinishTime { get; set; }
    public double WaitingTime { get; set; }
    public double TimeInSystem { get; set; }
    public int QueueLengthAtArrival { get; set; }
}
