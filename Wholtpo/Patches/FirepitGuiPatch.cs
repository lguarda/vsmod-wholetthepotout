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

            // Ok so this put pack the lid by reseting client side render
            var rendererObj = firepit.GetType().GetField("renderer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(firepit) as FirepitContentsRenderer;
            if (rendererObj != null) {
                rendererObj.contentStackRenderer?.Dispose();
                rendererObj.contentStackRenderer = null;
                rendererObj.ContentStack = null;
            }
            typeof(BlockEntityFirepit).GetMethod("UpdateRenderer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(firepit, null);
        }
    }
}

// This is a little bit dumb but i don't know what else to patch client side
// on interact just wait 200ms then run the logic
[HarmonyPatch(typeof(BlockFirepit), "OnBlockInteractStart")]
public static class FirepitOnInteractFixPot {
    [HarmonyPostfix]
    public static void Postfix(BlockFirepit __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        var capi = typeof(BlockFirepit).GetField("capi", BindingFlags.NonPublic | BindingFlags.Instance)
        ?.GetValue(__instance) as ICoreClientAPI;
        if (capi == null) return;
        var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFirepit;
        if (be == null) return;
        be.RegisterDelayedCallback(dt => {
            FirepitPatchHelper.FixFirePitCookingPot(capi, be);
        }, 200);
    }
}

// Auto-swap empty cooking pot back to input slot when GUI is opened
[HarmonyPatch(typeof(BlockEntityOpenableContainer), "toggleInventoryDialogClient")]
public static class FirepitOnGuiFixPot {
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

        // Apparently the game client do something special when user have a crock pot/bowl in hand
        // sot first lets' switch the ActiveHotbarSlotNumber
        capi.World.Player.InventoryManager.ActiveHotbarSlotNumber = emptySlotId;

        // then in the main thread handle the logic
        capi.Event.EnqueueMainThreadTask(() => {

            // So fun fact it's not "open" but it work without it so let's not do this
            // capi.ShowChatMessage($"is already open?: {inv.HasOpened(byPlayer)}");
            // capi.Network.SendPacketClient(inv.Open(byPlayer));

            // so in BlockEntityOpenableContainer.cs packet 1000 is sent by toggleInventoryDialogClient
            // then receive by OnReceivedClientPacket in order to mark the inventory as "open" for the client
            // this is mandatory to stay synced with the server when doing the logic
            capi.Network.SendPacketClient(inv.Open(byPlayer));
            capi.Network.SendBlockEntityPacket(__instance.Pos, 1000, null);

            var packet = hotbar.TryFlipItems(emptySlotId, firepitSlot);

            if (packet != null) {
                capi.Network.SendPacketClient(packet);
            }

            capi.Network.SendBlockEntityPacket(__instance.Pos, 1001, null);
            capi.Network.SendPacketClient(inv.Close(byPlayer));
        }, "Wholtpo");

        // and return fols to prevent gui to be opend 
        return false;
    }
}
