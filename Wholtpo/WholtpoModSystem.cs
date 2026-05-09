using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using System.Reflection;
using System.Linq;

namespace Wholtpo {

public class WholtpoModSystem : ModSystem {

    ICoreClientAPI _capi;

    public void log(string message) {
        _capi.ShowChatMessage(message);
        _capi.Logger.Debug(message);
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
            //
            var inventory = container.Inventory; // already public from BlockEntityContainer
            object obj = inventory.Open(api.World.Player);
            if (obj != null) {
                _capi.Network.SendPacketClient(obj);
            }
            foreach (var slot in container.Inventory) {
                log($"Slot: {slot.Itemstack?.Item?.Code}");
                        log($"Slot: {slot.Itemstack?.Collectible?.Code}");
            }
            obj = inventory.Close(api.World.Player);
            if (obj != null) {
                _capi.Network.SendPacketClient(obj);
            }

            //toggle?.Invoke(be, null);
        }, 2000);
    }
}
}
