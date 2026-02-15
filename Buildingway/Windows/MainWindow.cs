using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

namespace Buildingway.Windows;

public class MainWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;
    
    private FileDialogManager fileDialogManager;
    
    public MainWindow(Plugin plugin) : base("Buildingway##MakesTheBestHouses", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        this.plugin = plugin;
        fileDialogManager = new FileDialogManager();
    }

    public void Dispose() { }

    private string path = "bgcommon/hou/outdoor/general/0332/asset/gar_b0_m0332.sgb";
    
    protected override void Render()
    { 
        fileDialogManager.Draw();
        
        if (Plugin.ObjectTable.LocalPlayer == null)
        {
            ImGui.TextUnformatted("Player is null!");
            return;
        }
        var player = Plugin.ObjectTable.LocalPlayer;
        
        if (ImGui.Button("Save Layout"))
        {
            var json = Serializer.SerializeCurrent();
            fileDialogManager.SaveFileDialog("Save Layout File", ".json", "layout", ".json", (success, pathToFile) =>
            {
                if (!success) return;
                File.WriteAllText(pathToFile, json);
            });
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Layout"))
        {
            fileDialogManager.OpenFileDialog("Open Layout File", ".json", (success, pathToFile) =>
            {
                if (!success) return;
                var json = File.ReadAllText(pathToFile);
                var layout = Serializer.Deserialize(json);
                
                plugin.Overlay.SelectedGroup = null;
                Plugin.ObjectManager.LoadLayout(layout);
            });
        }
        
        if (ImGui.Button("Furniture Catalog")) plugin.ToggleCatalogUi();

        ImGui.SameLine();
        if (ImGui.Button("Saved Paths")) plugin.ToggleSavedPathsUi();
        
        ImGui.InputText("Path", ref path);

        ImGui.SameLine();
        if (ImGui.Button("Spawn"))
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                Plugin.ObjectManager.Add(path, player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
            });
            
            plugin.Overlay.SelectedGroup = null;
        }

        if (plugin.Overlay.SelectedGroup != null)
        {
            if (ImGui.Button("Stop Editing")) plugin.Overlay.SelectedGroup = null;
            ImGui.SameLine();
        }
        
        if (ImGui.Button("Clear All"))
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                Plugin.ObjectManager.Clear();
            });
            
            plugin.Overlay.SelectedGroup = null;
        }

        ImGui.SameLine();
        var collision = plugin.Configuration.SpawnWithCollision;
        if (ImGui.Checkbox("Spawn with collision", ref collision))
        {
            plugin.Configuration.SpawnWithCollision = collision;
            plugin.Configuration.Save();
        }
        
        Ui.CenteredTextWithLine("Groups", ImGui.GetColorU32(ImGuiCol.TabActive));
        
        var ctrl = ImGui.GetIO().KeyCtrl;

        var id = 0;
        foreach (var group in Plugin.ObjectManager.Groups.ToList())
        {
            using var pushedId = ImRaii.PushId(id++);
            
            if (!plugin.Configuration.PathDictionary.TryGetValue(group.Path, out var groupPath))
            {
                groupPath = group.Path;
            }

            var distance = Vector3.Distance(group.Transform.Position, player.Position);
            if (!ImGui.CollapsingHeader($"[{distance:F1}] - {groupPath}###{groupPath}{id++}")) continue;
            using (new Ui.Hoverable(id.ToString(), 0f, margin: new Vector2(0f, 0f), padding: new Vector2(5f, 5f), highlight: true))
            {
                if (ImGuiComponents.IconButton("###GroupReposition", FontAwesomeIcon.ArrowsToDot))
                {
                    group.Transform.Position = player.Position;
                    group.UpdateTransform();
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Set object position to your characters position.");
                }
                
                ImGui.SameLine();
                if (ImGuiComponents.IconButton("###GroupGizmo", FontAwesomeIcon.RulerCombined))
                {
                    plugin.Overlay.SelectedGroup = group;
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Edit using the gizmo");
                }
                
                ImGui.SameLine();
                if (ImGuiComponents.IconButton("###GroupFavorite", FontAwesomeIcon.Star))
                {
                    ImGui.OpenPopup("###GroupFavoritePopup");
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Give this path a nickname.");
                }
                
                ImGui.SameLine();
                using (_ = ImRaii.Disabled(!ctrl))
                {
                    if (ImGuiComponents.IconButton("###GroupErase", FontAwesomeIcon.Eraser))
                    {
                        Plugin.ObjectManager.Groups.Remove(group);
                        group.Dispose();
                        plugin.Overlay.SelectedGroup = null;
                        continue;
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Ctrl + Click to erase this object.");
                    }
                }

                var nickname = "";
                if (Ui.AddTextConfirmationPopup("###GroupFavoritePopup", "Give this path a nickname!", ref nickname))
                {
                    if (nickname.Length == 0) return;
                    plugin.Configuration.PathDictionary[group.Path] = nickname;
                    plugin.Configuration.Save();
                }

                ImGui.SameLine();
                if (ImGui.Checkbox("Collision", ref group.Collide))
                {
                    group.UpdateTransform();
                }
                
                if (ImGui.DragFloat3("Position", ref group.Transform.Position, 0.05f))
                {
                    group.UpdateTransform();
                }

                var euler = group.Transform.Rotation.ToEuler();
                if (ImGui.DragFloat3("Rotation", ref euler))
                {
                    group.Transform.Rotation = euler.ToQuaternion();
                    group.UpdateTransform();
                }
                
                if (ImGui.DragFloat3("Scale", ref group.Transform.Scale, 0.05f))
                {
                    group.UpdateTransform();
                }
            }

            ImGui.Spacing();
        }
    }
}
