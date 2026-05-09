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

    //ICoreClientAPI _capi;

    //public void log(string message) {
    //    _capi.ShowChatMessage(message);
    //    _capi.Logger.Debug(message);
    //}

    public override void StartClientSide(ICoreClientAPI api) {
        Mod.Logger.Notification("Wholtpo starting");
        var harmony = new Harmony("glideview");
        var method = AccessTools.Method(typeof(BlockEntityFirepit), "OnSlotModified");
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(FirepitPatch), "SlotModifiedPostfix"));
    }
}
}
