//using HarmonyLib;

using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using HarmonyLib;

public static class FirepitPatch
{

    private static readonly FieldInfo _inventoryField =
        typeof(BlockEntityFirepit).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);

    private static bool detectMealinFirepit(IInventory inv) {
        var potStack = inv[2].Itemstack;
        return potStack != null && potStack.Collectible.GetType().Name == "BlockCookingContainer";
    }

    private static void fixFirePitCookingPot(ICoreClientAPI capi) {
        var sel = capi.World.Player.CurrentBlockSelection;
        if (sel == null)
            return;
        capi.ShowChatMessage("OMG 1"); return;

        var be = capi.World.BlockAccessor.GetBlockEntity(sel.Position);

        if (be is not BlockEntityFirepit firepit)
            return;
        capi.ShowChatMessage("OMG 2"); return;

        var inv = _inventoryField?.GetValue(be) as IInventory;

        if (inv == null) {
            return;
        }
        capi.ShowChatMessage("OMG 3"); return;

        if (inv[1].Empty && detectMealinFirepit(inv)) {
            var packet = inv.TryFlipItems(1, inv[2]);
            capi.ShowChatMessage("OMG 4"); return;
            if (packet != null)
                capi.Network.SendPacketClient(packet);
        }
    }

    public static void SlotModifiedPostfix(BlockEntityFirepit __instance)
    {
        var capi = __instance.Api as ICoreClientAPI;
        if (capi == null) {
            capi.ShowChatMessage("OMG NO capi"); return;
            return;
        }

        //if (__instance.World.Side != EnumAppSide.Client) {
        //    capi.ShowChatMessage("OMG not client"); return;
        //}
        //
        capi.Event.EnqueueMainThreadTask(() => {
            fixFirePitCookingPot(capi);
        }, "firepitpotreturn");

        //fixFirePitCookingPot(capi);
        //capi.ShowChatMessage("OMG");
    }
}

