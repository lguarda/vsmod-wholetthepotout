using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;

namespace Wholtpo {

public class WholtpoModSystem : ModSystem {
    public override void StartClientSide(ICoreClientAPI api) {
        Mod.Logger.Notification("Wholtpo starting");
        var harmony = new Harmony("glideview");
        harmony.PatchAll();
        // harmony.PatchAll(Assembly.GetExecutingAssembly());
        // foreach (var method in Harmony.GetAllPatchedMethods())
        //     api.Logger.Debug($"wholtpo Patched: {method.Name}");
    }
}
}
