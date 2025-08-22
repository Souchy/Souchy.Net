namespace Souchy.Net;

public record struct Taski(Task Task, CancellationTokenSource Cts)
{
    public void Stop()
    {
        Cts.Cancel();
        Task.Wait();
        Cts.Dispose();
        Task.Dispose();
    }
}

public static class Scheduler
{
    public static Taski RunTimed(int intervalMs, Action<double> action)
    {
        var cts = new CancellationTokenSource();
        var t = Task.Run(() =>
        {
            Stopwatch sw = new();
            double deltaMs = intervalMs;
            while (!cts.Token.IsCancellationRequested)
            {
                sw.Resume();
                action(deltaMs);

                double elapsedMs = sw.Stop();
                if (elapsedMs < intervalMs)
                {
                    Thread.Sleep(intervalMs - (int) elapsedMs);
                    deltaMs = intervalMs;
                }
                else
                {
                    deltaMs = elapsedMs;
                }
            }
            cts.Dispose();
        }, cts.Token);
        return new Taski(t, cts);
    }
    public static Taski RunLoop(Action action)
    {
        var cts = new CancellationTokenSource();
        var t = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                action();
            }
            cts.Dispose();
        }, cts.Token);
        return new Taski(t, cts);
    }
    public static Task Run(Action action)
    {
        var t = Task.Run(() =>
        {
            action();
        });
        return t;
    }
}

public class Executor
{
    public int IntervalMs { get; set; } = 0;
    public Thread Thread { get; private set; }
    private Action action { get; set; }
    public bool Running { get; set; }

    public Executor(Action action)
    {
        this.action = action;
    }

    private void Run(ThreadStart start)
    {
        if (IsRunning)
            throw new Exception("Thread is already running");
        Running = true;
        Thread = new(start);
        Thread.Start();
    }

    private void Run(ParameterizedThreadStart start)
    {
        if (IsRunning)
            throw new Exception("Thread is already running");
        Running = true;
        Thread = new(start);
        Thread.Start();
    }

    public void Schedule(int intervalMs)
    {
        Run(() =>
        {
            while (Running)
            {
                action();
                if (IntervalMs > 0)
                {
                    Thread.Sleep(IntervalMs);
                }
            }
        });
    }

    public void RunLoop()
    {
        Run(() =>
        {

            while (Running)
            {
                action();
            }
        });
    }

    public void RunOnce()
    {
        Run(() =>
        {
            action();
        });
    }

    public bool IsRunning
    {
        get
        {
            if (Running)
                return true;
            if (Thread.IsAlive)
                return true;
            return false;
        }
    }

    public void Stop()
    {
        if (!Running)
            throw new Exception("Thread is not running");
        if (!Thread.IsAlive)
            throw new Exception("Thread is not running");
        Running = false;
    }

    public void StopAndWait()
    {
        Stop();
        Thread.Join();
    }

}
