using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace DropPodsInProgress
{
    [StaticConstructorOnStartup]
    public class Command_ReloadTransporters : Command
    {
        public CompTransporter transComp;
        public List<CompTransporter> transporters = new List<CompTransporter>();
        private static HashSet<Building> tmpFuelingPortGivers = new HashSet<Building>();

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (transporters == null)
            {
                transporters = new List<CompTransporter>();
            }
            if (!transporters.Contains(transComp))
            {
                transporters.Add(transComp);
            }
            CompLaunchable launchable = transComp.Launchable;
            if (launchable != null)
            {
                Building fuelingPortSource = launchable.FuelingPortSource;
                if (fuelingPortSource != null)
                {
                    Map map = transComp.Map;
                    tmpFuelingPortGivers.Clear();
                    map.floodFiller.FloodFill(fuelingPortSource.Position, (IntVec3 x) => FuelingPortUtility.AnyFuelingPortGiverAt(x, map), delegate (IntVec3 x)
                    {
                        tmpFuelingPortGivers.Add(FuelingPortUtility.FuelingPortGiverAt(x, map));
                    }, int.MaxValue, false, null);
                    for (int i = 0; i < transporters.Count; i++)
                    {
                        Building fuelingPortSource2 = transporters[i].Launchable.FuelingPortSource;
                        if (fuelingPortSource2 != null && !tmpFuelingPortGivers.Contains(fuelingPortSource2))
                        {
                            Messages.Message("MessageTransportersNotAdjacent".Translate(), fuelingPortSource2, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                    }
                }
            }
            for (int j = 0; j < transporters.Count; j++)
            {
                if (transporters[j] != transComp)
                {
                    if (!transComp.Map.reachability.CanReach(transComp.parent.Position, transporters[j].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                    {
                        Messages.Message("MessageTransporterUnreachable".Translate(), transporters[j].parent, MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                }
            }
            Find.WindowStack.Add(new Dialog_ReloadTransporters(transComp.Map, transporters));
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            Command_ReloadTransporters command_LoadToTransporter = (Command_ReloadTransporters)other;
            if (command_LoadToTransporter.transComp.parent.def != transComp.parent.def)
            {
                return false;
            }
            if (transporters == null)
            {
                transporters = new List<CompTransporter>();
            }
            if (!transporters.Contains(transComp))
            {
                transporters.Add(command_LoadToTransporter.transComp);
            }
            return false;
        }
    }
}
