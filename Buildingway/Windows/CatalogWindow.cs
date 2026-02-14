using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace Buildingway.Windows;

public class CatalogWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;

    private ExcelSheet<FurnitureCatalogCategory> indoorCatagories;
    private ExcelSheet<YardCatalogCategory> outdoorCatagories;

    private List<Furnishing> indoorFurniture  = [];
    private List<Furnishing> outdoorFurniture = [];

    private bool built; // if the furniture dict is built

    public CatalogWindow(Plugin plugin) : base("Furniture Catalog##BuildingwayCatalog")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        
        indoorCatagories = Plugin.DataManager.GetExcelSheet<FurnitureCatalogCategory>();
        outdoorCatagories = Plugin.DataManager.GetExcelSheet<YardCatalogCategory>();

        built = false;
        BuildCategories();
    }

    private async void BuildCategories()
    {
        var indoorSheet = Plugin.DataManager.GetExcelSheet<HousingFurniture>();
        var outdoorSheet = Plugin.DataManager.GetExcelSheet<HousingYardObject>();
                
        var indoorCatalog = Plugin.DataManager.GetExcelSheet<FurnitureCatalogItemList>();
        var outdoorCatalog = Plugin.DataManager.GetExcelSheet<YardCatalogItemList>();
        
        try
        {
            await Task.Run(() =>
            {
                var watch = Stopwatch.StartNew();
                Plugin.Log.Debug("Building catalog...");

                indoorFurniture = [];
                
                foreach (var furniture in indoorSheet)
                {
                    if (furniture.Item.Value.Name.IsEmpty) continue;

                    var row = indoorCatalog.FirstOrNull(x => x.Item.RowId == furniture.Item.RowId);
                    if (row == null) continue;
                    
                    var category = row.Value.Category.RowId;
                    
                    indoorFurniture.Add(new Furnishing
                    {
                        Name = furniture.Item.Value.Name.ToString(),
                        Model = furniture.ModelKey,
                        Category = category,
                        Indoors = true
                    });
                }

                outdoorFurniture = [];
                
                foreach (var furniture in outdoorSheet)
                {
                    if (furniture.Item.Value.Name.IsEmpty) continue;
                    
                    var row = outdoorCatalog.FirstOrNull(x => x.Item.RowId == furniture.Item.RowId);
                    if (row == null) continue;
                    
                    var category = row.Value.Category.RowId;
                    
                    outdoorFurniture.Add(new Furnishing
                    {
                        Name = furniture.Item.Value.Name.ToString(),
                        Model = furniture.ModelKey,
                        Category = category,
                        Indoors = false
                    });
                }

                outdoorFurniture = outdoorFurniture.OrderBy(x => x.Name).ToList();
                
                built = true;
                watch.Stop();
                Plugin.Log.Debug($"Built catalog after {watch.ElapsedMilliseconds} ms");
            });
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.ToString());
        }
    }

    private bool indoors = true;
    private FurnitureCatalogCategory? selectedCategory;
    private string query = "";
    
    protected override void Render()
    {
        if (!built)
        {
            ImGui.Text("Currently building catalog, please wait!");
            return;
        }

        if (ImGui.Button(indoors ? "Show Outdoors" : "Show Indoors"))
        {
            indoors = !indoors;
            selectedCategory = null;
        }
        
        ImGui.InputText("Search", ref query);
        DrawCategories();
        
        var collision = plugin.Configuration.SpawnWithCollision;
        if (ImGui.Checkbox("Spawn with collision", ref collision))
        {
            plugin.Configuration.SpawnWithCollision = collision;
            plugin.Configuration.Save();
        }
        
        ImGui.Spacing();

        using var child = ImRaii.Child("##ItemChild");
        if (!child.Success) return;
        
        DrawItems();
    }

    private void DrawItems()
    {
        if (Plugin.ObjectTable.LocalPlayer == null) return;
        var player = Plugin.ObjectTable.LocalPlayer;

        var list = indoors ? indoorFurniture : outdoorFurniture;
        if (query != "") list = list.Where(x => x.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();

        using var table = ImRaii.Table("##ItemTable", 3, ImGuiTableFlags.SizingFixedFit);
        if (!table.Success) return;

        uint id = 0;
        foreach (var furniture in list)
        {
            ImGui.PushID(id++);
            ImGui.TableNextRow();
            
            ImGui.TableNextColumn();
            if (ImGui.Selectable(furniture.Name, flags: ImGuiSelectableFlags.SpanAllColumns))
            {
                Plugin.ObjectManager.Add(furniture.GetPath(), player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
            }
            ImGui.TableNextColumn();
            ImGui.Text(furniture.GetPath());
            // ImGui.TableNextColumn();
            // ImGui.Text(furniture.Indoors ? "indoor" : "outdoor" );
        }

        // foreach (var furniture in list)
        // {
        //     if (ImGui.Selectable(furniture.Name))
        //     {
        //         Plugin.ObjectManager.Add(furniture.GetPath(), player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
        //     }
        // }
    }

    private void DrawCategories()
    {
        uint id = 0;

        var selected = selectedCategory == null ? "All" : selectedCategory.Value.Category.ToString();
        using var popup = ImRaii.Combo("Category", selected);

        if (!popup.Success) return;
        
        ImGui.PushID(id++);
        if (ImGui.Selectable("All", selectedCategory == null))
        {
            selectedCategory = null;
        }

        if (indoors)
        {
            foreach (var category in indoorCatagories)
            {
                ImGui.PushID(id++);
                var name = category.Category.ToString();
                if (ImGui.Selectable(name, name == selected))
                {
                    selectedCategory = category;
                }
            }
        }
        // else
        // {
        //     foreach (var category in outdoorCatagories)
        //     {
        //         ImGui.PushID(id++);
        //         var name = category.Category.ToString();
        //         if (ImGui.Selectable(name, name == selected))
        //         {
        //             selectedCategory = category;
        //         }
        //     }
        // }
    }

    public void Dispose() { }
}

public struct Furnishing
{
    public string Name;
    public uint Model;
    public uint Category;
    public bool Indoors;

    public string GetPath()
    {
        var model = Model.ToString("0000");
        var location = Indoors ? "indoor" : "outdoor";
        var funGar = Indoors ? "fun" : "gar";
        return $"bgcommon/hou/{location}/general/{model}/asset/{funGar}_b0_m{model}.sgb";
    }
}
