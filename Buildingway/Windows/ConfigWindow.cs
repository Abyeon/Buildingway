using System;
using System.Numerics;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Buildingway.Windows;

public class ConfigWindow : CustomWindow, IDisposable
{
    private readonly Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Size = new Vector2(232, 90);
        configuration = plugin.Configuration;
    }

    public void Dispose() { }
    protected override void Render() { }
}
