using System;
using System.Linq;
using System.Numerics;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

namespace Buildingway.Windows;

public class MainWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;
    
    public MainWindow(Plugin plugin) : base("Buildingway##MakesTheBestHouses", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        this.plugin = plugin;
    }

    public void Dispose() { }

    private string path = "bgcommon/hou/outdoor/general/0332/asset/gar_b0_m0332.sgb";
    private bool collision = true;

    protected override void Render()
    {
        if (Plugin.ObjectTable.LocalPlayer == null)
        {
            ImGui.TextUnformatted("Player is null!");
            return;
        }
        var player = Plugin.ObjectTable.LocalPlayer;
        
        ImGui.InputText("Path", ref path);

        if (ImGui.Button("Spawn"))
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                Plugin.ObjectManager.Add(path, player.Position,Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: collision);
            });
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear All"))
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                Plugin.ObjectManager.Clear();
            });
        }

        ImGui.SameLine();
        ImGui.Checkbox("Spawn with collision", ref collision);
        
        Ui.CenteredTextWithLine("Groups", ImGui.GetColorU32(ImGuiCol.TabActive));
        
        var ctrl = ImGui.GetIO().KeyCtrl;

        var id = 0;
        foreach (var group in Plugin.ObjectManager.Groups.ToList())
        {
            using var hoverable = new Ui.Hoverable(id.ToString(), padding: new Vector2(5f, 5f));
            using var pushedId = ImRaii.PushId(id++);
            
            ImGui.Text(group.Path);
            if (ImGuiComponents.IconButton("###GroupReposition", FontAwesomeIcon.ArrowsToDot))
            {
                group.Position = player.Position;
                group.UpdateTransform();
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Set object position to your characters position.");
            }
            
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("###GroupGizmo", FontAwesomeIcon.RulerCombined))
            {
                // TODO: Actually add gizmo for objects
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Edit using the gizmo");
            }
            
            ImGui.SameLine();
            using (_ = ImRaii.Disabled(!ctrl))
            {
                if (ImGuiComponents.IconButton("###GroupErase", FontAwesomeIcon.Eraser))
                {
                    Plugin.ObjectManager.Groups.Remove(group);
                    group.Dispose();
                    continue;
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Ctrl + Click to erase this object.");
                }
            }

            ImGui.SameLine();
            if (ImGui.Checkbox("Collision", ref group.Collide))
            {
                group.UpdateTransform();
            }
            
            if (ImGui.DragFloat3("Position", ref group.Position, 0.05f))
            {
                group.UpdateTransform();
            }
            
            var asEuler = group.Rotation.ToEulerAngles();
            if (ImGui.DragFloat3("Rotation", ref asEuler, 0.05f))
            {
                var asQuat = asEuler.ToQuaternion();
                group.Rotation = asQuat;
                group.UpdateTransform();
            }

            if (ImGui.DragFloat3("Scale", ref group.Scale, 0.05f))
            {
                group.UpdateTransform();
            }
        }
    }
}
