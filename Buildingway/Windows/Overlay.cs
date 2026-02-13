using System;
using System.Numerics;
using Buildingway.Utils.Interface;
using Buildingway.Utils.Objects;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.Utility;
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

    public Group? SelectedGroup = null;
    
    public override void Draw()
    {
        var io = ImGui.GetIO();
        
        ImGuiHelpers.SetWindowPosRelativeMainViewport("###BuildingwayOverlay", new Vector2(0, 0));
        ImGui.SetWindowSize(io.DisplaySize);
        
        if (SelectedGroup == null) return;
        
        var ctrl = ImGui.GetIO().KeyCtrl;
        var shift = ImGui.GetIO().KeyShift;

        if (ctrl)
        {
            DrawExtensions.Operation = ImGuizmoOperation.Scale;
        } else if (shift)
        {
            DrawExtensions.Operation = ImGuizmoOperation.Rotate;
        }
        else
        {
            DrawExtensions.Operation = ImGuizmoOperation.Translate;
        }
        
        var transform = SelectedGroup.Transform;
        if (DrawExtensions.Manipulate(ref transform, 0.05f, "BuildingwayManipulate"))
        {
            SelectedGroup.Transform = transform;
            SelectedGroup.UpdateTransform();
        }
    }

    public void Dispose() { }
}
