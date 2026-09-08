# UBL Gulshan Chowrangi - Single Server M/M/1 Simulation

C# .NET 8 WinForms project for the Modeling & Simulation course.

## What is included

### Part A - Trace-Driven Single Server Simulation
- Imports the UBL Excel observation data.
- Uses FCFS discipline.
- Uses exactly one teller/server.
- Calculates:
  - Inter-arrival time
  - Service start time
  - Service finish time
  - Waiting time
  - Time in system
  - Queue length at arrival
  - Mean-wise parameters
  - Server utilization
  - Busy/idle time
  - Maximum queue and waiting time

### Part B - M/M/1 Analytical Model
Uses the standard formulas:
- λ = 1 / mean inter-arrival time
- μ = 1 / mean service time
- ρ = λ / μ
- P0 = 1 - ρ
- Lq = ρ² / (1 - ρ)
- Wq = Lq / λ
- W = Wq + 1/μ
- L = λW

The analytical steady-state formulas are shown only when ρ < 1.
If ρ >= 1, the application reports "Not Stable" and keeps the trace-driven simulation results.

## Run

```bash
dotnet restore
dotnet run
```

Windows is required because this is a WinForms application.

## Important
This project is for the UBL branch data. It follows the same single-server/M/M/1 methodology as a typical KFC queue project, but the data, entity, and results are based on UBL.
