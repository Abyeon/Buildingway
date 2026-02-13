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

    private bool built; // if the furniture dict is built

    public CatalogWindow(Plugin plugin) : base("Furniture Catalog##Buildingway")
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

                categories = Plugin.DataManager.GetExcelSheet<FurnitureCatalogCategory>()
                                   .GroupBy(x => x.Category)
                                   .Select(g => g.First())
                                   .OrderBy(x => x.Category.ToString()).ToList();

                selectedCategory = categories.First();

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

    private FurnitureCatalogCategory selectedCategory;
    
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
        
        var list = furnitureDict[selectedCategory.RowId];
        foreach (var furniture in list)
        {
            if (ImGui.Selectable(furniture.Item.Value.Name.ToString()))
            {
                string model = furniture.ModelKey.ToString("0000");
                string path = $"bgcommon/hou/indoor/general/{model}/asset/fun_b0_m{model}.sgb";
                Plugin.ObjectManager.Add(path, player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
            }
        }
    }

    private void DrawCategories()
    {
        uint id = 0;
        using var popup = ImRaii.Combo("Category", selectedCategory.Category.ToString());

        if (!popup.Success) return;
        
        foreach (var category in categories)
        {
            ImGui.PushID(id++);
            var name = category.Category.ToString();
            if (ImGui.Selectable(name, name == selectedCategory.Category.ToString()))
            {
                selectedCategory = category;
            }
        }
    }

    public void Dispose() { }
}
