using System;
using System.Numerics;
using System.Text;
using Buildingway.Utils.Interop.Structs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;

namespace Buildingway.Utils.Objects;

public unsafe class Group : IDisposable
{
    public SharedGroupLayoutInstance* Data;
    public string Path;
    
    public Transform Transform;
    public bool Collide;
    public byte Alpha = 0;
    
    public bool IsHovered = false;
    public byte HighlightColor = 70;

    public Group(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool collide = true)
    {
        Data = IMemorySpace.GetDefaultSpace()->Malloc<SharedGroupLayoutInstance>();
        Plugin.SharedGroupLayoutFunctions.Ctor(Data);
        
        Plugin.Log.Verbose($"Attempting to create group {path} @ {((IntPtr)Data):x8}");
        Path = path;

        Transform = new Transform()
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };

        Transform.OnUpdate += UpdateTransform;
        
        Collide = collide;
        
        Plugin.Framework.RunOnTick(SetModel);
    }

    private void SetModel()
    {
        Plugin.SharedGroupLayoutFunctions.LoadSgb(Data, Path);
        
        UpdateTransform();

        var first = (IntPtr)Data->Instances.Instances.First;
        var last = (IntPtr)Data->Instances.Instances.Last;
        
        if (first != last)
        {
            Plugin.SharedGroupLayoutFunctions.FixGroupChildren(Data);
        }
    }

    private void UpdateTransform()
    {
        var t = Data->GetTransformImpl();
        t->Translation = Transform.Position;
        t->Rotation = Transform.Rotation;
        t->Scale = Transform.Scale;

        Data->SetTransformImpl(t);
        Data->SetColliderActive(Collide);
    }

    public void SetAlpha(byte alpha)
    {
        Alpha = alpha;
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            var instance = ptr.Value->Instance;
            var graphics = (BgObjectEx*)instance->GetGraphics();
            if (graphics == null) continue;
            graphics->Alpha = alpha;
        }
    }

    public void SetHighlight(byte color)
    {
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            var instance = ptr.Value->Instance;
            if (instance->Id.Type != InstanceType.BgPart) continue;
            
            var graphics = (BgObjectEx*)instance->GetGraphics();
            var transform = instance->GetTransformImpl();
            if (graphics != null)
            {
                graphics->HighlightFlags = color;
            }
            instance->SetGraphics((Object*)graphics, transform);
        }
    }

    public void DrawInfo()
    {
        bool open = ImGui.CollapsingHeader("Debug Info");
        if (!open) return;

        if (ImGui.SliderByte($"Alpha##{((IntPtr)Data):X8}", ref Alpha, 0, 255))
        {
            SetAlpha(Alpha);
        }

        if (ImGui.InputByte("Highlght", ref HighlightColor))
        {
            SetHighlight(HighlightColor);
        }
        
#if DEBUG
        if (ImGui.Button("Copy Addr")) ImGui.SetClipboardText(((IntPtr)Data).ToString("X8"));
        if (ImGui.Button("Init Timelines"))
        {
            var first = (IntPtr)Data->Instances.Instances.First;
            var last = (IntPtr)Data->Instances.Instances.Last;
        
            if (first != last)
            {
                Plugin.SharedGroupLayoutFunctions.FixGroupChildren(Data);
            }
        }
#endif

        var id = 0;
        foreach (var ptr in Data->Instances.Instances)
        {
            if (ptr.Value == null) continue;
            ImRaii.PushId(id++);
            
            var instance = ptr.Value->Instance;
            ImGui.Text(instance->GetPrimaryPath().ToString());
            if (ImGui.Button("get ptr"))
            {
                // var type = instance->GetGraphics2();
                // if (type.FullName != null) Plugin.Log.Debug(type.FullName);
            }
            // if (ImGui.Button("Make alpha gooo"))
            // {
            //     var graphics = (BgObjectEx*)instance->GetGraphics();
            //     if (graphics == null) continue;
            //
            //     graphics->Alpha = 120;
            // }
        }
        
        //if (ImGui.Button("Update Render")) Data->
    }
    
    public void Dispose()
    {
        Plugin.Log.Verbose($"Disposing group {Path}");
        if (Data == null) return;
        
        Data->Deinit();
        Data->Dtor(0);
        
        IMemorySpace.Free(Data);
        
        Data = null;
    }
}
