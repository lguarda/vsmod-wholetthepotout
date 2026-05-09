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

namespace Wholtpo {

public class WholtpoModSystem : ModSystem {
    public override void StartClientSide(ICoreClientAPI api) {
        Mod.Logger.Notification("Wholtpo starting");
        var harmony = new Harmony("glideview");
        //harmony.PatchAll();
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        foreach (var method in Harmony.GetAllPatchedMethods())
            api.Logger.Debug($"[wholtpo] Patched: {method.Name}");
        //{
        //  var method =
        //      AccessTools.Method(typeof(BlockEntityFirepit), "OnSlotModified");
        //  if (method == null) {
        //    api.Logger.Error(
        //        $"Wholtpo: fail to found method OnSlotModified  stopping the mod");
        //    return;
        //  }
        //  harmony.Patch(method, postfix: new HarmonyMethod(typeof(FirepitPatch),
        //                                                   "SlotModifiedPostfix"));
        //}

        //{
        //  var method = AccessTools.Method(typeof(BlockEntityOpenableContainer),
        //                                  "toggleInventoryDialogClient");
        //  if (method == null) {
        //    api.Logger.Error(
        //        $"Wholtpo: fail to found method toggleInventoryDialogClient stopping the mod");
        //    return;
        //  }
        //  harmony.Patch(
        //      method, postfix: new HarmonyMethod(typeof(FirepitPatch), "Postfix"));
        //}
    }
}
}
