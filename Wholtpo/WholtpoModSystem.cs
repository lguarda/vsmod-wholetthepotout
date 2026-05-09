using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Wholtpo {

public class WholtpoModSystem : ModSystem {

    ICoreClientAPI _capi;

    public void log(string message) {
        _capi.ShowChatMessage(message);
        _capi.Logger.Debug(message);
    }

    private HashSet<string> GetChestItemCodes(IInventory chestInventory)
    {
        var codes = new HashSet<string>();
        foreach (var slot in chestInventory)
            if (slot.Itemstack != null)
                codes.Add(slot.Itemstack.Collectible.Code.ToString());
        return codes;
    }

    private bool ShouldTransfer(ItemSlot playerSlot, HashSet<string> chestCodes)
    {
        if ((playerSlot.Itemstack?.Collectible?.MaxStackSize ?? 1) <= 1) return false;
        if (playerSlot.Itemstack == null) return false;
        return chestCodes.Contains(playerSlot.Itemstack.Collectible.Code.ToString());
    }

    private void TransferSlot(ItemSlot playerSlot, ItemSlot chestSlot)
    {
        var op = new ItemStackMoveOperation(_capi.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, playerSlot.StackSize);
        op.ActingPlayer = _capi.World.Player;

        var packet = _capi.World.Player.InventoryManager.TryTransferTo(playerSlot, chestSlot, ref op);
        if (packet != null)
            _capi.Network.SendPacketClient(packet);
    }

    //private void QuickStackToChest(IInventory chestInventory, IInventory playerInventory)
    //{
    //    var chestCodes = GetChestItemCodes(chestInventory);
    //    var firstSlot = playerInventory.FirstOrDefault(s => ShouldTransfer(s, chestCodes));
    //    if (firstSlot == null) return;
    //    TransferSlot(firstSlot);
    //}

    private void QuickStackToChest(IInventory chestInventory, IInventory playerInventory)
    {
        var chestCodes = GetChestItemCodes(chestInventory);

        //foreach (var playerSlot in playerInventory)
        //{
        //    if (!ShouldTransfer(playerSlot, chestCodes)) continue;

        //    // find matching chest slot
        //    foreach (var chestSlot in chestInventory)
        //    {
        //        if (chestSlot.Itemstack?.Collectible?.Code?.ToString() != 
        //            playerSlot.Itemstack?.Collectible?.Code?.ToString()) continue;

        //        TransferSlot(playerSlot, chestSlot);
        //        if (playerSlot.Itemstack == null) break; // fully transferred
        //    }
        //}

        foreach (var playerSlot in playerInventory)
        {
            if (!ShouldTransfer(playerSlot, chestCodes)) continue;
            
            // pass 1: try to merge into existing stacks
            foreach (var chestSlot in chestInventory)
            {
                if (playerSlot.Itemstack == null) break;
                if (chestSlot.Itemstack?.Collectible?.Code?.ToString() != 
                    playerSlot.Itemstack?.Collectible?.Code?.ToString()) continue;
                TransferSlot(playerSlot, chestSlot);
            }
            
            // pass 2: fall back to empty slot if still has items
            if (playerSlot.Itemstack == null) continue;
            foreach (var chestSlot in chestInventory)
            {
                if (playerSlot.Itemstack == null) break;
                if (chestSlot.Itemstack != null) continue; // skip non-empty
                TransferSlot(playerSlot, chestSlot);
            }
        }
    }

    //public void toto()
    //    var op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge);
    //    op.ActingPlayer = capi.World.Player;
    //    op.ShiftDown = true;

    //    foreach (var playerSlot in playerInventory)
    //    {
    //        if (playerSlot.Itemstack == null) continue;
    //        var packet = chestInventory.ActivateSlot(slotId, playerSlot, ref op);
    //        if (packet != null)
    //            capi.Network.SendPacketClient(packet);
    //    }
    //}

    public void omg()
    {
        var plrPos = _capi.World.Player.Entity.Pos.AsBlockPos;
        var openedChests = new List<BlockEntityGenericTypedContainer>();

        for (int x = -5; x <= 5; x++)
        for (int y = -2; y <= 2; y++)
        for (int z = -5; z <= 5; z++)
        {
            var pos = plrPos.AddCopy(x, y, z);
            var be = _capi.World.BlockAccessor.GetBlockEntity(pos);
            if (be is not BlockEntityGenericTypedContainer container) continue;

            //var hotbar = _capi.World.Player.InventoryManager.GetOwnInventory("hotbar");
            //_capi.Network.SendPacketClient(hotbar.Close(_capi.World.Player));

            bool opened = container.Inventory.HasOpened(_capi.World.Player);
            if (!opened)
                _capi.Network.SendPacketClient(container.Inventory.Open(_capi.World.Player));
            ////openedChests.Add(container);
            log($"Found chest at {pos}");
            IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory("backpack");

            QuickStackToChest(container.Inventory, inventory);

            if (!opened)
                _capi.Network.SendPacketClient(container.Inventory.Close(_capi.World.Player));
        }

        // do quick stack logic here with openedChests

        // close all
        //foreach (var container in openedChests)
        //    _capi.Network.SendPacketClient(container.Inventory.Close(_capi.World.Player));
    }

    public void openChest() {

            var sel = _capi.World.Player.CurrentBlockSelection;
            if (sel == null)
                return;

            var be = _capi.World.BlockAccessor.GetBlockEntity(sel.Position);
            if (be == null) {
                return ;
            }

            if (be is not BlockEntityGenericTypedContainer container) {
                log($"OMG {be.GetType()}");
                log($"OMG {be.GetType().FullName}");
                return;
            }

            var inventory = container.Inventory; // already public from BlockEntityContainer
            object obj = inventory.Open(_capi.World.Player);
            if (obj != null) {
                _capi.Network.SendPacketClient(obj);
            }
            foreach (var slot in container.Inventory) {
                log($"Slot: {slot.Itemstack?.Item?.Code}");
                        log($"Slot: {slot.Itemstack?.Collectible?.Code}");
            }
            obj = inventory.Close(_capi.World.Player);
            if (obj != null) {
                _capi.Network.SendPacketClient(obj);
            }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        _capi = api;
        Mod.Logger.Notification("Wholtpo starting");
        var harmony = new Harmony("glideview");
        harmony.PatchAll();
        // harmony.PatchAll(Assembly.GetExecutingAssembly());
        // foreach (var method in Harmony.GetAllPatchedMethods())
        //     api.Logger.Debug($"wholtpo Patched: {method.Name}");
        //
        ////////////////////////////
        api.Event.RegisterGameTickListener(dt => {
                omg();
            var sel = api.World.Player.CurrentBlockSelection;
            if (sel == null)
                return;

            var be = api.World.BlockAccessor.GetBlockEntity(sel.Position);
            if (be == null) {
                return ;
            }

            if (be is not BlockEntityGenericTypedContainer container) {
                log($"OMG {be.GetType()}");
                log($"OMG {be.GetType().FullName}");
                return;
            }

            // reflecion check availble method
            //foreach (var method in be.GetType().GetMethods(
            //    System.Reflection.BindingFlags.NonPublic |
            //    System.Reflection.BindingFlags.Public |
            //    System.Reflection.BindingFlags.Instance))
            //{
            //    log($"[Wholtpo] Method: {method.Name}");
            //}

            //var openLid = be.GetType().GetMethod("OpenLid",
            //    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //openLid?.Invoke(be, null);

            //api.Network.SendPacketClient(container.Inventory.Open(api.World.Player));
            //api.Network.SendBlockEntityPacket(be.Pos, 1000, null);

            //// wait for inventory to be populated
            //long listenerId = 0;
            //listenerId = api.Event.RegisterGameTickListener(dt =>
            //{
            //    log("omg 1");
            //    if (container.Inventory.Any(s => s.Itemstack != null))
            //    {
            //    log("omg 2");
            //        var inventory = container.Inventory; // already public from BlockEntityContainer
            //        object obj = inventory.Open(api.World.Player);
            //        if (obj != null) {
            //            api.Network.SendPacketClient(obj);
            //        }
            //        foreach (var slot in container.Inventory) {
            //            log($"Slot: {slot.Itemstack?.Item?.Code}");
            //            log($"Slot: {slot.Itemstack?.Collectible?.Code}");
            //        }
            //        // inventory is now populated, do quick stack
            //        api.Event.UnregisterGameTickListener(listenerId);
            //        //api.Network.SendBlockEntityPacket(be.Pos, 1001, null); // close after
            //    }
            //}, 1000);
            //var toggle = be.GetType().GetMethod("toggleInventoryDialogClient",
            //BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


            //foreach (var p in toggle.GetParameters())
            //    log($"Param: {p.Name} ({p.ParameterType})");

            //toggle?.Invoke(be, new object[] { api.World.Player, null });

            //var method = be.GetType().GetMethod("OnPlayerRightClick",
            //    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //foreach (var p in method.GetParameters())
            //    log($"Param: {p.Name} ({p.ParameterType})");
            // method?.Invoke(be, new object[] { api.World.Player, sel });
            //var method = be.GetType().GetMethod("get_Inventory",
            //    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //foreach (var p in method.GetParameters())
            //    log($"Param: {p.Name} ({p.ParameterType})");

            //var inventory = method?.Invoke(be, new object[] { });


            //toggle?.Invoke(be, null);
        }, 2000);
    }
}
}
