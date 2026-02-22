using System;
using System.Linq;
using System.Numerics;
using Anyder;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace Buildingway.Windows;

public class SavedPathsWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;
    
    public SavedPathsWindow(Plugin plugin) : base("Saved Paths##BuildingwayPaths")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        this.plugin = plugin;
    }

    protected override void Render()
    {
        if (Plugin.ObjectTable.LocalPlayer == null) return;
        var player = Plugin.ObjectTable.LocalPlayer;
        
        var collision = plugin.Configuration.SpawnWithCollision;
        if (ImGui.Checkbox("Spawn with collision", ref collision))
        {
            plugin.Configuration.SpawnWithCollision = collision;
            plugin.Configuration.Save();
        }
        
        using var child = ImRaii.Child("##ItemChild");
        if (!child.Success) return;

        uint id = 0;
        foreach (var (path, nickname) in plugin.Configuration.PathDictionary.ToList())
        {
            ImGui.PushID(id++);
            using (new Ui.Hoverable(path, rounding: 0f, padding: new Vector2(5f, 2f), highlight: true))
            {
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{nickname}");
                ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey3, $"{path}");
            }
            
            if (Ui.Hovered(path))
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var obj = AnyderService.ObjectManager.Add(path, player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.ShouldSpawnWithCollision);
                    obj.Name = nickname;
                } else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup("##ItemRightPopup");
                }
            }

            using (var popup = ImRaii.Popup("##ItemRightPopup"))
            {
                if (popup.Success)
                {
                    if (ImGui.Button("Delete"))
                    {
                        plugin.Configuration.PathDictionary.Remove(path);
                        plugin.Configuration.Save();
                    }
                }
            }
            
            ImGuiHelpers.ScaledDummy(1);
        }
    }

    public void Dispose() { }
}
