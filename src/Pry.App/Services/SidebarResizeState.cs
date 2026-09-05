namespace Pry.App.Services;

public sealed class SidebarResizeState(double minimumWidth = 220, double maximumWidth = 420)
{
    private double _startPointerX;
    private double _startWidth;

    public bool Active { get; private set; }
    public double PendingWidth { get; private set; }

    public void Begin(double pointerX, double currentWidth)
    {
        Active = true;
        _startPointerX = pointerX;
        _startWidth = Math.Clamp(currentWidth, minimumWidth, maximumWidth);
        PendingWidth = _startWidth;
    }

    public double Move(double pointerX)
    {
        if (!Active) return PendingWidth;
        PendingWidth = Math.Clamp(_startWidth + pointerX - _startPointerX, minimumWidth, maximumWidth);
        return PendingWidth;
    }

    public double Complete()
    {
        Active = false;
        return PendingWidth;
    }
}
