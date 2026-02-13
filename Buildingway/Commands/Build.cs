namespace Buildingway.Commands;

public class Build(Plugin plugin) : ICommand
{
    public string Name => "Build";
    public string Description => "Open the main buildingway UI";
    public bool ShowInHelp => true;
    public int DisplayOrder => 0;
    
    public void Execute(string command, string args)
    {
        plugin.ToggleMainUi();
    }

    public void Dispose() { }
}
