namespace Buildingway.Commands;

public class Furniture(Plugin plugin) : ICommand
{
    public string Name => "Furniture";
    public string Description => "Open the furniture catalog.";
    public bool ShowInHelp => true;
    public int DisplayOrder => 1;
    public void Execute(string command, string args)
    {
        plugin.ToggleCatalogUi();
    }
    
    public void Dispose() { }
}
