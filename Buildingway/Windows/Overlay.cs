using System;
using System.Numerics;
using Anyder;
using Anyder.Objects;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
// using Buildingway.Utils.Objects;
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

    public Transform? SelectedTransform = null;
    
    public override void Draw()
    {
        var io = ImGui.GetIO();
        
        ImGuiHelpers.SetWindowPosRelativeMainViewport("###BuildingwayOverlay", new Vector2(0, 0));
        ImGui.SetWindowSize(io.DisplaySize);
        
        if (SelectedTransform == null) return;
        
        var ctrl = ImGui.GetIO().KeyCtrl;
        var shift = ImGui.GetIO().KeyShift;

        if (!ImGuizmo.IsUsing())
        {
            if (ctrl)
            {
                Gizmo.Operation = ImGuizmoOperation.Scale;
            } else if (shift)
            {
                Gizmo.Operation = ImGuizmoOperation.Rotate;
            }
            else
            {
                Gizmo.Operation = ImGuizmoOperation.Translate;
            }
        }
        
        var transform = SelectedTransform;
        if (Gizmo.Manipulate(ref transform, 0.05f, "BuildingwayManipulate"))
        {
            SelectedTransform = transform;
            SelectedTransform.Update();
        }
    }

    public void Dispose() { }
}
