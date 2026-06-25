using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Network;

namespace KefkaHelper.Services;

public unsafe class BattleProcessor : IDisposable
{
    public delegate void ActorCastDelegate(uint sourceId, ActorCastPacket data);
    public event ActorCastDelegate? OnActorCast;

    
    private delegate void ActorCastHookDelegate(uint sourceId, ActorCastPacket* packet);
    [Signature("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1", DetourName = nameof(ActorCastDetour))]
    private Hook<ActorCastHookDelegate>? _actorCastHook = null!;

    public BattleProcessor()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
        _actorCastHook?.Enable();
    }

    public void Dispose()
    {
        _actorCastHook?.Dispose();
    }

    private void ActorCastDetour(uint sourceId, ActorCastPacket* packet)
    {
        OnActorCast?.Invoke(sourceId, *packet);
        _actorCastHook!.Original(sourceId, packet);
    }
}
