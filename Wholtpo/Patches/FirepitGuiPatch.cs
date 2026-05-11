using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Reflection;

public static class FirepitPatchHelper {

    public const int VS_FIREPIT_FUEL_INDEX = 0;
    public const int VS_FIREPIT_OUTPUT_INDEX = 2;
    public const int VS_FIREPIT_RECIPIENT_INDEX = 1;

    public static readonly FieldInfo inventoryField =
        typeof(BlockEntityFirepit).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);

    public static ItemSlot DetectMealInFirepit(IInventory inv) {
        var potStack = inv[VS_FIREPIT_OUTPUT_INDEX].Itemstack;
        if (potStack != null && potStack.Collectible.GetType().Name == "BlockCookingContainer")
            return inv[VS_FIREPIT_OUTPUT_INDEX];
        return null;
    }

    public static ItemSlot DetectSmeltedContainer(IInventory inv) {
        var potStack = inv[VS_FIREPIT_OUTPUT_INDEX].Itemstack;
        if (potStack != null && potStack.Collectible.GetType().Name == "BlockSmeltedContainer")
            return inv[VS_FIREPIT_OUTPUT_INDEX];
        return null;
    }

    public static int FindEmptySlot(IInventory inv) {
        for (int i = 0; i < inv.Count; i++) {
            if (inv[i] != null && inv[i].Empty)
                return i;
        }
        return -1;
    }

    public static void FixFirePitCookingPot(ICoreClientAPI capi, BlockEntityFirepit firepit) {
        var inv = inventoryField?.GetValue(firepit) as IInventory;
        if (inv == null) {
            return;
        }

        if (!inv[1].Empty) {
            return;
        }

        ItemSlot slot = DetectMealInFirepit(inv);
        if (slot != null) {
            var packet = inv.TryFlipItems(VS_FIREPIT_RECIPIENT_INDEX, slot);
            if (packet != null)
                capi.Network.SendPacketClient(packet);
        }
    }
}

// Auto-swap empty cooking pot back to input slot when a slot changes
[HarmonyPatch(typeof(BlockEntityFirepit), "OnSlotModified")]
public static class FirepitSlotPatch {
    [HarmonyPostfix]
    public static void Postfix(BlockEntityFirepit __instance) {
        var capi = __instance.Api as ICoreClientAPI;
        if (capi == null)
            return;
        capi.Event.EnqueueMainThreadTask(() => { FirepitPatchHelper.FixFirePitCookingPot(capi, __instance); },
                                         "Wholtpo");
    }
}

// Auto-swap empty cooking pot back to input slot when GUI is opened
[HarmonyPatch(typeof(BlockEntityOpenableContainer), "toggleInventoryDialogClient")]
public static class FirepitGuiPatch {
    [HarmonyPostfix]
    public static void Postfix(BlockEntityOpenableContainer __instance) {
        if (__instance is not BlockEntityFirepit firepit)
            return;
        var capi = __instance.Api as ICoreClientAPI;
        if (capi == null)
            return;
        capi.Event.EnqueueMainThreadTask(() => { FirepitPatchHelper.FixFirePitCookingPot(capi, firepit); }, "Wholtpo");
    }
}

// Intercept right-click to take smelted crucible directly without opening GUI
[HarmonyPatch(typeof(BlockEntityFirepit), "OnPlayerRightClick")]
public static class FirepitRightClickPatch {
    [HarmonyPrefix]
    public static bool Prefix(BlockEntityFirepit __instance, IPlayer byPlayer) {
        var capi = __instance.Api as ICoreClientAPI;
        if (capi == null)
            return true;

        var inv = FirepitPatchHelper.inventoryField?.GetValue(__instance) as IInventory;
        if (inv == null)
            return true;

        if (capi.World.Player.InventoryManager.OffhandTool != EnumTool.Tongs)
            return true;

        ItemSlot firepitSlot = FirepitPatchHelper.DetectSmeltedContainer(inv);
        if (firepitSlot == null)
            return true;

        IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
        int emptySlotId = FirepitPatchHelper.FindEmptySlot(hotbar);
        if (emptySlotId < 0)
            return true;

        // So fun fact it's not "open" but it work without it so let's not do this
        // capi.ShowChatMessage($"is already open?: {inv.HasOpened(byPlayer)}");
        // capi.Network.SendPacketClient(inv.Open(byPlayer));

        // so in BlockEntityOpenableContainer.cs packet 1000 is sent by toggleInventoryDialogClient
        // then receive by OnReceivedClientPacket in order to mark the inventory as "open" for the client
        // this is mandatory to stay synced with the server when doing the logic
        capi.Network.SendBlockEntityPacket(__instance.Pos, 1000, null);

        var packet = hotbar.TryFlipItems(emptySlotId, firepitSlot);
        if (packet != null) {
            capi.World.Player.InventoryManager.ActiveHotbarSlotNumber = emptySlotId;
            capi.Network.SendPacketClient(packet);
            return false; // if we took the item, cancel GUI opening
        }

        capi.Network.SendBlockEntityPacket(__instance.Pos, 1001, null);

        return true;
    }
}
