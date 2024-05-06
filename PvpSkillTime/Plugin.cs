using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace PvpSkillTime;

public sealed class Plugin : IDalamudPlugin
{
    public DalamudPluginInterface Dalamud { get; init; }

    public WindowSystem WindowSystem = new("PVPskillTime");
    private MainWindow _wnd;

    public Plugin(DalamudPluginInterface dalamud)
    {
        dalamud.Create<Service>();

        _wnd = new MainWindow(dalamud);
        WindowSystem.AddWindow(_wnd);

        Dalamud = dalamud;
        dalamud.UiBuilder.DisableAutomaticUiHide = true;
        Dalamud.UiBuilder.Draw += WindowSystem.Draw;

    }

    public void Dispose()
    {
        Dalamud.UiBuilder.Draw -= WindowSystem.Draw;
        WindowSystem.RemoveWindow(_wnd);  // 确保移除窗口
        _wnd.Dispose();

    }
}
