using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Lumina.Excel;

namespace KefkaHelper.Windows;

public class ForsakenWindow : Window, IDisposable
{
    public bool IsForsakenActive = false;
    public ForsakenMechanicType Mechanic = ForsakenMechanicType.Stack;
    public uint? LastEndCastId = null;
    public bool IsPreview = false;

    private ExcelSheet<Lumina.Excel.Sheets.Action> ActionSheet;

    public ForsakenWindow(Plugin plugin) : base("Forsaken###KefkaForsaken")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground;

        Size = new Vector2(120, 120);
        SizeCondition = ImGuiCond.Always;

        ActionSheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
    }

    public void Dispose() { }

    public override void Draw()
    {
        const uint spellsTroubleIconId = 218951;
        
        var iconId = IsForsakenActive ? GetIconForForsaken(Mechanic) : spellsTroubleIconId;
        var castName = LastEndCastId.HasValue ? ActionSheet.GetRow(LastEndCastId.Value).Name.ExtractText() : "---";
        
        var texture = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId, true))
                            .GetWrapOrEmpty();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        
        var iconOffset = (availableWidth - texture.Width) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + iconOffset);
        ImGui.Image(texture.Handle, texture.Size);
        
        var textSize = ImGui.CalcTextSize(castName);
        var textOffset = (availableWidth - textSize.X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + textOffset);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.1f, 0.8f));
        ImGui.BeginChild("TextBg", new Vector2(textSize.X, textSize.Y));
        ImGui.Text(castName);
        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static uint GetIconForForsaken(ForsakenMechanicType mechanic)
    {
        return mechanic switch
        {
            ForsakenMechanicType.Stack => 215969,
            ForsakenMechanicType.Circle => 215996,
            ForsakenMechanicType.Cone => 215967,
            _ => 218951
        };
    }
}
