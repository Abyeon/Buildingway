using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Buildingway.Windows;

public class CatalogWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;

    private List<FurnitureCatalogCategory> categories = [];
    private readonly Dictionary<uint, List<HousingFurniture>> furnitureDict = new();

    private List<HousingFurniture> allFurniture = [];

    private bool built; // if the furniture dict is built

    public CatalogWindow(Plugin plugin) : base("Furniture Catalog##BuildingwayCatalog")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;

        built = false;

        var furnitureSheet = Plugin.DataManager.GetExcelSheet<HousingFurniture>();
        var catalogItemSheet = Plugin.DataManager.GetExcelSheet<FurnitureCatalogItemList>();

        BuildCategories(furnitureSheet, catalogItemSheet);
    }

    private async void BuildCategories(ExcelSheet<HousingFurniture> furnitureSheet, ExcelSheet<FurnitureCatalogItemList> catalogItemSheet)
    {
        try
        {
            await Task.Run(() =>
            {
                var watch = Stopwatch.StartNew();
                Plugin.Log.Debug("Building catalog...");
                
                allFurniture = furnitureSheet.Where(x => !x.Item.Value.Name.IsEmpty).OrderBy(x => x.Item.Value.Name.ToString()).ToList();

                categories = Plugin.DataManager.GetExcelSheet<FurnitureCatalogCategory>()
                                   .GroupBy(x => x.Category)
                                   .Select(g => g.First())
                                   .OrderBy(x => x.Category.ToString()).ToList();

                // Build furniture dict
                Parallel.ForEach(categories, row =>
                {
                    var itemRows = catalogItemSheet
                                   .Where(x => x.Category.Value.Category.ToString() == row.Category.ToString())
                                   .Select(x => x.Item.RowId);
                    
                    var list = furnitureSheet.Where(x => itemRows.Contains(x.Item.RowId) && !x.Item.Value.Name.IsEmpty)
                                             .OrderBy(x => x.Item.Value.Name.ToString()).ToList();
                    
                    furnitureDict[row.RowId] = list;
                });
                
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

    private FurnitureCatalogCategory? selectedCategory = null;
    
    protected override void Render()
    {
        if (!built)
        {
            ImGui.Text("Currently building catalog, please wait!");
            return;
        }
        
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

        var list = selectedCategory == null ? allFurniture : furnitureDict[selectedCategory.Value.RowId];
        
        foreach (var furniture in list)
        {
            if (ImGui.Selectable(furniture.Item.Value.Name.ToString()))
            {
                var model = furniture.ModelKey.ToString("0000");
                var path = $"bgcommon/hou/indoor/general/{model}/asset/fun_b0_m{model}.sgb";
                Plugin.ObjectManager.Add(path, player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
            }
        }
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
        
        foreach (var category in categories)
        {
            ImGui.PushID(id++);
            var name = category.Category.ToString();
            if (ImGui.Selectable(name, name == selected))
            {
                selectedCategory = category;
            }
        }
    }

    public void Dispose() { }
}
