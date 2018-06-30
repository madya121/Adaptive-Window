using System.Diagnostics;
using System.Collections.Generic;

class TimerHistory
{
    private Stopwatch _timer = new Stopwatch();
    private Queue<long> _tickHistory = new Queue<long>();
    private float _averageMilliseconds = 0;
    private int _maxTickCount = 60;
    private bool _isPaused = false;

    public void Start()
    {
        if (_isPaused)
        {
            _timer.Start();
            _isPaused = false;
        }
        else
        {
            _timer.Reset();
            _timer.Start();
        }
    }

    public void Pause()
    {
        _timer.Stop();
        _isPaused = true;
    }

    public void Stop()
    {
        _timer.Stop();
        UpdateEventTimeMeasurement();
        _isPaused = false;
    }

    public float AverageMilliseconds
    {
        get
        {
            return _averageMilliseconds;
        }
    }

    private void UpdateEventTimeMeasurement()
    {
        long ticks = _timer.ElapsedTicks;
        _tickHistory.Enqueue(ticks);

        // Make sure there are a max of maxTickCount in the queue,
        // dropping the oldest measurements
        while (_tickHistory.Count > _maxTickCount)
        {
            _tickHistory.Dequeue();
        }

        // Total and then average the ticks in the queue
        long totalTicks = 0;
        foreach (long t in _tickHistory)
        {
            totalTicks += t;
        }
        float averageTicks = totalTicks / (float)_tickHistory.Count;
        _averageMilliseconds = 1000 * averageTicks / (float)Stopwatch.Frequency;
    }
}
