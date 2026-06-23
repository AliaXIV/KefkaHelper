using System;
using System.Runtime.InteropServices;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace KefkaHelper;

public unsafe class CommandExecutor
{
    public delegate void ProcessChatBoxDelegate(UIModule* uiModule, nint message, nint unused, byte a4);

    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9")]
    public static ProcessChatBoxDelegate ProcessChatBox;


    public static void ExecuteCommand(string text)
    {
        var uiModule = Framework.Instance()->GetUIModule();

        using var payload = new ChatPayload(text);

        var strMemory = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(payload, strMemory, false);
        ProcessChatBox(uiModule, strMemory, IntPtr.Zero, 0);
        Marshal.FreeHGlobal(strMemory);
    }
}
