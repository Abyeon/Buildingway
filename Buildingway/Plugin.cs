using System.Collections.Generic;
using Buildingway.Commands;
// using Buildingway.Utils.Interop.Structs;
// using Buildingway.Utils.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Buildingway.Windows;
using ECommons;
using ECommons.Reflection;
using Anyder;
using Buildingway.Utils;

namespace Buildingway;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // internal static BgObjectFunctions BgObjectFunctions { get; private set; } = null!;
    // internal static VfxFunctions VfxFunctions { get; private set; } = null!;
    // internal static SharedGroupLayoutFunctions SharedGroupLayoutFunctions { get; private set; } = null!;

    internal static CommandHandler CommandHandler { get; private set; } = null!;
    // internal static ObjectManager ObjectManager { get; private set; } = null!;
    internal Configuration Configuration { get; init; }
    internal readonly WindowSystem WindowSystem = new("Buildingway");
    
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private CatalogWindow CatalogWindow { get; init; }
    private SavedPathsWindow SavedPathsWindow { get; init; }
    internal Overlay Overlay { get; init; }

    internal IDalamudPlugin Hyperborea = null!;

    internal bool Enabled { get; set; } = false;

    public Plugin()
    {
        AnyderService.Init(PluginInterface);
        ECommonsMain.Init(PluginInterface, this, Module.DalamudReflector);
        DalamudReflector.RegisterOnInstalledPluginsChangedEvents(PluginsChanged);
        
        var success = DalamudReflector.TryGetDalamudPlugin("Hyperborea", out var hyperborea);
        if (success)
        {
            Enabled = true;
            Hyperborea = hyperborea;
        }
        
        // BgObjectFunctions = new BgObjectFunctions();
        // VfxFunctions = new VfxFunctions();
        // SharedGroupLayoutFunctions = new SharedGroupLayoutFunctions();
        //
        // ObjectManager = new ObjectManager(this, ClientState, Framework);

        CommandHandler = new CommandHandler(this);
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        CatalogWindow = new CatalogWindow(this);
        SavedPathsWindow = new SavedPathsWindow(this);
        Overlay = new Overlay(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(CatalogWindow);
        WindowSystem.AddWindow(SavedPathsWindow);
        WindowSystem.AddWindow(Overlay);
        
        Overlay.Toggle();
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CatalogWindow.Dispose();
        SavedPathsWindow.Dispose();
        Overlay.Dispose();

        CommandHandler.Dispose();
        // ObjectManager.Dispose();
        // VfxFunctions.Dispose();
        
        AnyderService.Dispose();
        ECommonsMain.Dispose();

        placementQueue.Clear();
    }

    public void PluginsChanged()
    {
        Enabled = DalamudReflector.TryGetDalamudPlugin("Hyperborea", out var hyperborea);
        if (!Enabled)
        {
            AnyderService.ObjectManager.Clear();
        }
        else
        {
            Hyperborea = hyperborea;
        }
    }

    private Queue<Placement> placementQueue = new();
    
    public void LoadLayout(Layout layout)
    {
        placementQueue = new Queue<Placement>(layout.Placements);
        Framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (placementQueue.Count == 0)
        {
            Framework.Update -= OnUpdate;
            return;
        }
        
        var placement = placementQueue.Dequeue();
        AnyderService.ObjectManager.Add(placement.Path, placement.Position, placement.Rotation, placement.Scale, placement.Collision);
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
    public void ToggleCatalogUi() => CatalogWindow.Toggle();
    public void ToggleSavedPathsUi() => SavedPathsWindow.Toggle();
}
