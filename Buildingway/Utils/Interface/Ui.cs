using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace Buildingway.Utils.Interface;

/// <summary>
/// Provides custom ImGui components
/// </summary>
public static class Ui
{
    private static string Buf = "";

    /// <summary>
    /// Adds a popup that can be opened with ImGui.OpenPopup(id)
    /// Only returns true if the confirmation button was clicked.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="description">The text that appears inside the popup</param>
    /// <param name="input">Reference input to be updated</param>
    /// <param name="maxLength">Maximum length for the input</param>
    /// <param name="multiline">Whether the input should be multiline or not.</param>
    /// <returns>True if the input was updated</returns>
    public static bool AddTextConfirmationPopup(
        string id, string description, ref string input, int maxLength = 512, bool multiline = false)
    {
        using var popup = ImRaii.Popup(id);
        if (popup.Success)
        {
            ImGui.PushID(id);
            ImGui.Text(description);
            ImGui.Separator();
            
            if (multiline)
            {
                ImGui.InputTextMultiline("##textInput", ref Buf, maxLength, flags: ImGuiInputTextFlags.NoHorizontalScroll);
            }
            else
            {
                ImGui.InputText("##textInput", ref Buf, maxLength);
            }

            if (ImGui.Button("Confirm"))
            {
                input = Buf;
                Buf = "";
                ImGui.CloseCurrentPopup();
                return true;
            }

            ImGui.SameLine();
            if (RightAlignedButton("Cancel"))
            {
                Buf = "";
                ImGui.CloseCurrentPopup();
                return false;
            }
        }

        return false;
    }

    public static void RightAlignCursorForButton(ImU8String label)
    {
        var buttonSize = ImGui.CalcTextSize(label) + (ImGui.GetStyle().FramePadding * 2) + (ImGui.GetStyle().ItemSpacing * 2);
        var space = ImGui.GetContentRegionAvail().X - buttonSize.X;
        ImGui.Dummy(new Vector2(space, 0));
        ImGui.SameLine();
    }

    public static bool RightAlignedButton(ImU8String label)
    {
        RightAlignCursorForButton(label);
        return ImGui.Button(label);
    }

    public static bool CtrlButton(ImU8String label, string hoverLabel = "Hold Ctrl to enable.", Vector2 size = default)
    {
        var ctrl = ImGui.GetIO().KeyCtrl;
        using var _ = ImRaii.Disabled(!ctrl);
        if (ImGui.Button(label, size))
        {
            return true;
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(hoverLabel);
        }

        return false;
    }
    
    public static bool CtrlSelectable(ImU8String label, string hoverLabel = "Hold Ctrl to enable.")
    {
        var ctrl = ImGui.GetIO().KeyCtrl;
        using var _ = ImRaii.Disabled(!ctrl);
        if (ImGui.Selectable(label))
        {
            return true;
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(hoverLabel);
        }

        return false;
    }

    public static void CenteredTextWithLine(uint textColor, ImU8String text, uint lineColor, float padding = 5f)
    {
        var draw = ImGui.GetWindowDrawList();
        ImGuiHelpers.CenterCursorForText(text);
        
        var leftOfText = new Vector2
        {
            X = ImGui.GetCursorScreenPos().X - padding,
            Y = ImGui.GetCursorScreenPos().Y + (ImGui.GetTextLineHeight() * .5f)
        };
        
        ImGui.TextColored(textColor, text);
        
        var rightOfText = leftOfText with
        {
            X = leftOfText.X + ImGui.CalcTextSize(text).X + (padding * 2)
        };
        
        var width = ImGui.GetWindowWidth();
        
        draw.AddLine(leftOfText, leftOfText with { X = leftOfText.X - width }, lineColor);
        draw.AddLine(rightOfText, rightOfText with { X = rightOfText.X + width }, lineColor);
    }
    
    public static void CenteredTextWithLine(ImU8String text, uint lineColor, float padding = 5f)
    {
        var textColor = ImGui.GetColorU32(ImGuiCol.Text);
        CenteredTextWithLine(textColor, text, lineColor, padding);
    }

    // Straight yoinked from Chat2
    // https://github.com/Infiziert90/ChatTwo/blob/c54efe542012ec8891f71b87083a658c3aad9df9/ChatTwo/Util/ImGuiUtil.cs#L275
    public static SingleFontChooserDialog? FontChooser( string label, SingleFontSpec font, Predicate<IFontFamilyId>? exclusion = null, string? preview = null)
    {
        using var id = ImRaii.PushId(label);

        var locale = Plugin.ClientState.ClientLanguage.ToCode();
        var fontFamily = font.FontId.Family.GetLocalizedName(locale);
        var fontStyle = font.FontId.GetLocalizedName(locale);
        fontStyle = fontStyle.Equals(fontFamily) ? "" : $" - {fontStyle}";

        var buttonText = $"{fontFamily}{fontStyle} ({font.SizePt}pt)";
        if (!ImGui.Button($"{buttonText}##{label}"))
            return null;

        var chooser = SingleFontChooserDialog.CreateAuto((UiBuilder)Plugin.PluginInterface.UiBuilder);
        chooser.SelectedFont = font;
        if (exclusion is not null)
            chooser.FontFamilyExcludeFilter = exclusion;
        if (preview is not null)
            chooser.PreviewText = preview;

        return chooser;
    }
    
