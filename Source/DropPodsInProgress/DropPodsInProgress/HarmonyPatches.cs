using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;
using UnityEngine;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace DropPodsInProgress
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        private static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", true);
        private static readonly Texture2D CancelLoadCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.droppodsinprogress.smashphil");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            harmony.Patch(original: AccessTools.Method(typeof(CompTransporter), nameof(CompTransporter.CompGetGizmosExtra)), prefix: null,
                postfix: new HarmonyMethod(typeof(HarmonyPatches),
                nameof(BoardTransporterInProgress)));
        }

        public static IEnumerable<Gizmo> BoardTransporterInProgress(IEnumerable<Gizmo> __result, CompTransporter __instance)
        {
            List<CompTransporter> transporterGroup = __instance.TransportersInGroup(__instance.parent.Map);
            if (__instance.LoadingInProgressOrReadyToLaunch && !transporterGroup.NullOrEmpty())
            {
                yield return new Command_ReloadTransporters
                {
                    defaultLabel = transporterGroup.Count > 1 ? "CommandReloadTransporter".Translate(transporterGroup.Count) : "CommandReloadTransporterSingle".Translate(),
                    defaultDesc = "CommandReloadTransporterDesc".Translate(),
                    icon = LoadCommandTex,
                    transComp = __instance,
                    transporters = transporterGroup
                };
            }
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
        }
    }
}
