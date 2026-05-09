using HarmonyLib;
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

//public static class FirepitPatch {
//
//    private static readonly FieldInfo _inventoryField =
//        typeof(BlockEntityFirepit).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);
//
//    private static bool detectMealinFirepit(IInventory inv) {
//        var potStack = inv[2].Itemstack;
//        return potStack != null && potStack.Collectible.GetType().Name == "BlockCookingContainer";
//    }
//
//    private static void fixFirePitCookingPot(ICoreClientAPI capi, BlockEntityFirepit firepit) {
//        var inv = _inventoryField?.GetValue(firepit) as IInventory;
//        if (inv == null)
//            return;
//        if (inv[1].Empty && detectMealinFirepit(inv)) {
//            var packet = inv.TryFlipItems(1, inv[2]);
//            if (packet != null)
//                capi.Network.SendPacketClient(packet);
//        }
//    }
//
//    [HarmonyPatch(typeof(BlockEntityFirepit), "OnSlotModified")]
//    public static void SlotModifiedPostfix(BlockEntityFirepit __instance) {
//        var capi = __instance.Api as ICoreClientAPI;
//        if (capi == null) {
//            return;
//        }
//
//        capi.Logger.Error("OMG");
//        capi.Event.EnqueueMainThreadTask(() => { fixFirePitCookingPot(capi, __instance); }, "Wholtpo");
//    }
//
//    [HarmonyPatch(typeof(BlockEntityOpenableContainer), "toggleInventoryDialogClient")]
//    public static void Postfix(BlockEntityOpenableContainer __instance) {
//        if (__instance is not BlockEntityFirepit firepit)
//            return;
//        var capi = __instance.Api as ICoreClientAPI;
//
//        // This may be on server side?
//        if (capi == null)
//            return;
//
//        capi.Logger.Error("OMG");
//        capi.Event.EnqueueMainThreadTask(() => { fixFirePitCookingPot(capi, firepit); }, "Wholtpo");
//    }
//}

public static class FirepitPatchHelper {
    private static readonly FieldInfo _inventoryField =
        typeof(BlockEntityFirepit).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);

    public static bool DetectMealInFirepit(IInventory inv) {
        var potStack = inv[2].Itemstack;
        return potStack != null && potStack.Collectible.GetType().Name == "BlockCookingContainer";
    }

    public static void FixFirePitCookingPot(ICoreClientAPI capi, BlockEntityFirepit firepit) {
        var inv = _inventoryField?.GetValue(firepit) as IInventory;
        if (inv == null) return;
        if (inv[1].Empty && DetectMealInFirepit(inv)) {
            var packet = inv.TryFlipItems(1, inv[2]);
            if (packet != null)
                capi.Network.SendPacketClient(packet);
        }
    }
}

[HarmonyPatch(typeof(BlockEntityFirepit), "OnSlotModified")]
public static class FirepitSlotPatch {
    [HarmonyPostfix]
    public static void Postfix(BlockEntityFirepit __instance) {
        var capi = __instance.Api as ICoreClientAPI;
        if (capi == null) {
            return;
        }

        capi.Event.EnqueueMainThreadTask(() => { FirepitPatchHelper.FixFirePitCookingPot(capi, __instance); }, "Wholtpo");
    }
}

[HarmonyPatch(typeof(BlockEntityOpenableContainer), "toggleInventoryDialogClient")]
public static class FirepitGuiPatch {
    [HarmonyPostfix]
    public static void Postfix(BlockEntityOpenableContainer __instance) {
        if (__instance is not BlockEntityFirepit firepit)
            return;
        var capi = __instance.Api as ICoreClientAPI;

        // This may be on server side?
        if (capi == null)
            return;

        capi.Event.EnqueueMainThreadTask(() => { FirepitPatchHelper.FixFirePitCookingPot(capi, firepit); }, "Wholtpo");
    }
}
