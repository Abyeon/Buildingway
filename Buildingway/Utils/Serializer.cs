using System;
using System.Text.Json;

namespace Buildingway.Utils;

public static class Serializer
{
    public static string SerializeCurrent()
    {
        return Serialize(new Layout(Plugin.ObjectManager));
    }
    
    public static string Serialize(Layout layout)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        return JsonSerializer.Serialize(layout, options);
    }

    public static Layout Deserialize(string json)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        
        return JsonSerializer.Deserialize<Layout>(json, options) ?? throw new InvalidOperationException("Could not deserialize layout.");
    }
}
