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
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

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

    private string path = "";
    
    //bgcommon/hou/outdoor/general/0332/asset/gar_b0_m0332.sgb
    
    protected override void Render()
    {
        switch (plugin.Enabled)
        {
            case false:
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "Hyperborea not installed, collisions will be disabled.");
                ImGui.SameLine();
                if (ImGui.Button("Add Repo"))
                {
                    const string repo = "https://puni.sh/api/repository/kawaii";
                    if (!DalamudReflector.HasRepo(repo)) DalamudReflector.AddRepo(repo, true);
                }

                break;
            }
            case true when !plugin.Hyperborea.GetFoP<bool>("Enabled"):
                ImGui.TextColored(ImGuiColors.DalamudRed, "Hyperborea not enabled, collisions will be disabled.");
                break;
        }
        
        if (Plugin.ObjectTable.LocalPlayer == null)
        {
            ImGui.TextUnformatted("Player is null!");
            return;
        }
        
        fileDialogManager.Draw();
        var player = Plugin.ObjectTable.LocalPlayer;
        
        DrawHeader(player);
        Ui.CenteredTextWithLine("Spawned Items", ImGui.GetColorU32(ImGuiCol.TabActive));
        
        using var child = ImRaii.Child("BuildingwayMainChild");
        if (!child.Success) return;
        
        var ctrl = ImGui.GetIO().KeyCtrl;

        var id = 0;
        foreach (var obj in AnyderService.ObjectManager.Objects.ToList())
        {
            if (obj.Type is ObjectType.ActorVfx || !obj.IsValid) continue;
            using var pushedId = ImRaii.PushId(id++);

            var transform = obj.GetTransform();
            var opened = DrawObjHeader(player.Position, obj, ref transform!, ref id);
            var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);

            if (opened)
            {
                using (new Ui.Hoverable(id.ToString(), 0f, margin: new Vector2(0f, 0f), padding: new Vector2(5f, 5f), highlight: true))
                {
                    DrawWidgets(player.Position, obj, ref transform!);
                
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

                    if (plugin.HyperEnabled && obj.Type is ObjectType.SharedGroup)
                    {
                        ImGui.SameLine();
                        if (ImGui.Checkbox("Collision", ref obj.Group!.Collide))
                        {
                            transform.Update();
                        }
                    }
                
                    DrawTransform(ref transform);
                }
            }
            
            // Handle highlighting for supported objects
            hovered = hovered || ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
            if (obj.Type is ObjectType.SharedGroup or ObjectType.Model)
            {
                switch (hovered)
                {
                    case true when !obj.IsHighlighted:
                        obj.Highlight(35);
                        break;
                    case false when obj.IsHighlighted:
                        obj.RemoveHighlight();
                        break;
                }
            }
            
            ImGui.Spacing();
        }
    }

    private void DrawHeader(IPlayerCharacter player)
    {
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
                AnyderService.ObjectManager.Add(path, player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.ShouldSpawnWithCollision);
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
            ImGui.OpenPopup("###BuildingwayClearAll");
        }
        
        if (Ui.AddConfirmationPopup("###BuildingwayClearAll", "Are you sure?\nThis will remove all currently placed objects!"))
        {
            Plugin.Framework.RunOnFrameworkThread(() =>
            {
                AnyderService.ObjectManager.Clear();
            });
                
            plugin.Overlay.SelectedTransform = null;
        }

        if (plugin.HyperEnabled)
        {
            ImGui.SameLine();
            if (ImGui.Button("Enable all Collision"))
            {
                ImGui.OpenPopup("###BuildingwayCollideAll");
            }
        
            if (Ui.AddConfirmationPopup("###BuildingwayCollideAll", "Are you sure?\nThis will give all your objects collision!"))
            {
                Plugin.Framework.RunOnFrameworkThread(() =>
                {
                    foreach (var obj in AnyderService.ObjectManager.Objects)
                        obj.Group?.SetCollision(true);
                });
            }
            
            ImGui.SameLine();
            var collision = plugin.Configuration.SpawnWithCollision;
            if (ImGui.Checkbox("Spawn with collision", ref collision))
            {
                plugin.Configuration.SpawnWithCollision = collision;
                plugin.Configuration.Save();
            }
        }
    }

    private bool DrawObjHeader(Vector3 playerPos, SpawnedObject obj, ref Transform transform, ref int id)
    {
        var name = plugin.Configuration.PathDictionary.GetValueOrDefault(obj.Path, obj.Name);
        var distance = Vector3.Distance(transform.Position, playerPos);
        return ImGui.CollapsingHeader($"[{distance:F1}] - {name} [{obj.Type}]###{name}{id++}");
    }

    private void DrawWidgets(Vector3 playerPos, SpawnedObject obj, ref Transform transform)
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
                var collide = obj is {Type: ObjectType.SharedGroup, Group.Collide: true } && plugin.HyperEnabled;
                var clone = AnyderService.ObjectManager.Add(obj.Path, transformCopy.Position, transformCopy.Rotation, transformCopy.Scale, collide);
                clone.Name = obj.Name;
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
            plugin.Configuration.PathDictionary[obj.Path] = nickname;
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
