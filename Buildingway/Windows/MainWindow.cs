using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
using Buildingway.Utils.Objects.Vfx;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;

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
                
                plugin.Overlay.SelectedTransform = null;
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
            
            plugin.Overlay.SelectedTransform = null;
        }

        if (plugin.Overlay.SelectedTransform != null)
        {
            if (ImGui.Button("Stop Editing")) plugin.Overlay.SelectedTransform = null;
            ImGui.SameLine();
        }
        
        if (ImGui.Button("Clear All"))
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                Plugin.ObjectManager.Clear();
            });
            
            plugin.Overlay.SelectedTransform = null;
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

            if (!DrawHeader(player.Position, ref group.Transform, group.Path, ref id)) continue;
            using (new Ui.Hoverable(id.ToString(), 0f, margin: new Vector2(0f, 0f), padding: new Vector2(5f, 5f), highlight: true))
            {
                DrawWidgets(player.Position, ref group.Transform, group.Path);
                
                ImGui.SameLine();
                using (_ = ImRaii.Disabled(!ctrl))
                {
                    if (ImGuiComponents.IconButton("###GroupErase", FontAwesomeIcon.Eraser))
                    {
                        Plugin.ObjectManager.Groups.Remove(group);
                        group.Dispose();
                        plugin.Overlay.SelectedTransform = null;
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
                    group.Transform.Update();
                }
                
                DrawTransform(ref group.Transform);
            }

            ImGui.Spacing();
        }
        
        Ui.CenteredTextWithLine("Models", ImGui.GetColorU32(ImGuiCol.TabActive));

        foreach (var model in Plugin.ObjectManager.Models.ToList())
        {
            using var pushedId = ImRaii.PushId(id++);
            if (!DrawHeader(player.Position, ref model.Transform, model.Path, ref id)) continue;
            
            using (new Ui.Hoverable(id.ToString(), 0f, margin: new Vector2(0f, 0f), padding: new Vector2(5f, 5f), highlight: true))
            {
                DrawWidgets(player.Position, ref model.Transform, model.Path);
                
                ImGui.SameLine();
                using (_ = ImRaii.Disabled(!ctrl))
                {
                    if (ImGuiComponents.IconButton("###ModelErase", FontAwesomeIcon.Eraser))
                    {
                        Plugin.ObjectManager.Models.Remove(model);
                        model.Dispose();
                        plugin.Overlay.SelectedTransform = null;
                        continue;
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Ctrl + Click to erase this object.");
                    }
                }
                
                DrawTransform(ref model.Transform);
            }

            ImGui.Spacing();
        }
        
        Ui.CenteredTextWithLine("Vfx", ImGui.GetColorU32(ImGuiCol.TabActive));

        foreach (var item in Plugin.ObjectManager.Vfx.ToList())
        {
            if (item is not StaticVfx vfx) continue;

            using var pushedId = ImRaii.PushId(id++);
            if (!DrawHeader(player.Position, ref vfx.Transform, vfx.Path, ref id)) continue;
            
            using (new Ui.Hoverable(id.ToString(), 0f, margin: new Vector2(0f, 0f), padding: new Vector2(5f, 5f), highlight: true))
            {
                DrawWidgets(player.Position, ref vfx.Transform, vfx.Path);
                
                ImGui.SameLine();
                using (_ = ImRaii.Disabled(!ctrl))
                {
                    if (ImGuiComponents.IconButton("###VfxErase", FontAwesomeIcon.Eraser))
                    {
                        Plugin.ObjectManager.Vfx.Remove(vfx);
                        vfx.Dispose();
                        plugin.Overlay.SelectedTransform = null;
                        continue;
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Ctrl + Click to erase this object.");
                    }
                }
                
                DrawTransform(ref vfx.Transform);
            }

            ImGui.Spacing();
        }
    }

    private bool DrawHeader(Vector3 playerPos, ref Transform transform, string itemPath, ref int id)
    {
        var name = plugin.Configuration.PathDictionary.GetValueOrDefault(itemPath, itemPath);
        var distance = Vector3.Distance(transform.Position, playerPos);
        return ImGui.CollapsingHeader($"[{distance:F1}] - {name}###{name}{id++}");
    }

    private void DrawWidgets(Vector3 playerPos, ref Transform transform, string itemPath)
    {
        if (ImGuiComponents.IconButton("###WidgetReposition", FontAwesomeIcon.ArrowsToDot))
        {
            transform.Position = playerPos;
            transform.Update();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Set object position to your characters position.");
        }
                
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("###WidgetGizmo", FontAwesomeIcon.RulerCombined))
        {
            plugin.Overlay.SelectedTransform = transform;
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Edit using the gizmo");
        }
                
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("###WidgetFavorite", FontAwesomeIcon.Star))
        {
            ImGui.OpenPopup("###WidgetFavoritePopup");
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Give this path a nickname.");
        }
                
        var nickname = "";
        if (Ui.AddTextConfirmationPopup("###WidgetFavoritePopup", "Give this path a nickname!", ref nickname))
        {
            if (nickname.Length == 0) return;
            plugin.Configuration.PathDictionary[itemPath] = nickname;
            plugin.Configuration.Save();
        }
    }

    private void DrawTransform(ref Transform transform)
    {
        if (ImGui.DragFloat3("Position", ref transform.Position, 0.05f))
        {
            transform.Update();
        }

        var euler = transform.Rotation.ToEuler();
        if (ImGui.DragFloat3("Rotation", ref euler))
        {
            transform.Rotation = euler.ToQuaternion();
            transform.Update();
        }
        if (ImGui.DragFloat3("Scale", ref transform.Scale, 0.05f))
        {
            transform.Update();
        }
    }
}
