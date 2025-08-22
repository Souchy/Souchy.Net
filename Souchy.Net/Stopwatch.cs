namespace Souchy.Net;

public class Stopwatch
{
    private DateTime StartTime { get; set; }
    private DateTime EndTime { get; set; }
    public TimeSpan ElapsedTime => EndTime - StartTime;
    public double ElapsedTimeMs => ElapsedTime.TotalMilliseconds;

    public Stopwatch() => Start();

    public void Start()
    {
        StartTime = DateTime.Now;
        EndTime = StartTime;
    }

    public void Resume()
    {
        StartTime = DateTime.Now;
    }

    public double Stop()
    {
        EndTime = DateTime.Now;
        return ElapsedTimeMs;
    }

    /// <summary>
    /// Moves the EndTime and returns the elapsed time
    /// </summary>
    public double Checkpoint()
    {
        var time = Stop();
        Start();
        return time;
    }


}
