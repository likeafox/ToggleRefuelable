using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Harmony;
using CompRefuelable = RimWorld.CompRefuelable;

namespace ToggleRefuelable
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            var harmony = HarmonyInstance.Create("likeafox.rimworld.togglerefuelable");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        }
    }

    public class ToggleRefuelable : GameComponent
    {
        public HashSet<ThingWithComps> nonrefuelingThings = new HashSet<ThingWithComps>();

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
                nonrefuelingThings.RemoveWhere(t => t.Destroyed);
            Scribe_Collections.Look(ref nonrefuelingThings, "NonrefuelingThings", LookMode.Reference);
        }

        public static ToggleRefuelable instance { get; private set; }
        public ToggleRefuelable(Game game) : this() { }
        public ToggleRefuelable() { instance = this; }
    }

    public static class Extensions
    {
        public static void SetRefueling(this CompRefuelable comp, bool enable)
        {
            if (enable)
                ToggleRefuelable.instance.nonrefuelingThings.Remove(comp.parent);
            else
                ToggleRefuelable.instance.nonrefuelingThings.Add(comp.parent);
        }

        public static bool IsSetForRefueling(this CompRefuelable comp)
        {
            return !ToggleRefuelable.instance.nonrefuelingThings.Contains(comp.parent);
        }
    }

    [HarmonyPatch(typeof(CompRefuelable), "CompGetGizmosExtra")]
    class CompRefuelable_CompGetGizmosExtra_Patch
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, CompRefuelable __instance)
        {
            foreach (var g in gizmos)
                yield return g;
            var refuel = new Command_Toggle
            {
                icon = ContentFinder<UnityEngine.Texture2D>.Get("Buttons/Refuel", true),
                defaultLabel = "Refuel",
                //defaultDesc = "",
                isActive = (() => __instance.IsSetForRefueling()),
                toggleAction = delegate
                {
                    __instance.SetRefueling(!__instance.IsSetForRefueling());
                },
                hotKey = null
            };
            yield return refuel;
        }
    }

    [HarmonyPatch(typeof(CompRefuelable), "ShouldAutoRefuelNowIgnoringFuelPct", MethodType.Getter)]
    class CompRefuelable_ShouldAutoRefuelNowIgnoringFuelPct_Patch
    {
        static void Postfix(ref bool __result, CompRefuelable __instance)
        {
            if (!__instance.IsSetForRefueling())
                __result = false;
        }
    }
}
