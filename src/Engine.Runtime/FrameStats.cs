namespace Engine.Runtime;

/// <summary>Collects frame timing and simulation counters, averaged over half-second windows so the numbers are readable.</summary>
internal sealed class FrameStats
{
    private double _accumulated;
    private int _frames;

    /// <summary>Frames per second, averaged over the last window.</summary>
    public double Fps { get; private set; }

    /// <summary>Average milliseconds per frame over the last window.</summary>
    public double FrameMs { get; private set; }

    /// <summary>Total fixed simulation steps run since start.</summary>
    public long FixedSteps { get; private set; }

    /// <summary>Records one rendered frame.</summary>
    /// <param name="dt">Seconds the frame took.</param>
    public void RecordFrame(double dt)
    {
        _accumulated += dt;
        _frames++;
        if (_accumulated < 0.5) return;

        Fps = _frames / _accumulated;
        FrameMs = _accumulated / _frames * 1000;
        _accumulated = 0;
        _frames = 0;
    }

    /// <summary>Records fixed simulation steps that ran this update.</summary>
    public void RecordFixedSteps(int steps) => FixedSteps += steps;
}
