using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Harmony;
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
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("rimworld.droppodsinprogress.smashphil");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            harmony.Patch(original: AccessTools.Method(type: typeof(CompTransporter), name: nameof(CompTransporter.CompGetGizmosExtra)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(HarmonyPatches),
                name: nameof(BoardTransporterInProgress)));
        }

        public static IEnumerable<Gizmo> BoardTransporterInProgress(IEnumerable<Gizmo> __result, CompTransporter __instance)
        {
            IEnumerator<Gizmo> enumerator = __result.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                if(__instance.LoadingInProgressOrReadyToLaunch && (element as Command_Action)?.icon == CancelLoadCommandTex)
                {
                    yield return element;
                    List<CompTransporter> transporterGroup = __instance.TransportersInGroup(__instance.parent.Map);
                    yield return new Command_ReloadTransporters
                    {
                        defaultLabel = transporterGroup.Count > 1 ? "CommandReloadTransporter".Translate(transporterGroup.Count) : "CommandReloadTransporterSingle".Translate(),
                        defaultDesc = "CommandReloadTransporterDesc".Translate(),
                        icon = LoadCommandTex,
                        transComp = __instance,
                        transporters = transporterGroup
                    };
                    continue;
                }
                yield return element;
            }
        }

        private static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", true);
        private static readonly Texture2D CancelLoadCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
    }
}
