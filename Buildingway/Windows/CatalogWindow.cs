using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Anyder;
using Buildingway.Utils;
using Buildingway.Utils.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace Buildingway.Windows;

public class CatalogWindow : CustomWindow, IDisposable
{
    private readonly Plugin plugin;

    private readonly ExcelSheet<FurnitureCatalogCategory> indoorCategories;
    private readonly ExcelSheet<YardCatalogCategory> outdoorCategories;

    private List<Furnishing> indoorFurniture  = [];
    private List<Furnishing> outdoorFurniture = [];
    private List<Furnishing> currentSearch = [];

    private readonly Dictionary<uint, string> placementTypes = new()
    {
        { 12, "Indoor Furnishings" },
        { 13, "Tables" },
        { 14, "Tabletop" },
        { 15, "Wall-mounted" },
        { 16, "Rugs" },
        // { 17, "Outdoor Furnishings" }
    };

    private bool built; // if the furniture dict is built

    public CatalogWindow(Plugin plugin) : base("Furniture Catalog##BuildingwayCatalog")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        
        indoorCategories = Plugin.DataManager.GetExcelSheet<FurnitureCatalogCategory>();
        outdoorCategories = Plugin.DataManager.GetExcelSheet<YardCatalogCategory>();

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
                    if (!indoorCategories.TryGetRow(category, out var categoryRow)) continue;
                    
                    indoorFurniture.Add(new Furnishing
                    {
                        Name = furniture.Item.Value.Name.ToString(),
                        Model = furniture.ModelKey,
                        Category = category,
                        Subcategory = categoryRow.Unknown0,
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
                        Subcategory = 17,
                        Indoors = false
                    });
                }

                outdoorFurniture = outdoorFurniture.OrderBy(x => x.Name).ToList();
                UpdateSearch();
                
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
    private uint selectedSubcategory = 0;
    private uint? selectedCategory;
    private string query = "";

    private void UpdateSearch()
    {
        IEnumerable<Furnishing> list = indoors ? indoorFurniture : outdoorFurniture;
        if (selectedCategory != null) list = list.Where(x => x.Category == selectedCategory);
        if (selectedCategory == null && selectedSubcategory != 0) list = list.Where(x => x.Subcategory == selectedSubcategory);
        if (query != "") list = list.Where(x => x.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        currentSearch = list.OrderBy(x => x.Name).ToList();
    }
    
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
            selectedSubcategory = 0;
            UpdateSearch();
        }
        
        if (ImGui.InputText("Search", ref query))
        {
            UpdateSearch();
        }
        
        DrawSubcategories();
        ImGuiComponents.HelpMarker("These are the filters listed at the top of the \"Indoor Furnishings\" menu.");
        
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

        using var table = ImRaii.Table("##ItemTable", 2, ImGuiTableFlags.SizingFixedFit);
        if (!table.Success) return;

        uint id = 0;
        foreach (var furniture in currentSearch)
        {
            ImGui.PushID(id++);
            ImGui.TableNextRow();
            
            ImGui.TableNextColumn();
            if (ImGui.Selectable(furniture.Name, flags: ImGuiSelectableFlags.SpanAllColumns))
            {
                AnyderService.ObjectManager.Add(furniture.GetPath(), player.Position, Quaternion.CreateFromYawPitchRoll(player.Rotation, 0, 0), collide: plugin.Configuration.SpawnWithCollision);
            }
            ImGui.TableNextColumn();
            ImGui.Text(furniture.GetPath());
        }
    }

    private void DrawSubcategories()
    {
        uint id = 0;
        var subName = "All";
        if (selectedSubcategory != 0)
        {
            subName = placementTypes[selectedSubcategory];
        }
        
        using var popup = ImRaii.Combo("Placement", subName);
        if (!popup.Success) return;
        
        ImGui.PushID(id++);
        if (ImGui.Selectable("All", selectedSubcategory == 0))
        {
            selectedSubcategory = 0;
            UpdateSearch();
        }

        if (!indoors) return;
        foreach (var pair in placementTypes)
        {
            ImGui.PushID(id++);
            var key = pair.Key;
            var name = pair.Value;

            if (ImGui.Selectable(name, key == selectedSubcategory))
            {
                selectedSubcategory = key;
                selectedCategory = null;
                UpdateSearch();
            }
        }
    }

    private void DrawCategories()
    {
        uint id = 0;

        var categoryName = "All";
        if (selectedCategory != null)
        {
            if (indoors)
            {
                var row = indoorCategories.GetRow(selectedCategory.Value);
                categoryName = $"{row.Category} ({placementTypes[row.Unknown0]})";
            }
            else
            {
                categoryName = outdoorCategories.GetRow(selectedCategory.Value).Category.ToString();
            }
        }
        
        using var popup = ImRaii.Combo("Category", categoryName);

        if (!popup.Success) return;
        
        ImGui.PushID(id++);
        if (ImGui.Selectable("All", selectedCategory == null))
        {
            selectedCategory = null;
            UpdateSearch();
        }

        if (indoors)
        {
            foreach (var category in indoorCategories)
            {
                if (selectedSubcategory != 0 && category.Unknown0 != selectedSubcategory) continue;
                
                ImGui.PushID(id++);
                var name = $"{category.Category} ({placementTypes[category.Unknown0]})";
                if (ImGui.Selectable(name, category.RowId == selectedCategory))
                {
                    selectedCategory = category.RowId;
                    UpdateSearch();
                }
            }
        }
        else
        {
            foreach (var category in outdoorCategories)
            {
                ImGui.PushID(id++);
                var name = category.Category.ToString();
                if (ImGui.Selectable(name, category.RowId == selectedCategory))
                {
                    selectedCategory = category.RowId;
                    UpdateSearch();
                }
            }
        }
    }

    public void Dispose() { }
}

public struct Furnishing
{
    public string Name;
    public uint Model;
    public uint Category;
    public uint Subcategory;
    public bool Indoors;

    public string GetPath()
    {
        var model = Model.ToString("0000");
        var location = Indoors ? "indoor" : "outdoor";
        var funGar = Indoors ? "fun" : "gar";
        return $"bgcommon/hou/{location}/general/{model}/asset/{funGar}_b0_m{model}.sgb";
    }
}
