using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Buildingway.Windows;

public class Overlay : Window, IDisposable
{
    private Plugin Plugin;
    
    public Overlay(Plugin plugin) : base("###BuildingwayOverlay")
    {
        Flags = ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoDocking
                | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.NoTitleBar
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoFocusOnAppearing;
        
        Plugin = plugin;
    }
    
    public override void Draw()
    {
    }

    public void Dispose() { }
}
