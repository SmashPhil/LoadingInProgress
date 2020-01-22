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
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (this.transporters == null)
            {
                this.transporters = new List<CompTransporter>();
            }
            if (!this.transporters.Contains(this.transComp))
            {
                this.transporters.Add(this.transComp);
            }
            CompLaunchable launchable = this.transComp.Launchable;
            if (launchable != null)
            {
                Building fuelingPortSource = launchable.FuelingPortSource;
                if (fuelingPortSource != null)
                {
                    Map map = this.transComp.Map;
                    tmpFuelingPortGivers.Clear();
                    map.floodFiller.FloodFill(fuelingPortSource.Position, (IntVec3 x) => FuelingPortUtility.AnyFuelingPortGiverAt(x, map), delegate (IntVec3 x)
                    {
                        tmpFuelingPortGivers.Add(FuelingPortUtility.FuelingPortGiverAt(x, map));
                    }, int.MaxValue, false, null);
                    for (int i = 0; i < this.transporters.Count; i++)
                    {
                        Building fuelingPortSource2 = this.transporters[i].Launchable.FuelingPortSource;
                        if (fuelingPortSource2 != null && !tmpFuelingPortGivers.Contains(fuelingPortSource2))
                        {
                            Messages.Message("MessageTransportersNotAdjacent".Translate(), fuelingPortSource2, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                    }
                }
            }
            for (int j = 0; j < this.transporters.Count; j++)
            {
                if (this.transporters[j] != this.transComp)
                {
                    if (!this.transComp.Map.reachability.CanReach(this.transComp.parent.Position, this.transporters[j].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                    {
                        Messages.Message("MessageTransporterUnreachable".Translate(), this.transporters[j].parent, MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                }
            }
            Find.WindowStack.Add(new Dialog_ReloadTransporters(this.transComp.Map, this.transporters));
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            Command_ReloadTransporters command_LoadToTransporter = (Command_ReloadTransporters)other;
            if (command_LoadToTransporter.transComp.parent.def != this.transComp.parent.def)
            {
                return false;
            }
            if (this.transporters == null)
            {
                this.transporters = new List<CompTransporter>();
            }
            if(!this.transporters.Contains(transComp))
                this.transporters.Add(command_LoadToTransporter.transComp);
            return false;
        }

        public CompTransporter transComp;
        public List<CompTransporter> transporters = new List<CompTransporter>();
        private static HashSet<Building> tmpFuelingPortGivers = new HashSet<Building>();
    }
}