    private static unsafe void SetHovered(string id, bool hovered)
    {
        var storage = ImGuiNative.GetStateStorage();
        var key = ImGui.GetID(id);
        ImGuiNative.SetBool(storage, key, Convert.ToByte(hovered));
    }

    public static unsafe bool Hovered(string id)
    {
        var storage = ImGuiNative.GetStateStorage();
        var key = ImGui.GetID(id);
        return Convert.ToBoolean(ImGuiNative.GetBool(storage, key, Convert.ToByte(false)));
    }

    public static Vector4 GetColorVec4(ImGuiCol idx)
    {
        var col = ImGui.GetStyle().Colors[(int)idx];
        col.W *= ImGui.GetStyle().Alpha;
        return col;
    }

    public class PushFont : IDisposable
    {
        public IFontHandle? FontHandle { get; private set; }
        private readonly bool pushed = false;

        public PushFont(IFontHandle? fontHandle)
        {
            FontHandle = fontHandle;

            if (FontHandle is { Available: true })
            {
                FontHandle.Push();
                pushed = true;
            }
        }
        
        public void Dispose()
        {
            if (FontHandle is not null && pushed)
            {
                FontHandle.Pop();
            }
            
            GC.SuppressFinalize(this);
        }
    }
    
    /// <summary>
    /// Using this class will wrap any item in this context with a disabled ImGui.Selectable node which will display if the user hovers it.
    /// This will split the current ImGui DrawList into 2 channels and default to the front one.
    /// </summary>
    public class Hoverable : IDisposable
    {
        public Vector2 StartPos { get; private set; }
        public Vector2 EndPos { get; private set; }
        public Vector2 Margin { get; init; }
        public Vector2 Padding { get; init; }
        public float Rounding { get; init; }
        public bool Highlight { get; init; }
        public ImGuiSelectableFlags Flags { get; init; }
        
        public string Id { get; private set; }

        private ImDrawListPtr draw;

        public Hoverable(string id)
        {
            Id = id;
            Margin = Vector2.Zero;
            Padding = new Vector2(5f, 2f);
            Rounding = 5f;
            Highlight = false;

            Begin();
        }
        
        public Hoverable(string id, float rounding = 5f, Vector2 margin = default(Vector2), Vector2 padding = default(Vector2), bool highlight = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
        {
            Id = id;
            Margin = margin;
            Padding = padding;
            Rounding = rounding;
            Highlight = highlight;
            Flags = flags;
            
            Begin();
        }

        private void Begin()
        {
            StartPos = ImGui.GetCursorScreenPos();
            
            draw = ImGui.GetWindowDrawList();

            ImGui.ChannelsSplit(draw, 2);
            draw.ChannelsSetCurrent(1);
            
            ImGui.BeginGroup();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + Padding.Y);
            ImGui.Indent(Padding.X);
        }

        public void Dispose()
        {
            ImGui.Unindent();
            ImGui.EndGroup();
            
            EndPos = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(StartPos);
            
            draw.ChannelsSetCurrent(0); // Set the channel to the background

            using (ImRaii.Disabled())
            {
                ImGui.Selectable($"###{Id}", false, Flags, EndPos - StartPos);
                //ImGui.Button($"###{Id}", EndPos - StartPos);
            }

            var min = StartPos + Margin with { Y = Margin.X };
            var max = EndPos + Margin with { X = Margin.Y + ImGui.GetContentRegionAvail().X };
            var color = ImGui.GetColorU32(ImGuiCol.FrameBg, 0.25f);
            var color1 = ImGui.GetColorU32(ImGuiCol.FrameBg, 0f); // Used for gradient
            var lineColor = ImGui.GetColorU32(ImGuiCol.Tab);
            
            if (ImGui.IsMouseHoveringRect(min, max) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                color = ImGui.GetColorU32(ImGuiCol.FrameBgHovered);
                color1 = ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0f);
                lineColor = ImGui.GetColorU32(ImGuiCol.TabActive);
                SetHovered(Id, true);
            }
            else
            {
                SetHovered(Id, false);
            }
            
            if (Highlight)
            {
                draw.AddRectFilledMultiColor(min, max, color, color1, color1, color);
                draw.AddLine(min, EndPos, lineColor);
            }
            else
            {
                draw.AddRectFilled(min, max, color, Rounding, ImDrawFlags.None);
            }
            
            ImGui.SetCursorScreenPos(EndPos);

            draw.ChannelsSetCurrent(1);
            ImGui.ChannelsMerge(draw);
            GC.SuppressFinalize(this);
        }
    }
}
