using System;

namespace Buildingway.Commands;

public interface ICommand : IDisposable
{
    string Name { get; }
    string Description { get; }
    bool ShowInHelp { get; }
    int DisplayOrder { get; }
    
    void Execute(string command, string args);
}

