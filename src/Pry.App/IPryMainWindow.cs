using Avalonia.Controls;

namespace Pry.App;

public interface IPryMainWindow
{
    Window HostWindow { get; }
    string ActiveModelLink { get; }
    void ShowFromTray();
    Task PrepareForExitAsync(bool stopModelService);
}
