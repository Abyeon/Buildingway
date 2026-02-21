using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Anyder;
using Anyder.Objects;
using Anyder.Objects.Vfx;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
// using Buildingway.Utils.Objects;
// using Buildingway.Utils.Objects.Vfx;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using ECommons.Reflection;

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
#if (!DEBUG)
        if (!plugin.Enabled)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Please install Hyperborea!");
            if (ImGui.Button("Add Repo"))
            {
                const string repo = "https://puni.sh/api/repository/kawaii";
                if (!DalamudReflector.HasRepo(repo)) DalamudReflector.AddRepo(repo, true);
            }
        
            return;
        }
        
        if (!plugin.Hyperborea.GetFoP<bool>("Enabled"))
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Please enable Hyperborea!");
            return;
        }
#endif
        
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
                plugin.LoadLayout(layout);
                // Plugin.ObjectManager.LoadLayout(layout);
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
                AnyderService.ObjectManager.Add(path, player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
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
                AnyderService.ObjectManager.Clear();
                // Plugin.ObjectManager.Clear();
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
        foreach (var obj in AnyderService.ObjectManager.Objects.ToList())
        {
            if (obj.Type is ObjectType.ActorVfx || !obj.IsValid) continue;
            using var pushedId = ImRaii.PushId(id++);

            var transform = obj.GetTransform();
            var opened = DrawHeader(player.Position, obj, ref transform!, ref id);
            // CheckHighlight(obj);

            if (!opened) continue;
            using (new Ui.Hoverable(id.ToString(), 0f, margin: new Vector2(0f, 0f), padding: new Vector2(5f, 5f), highlight: true))
            {
                DrawWidgets(player.Position, ref transform!, obj.Path);
                
                ImGui.SameLine();
                using (_ = ImRaii.Disabled(!ctrl))
                {
                    if (ImGuiComponents.IconButton("###GroupErase", FontAwesomeIcon.Eraser))
                    {
                        AnyderService.ObjectManager.Objects.Remove(obj);
                        obj.Dispose();
                        plugin.Overlay.SelectedTransform = null;
                        continue;
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Ctrl + Click to erase this object.");
                    }
                }

                if (obj.Type is ObjectType.SharedGroup)
                {
                    ImGui.SameLine();
                    if (ImGui.Checkbox("Collision", ref obj.Group!.Collide))
                    {
                        transform.Update();
                    }
                }
                
                DrawTransform(ref transform);
                // group.DrawInfo();
            }
            
            ImGui.Spacing();
        }
    }

    private void CheckHighlight(Group group)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            group.IsHovered = true;
            group.SetHighlight(40);
        }
        else if (group.IsHovered)
        {
            group.IsHovered = false;
            group.SetHighlight(0);
        }
    }

    private bool DrawHeader(Vector3 playerPos, SpawnedObject obj, ref Transform transform, ref int id)
    {
        var name = plugin.Configuration.PathDictionary.GetValueOrDefault(obj.Path, obj.Name);
        var distance = Vector3.Distance(transform.Position, playerPos);
        return ImGui.CollapsingHeader($"[{distance:F1}] - {name} [{obj.Type}]###{name}{id++}");
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
        if (ImGuiComponents.IconButton("###WidgetCopy", FontAwesomeIcon.Copy))
        {
            var transformCopy = transform;
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                AnyderService.ObjectManager.Add(itemPath, transformCopy.Position, transformCopy.Rotation, transformCopy.Scale, plugin.Configuration.SpawnWithCollision);
            });
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
