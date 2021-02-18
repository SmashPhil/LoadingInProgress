using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace DropPodsInProgress
{
    public class Dialog_ReloadTransporters : Window
    {
        private const float TitleRectHeight = 35f;
        private const float BottomAreaHeight = 55f;

        private readonly Map map;
        private readonly List<CompTransporter> transporters;

        private List<TransferableOneWay> transferables;
        private TransferableOneWayWidget pawnsTransfer;
        private TransferableOneWayWidget itemsTransfer;

        private Tab tab;
        private float lastMassFlashTime = -9999f;
        private bool massUsageDirty = true;
        private float cachedMassUsage;
        private bool caravanMassUsageDirty = true;
        private float cachedCaravanMassUsage;
        private bool caravanMassCapacityDirty = true;
        private float cachedCaravanMassCapacity;
        private string cachedCaravanMassCapacityExplanation;
        private bool tilesPerDayDirty = true;
        private float cachedTilesPerDay;
        private string cachedTilesPerDayExplanation;
        private bool daysWorthOfFoodDirty = true;
        private Pair<float, float> cachedDaysWorthOfFood;
        private bool foragedFoodPerDayDirty = true;
        private Pair<ThingDef, float> cachedForagedFoodPerDay;
        private string cachedForagedFoodPerDayExplanation;
        private bool visibilityDirty = true;
        private float cachedVisibility;
        private string cachedVisibilityExplanation;

        private static readonly List<TabRecord> tabsList = new List<TabRecord>();

        public Dialog_ReloadTransporters(Map map, List<CompTransporter> transporters)
        {
            this.map = map;
            this.transporters = new List<CompTransporter>();
            this.transporters.AddRange(transporters);
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        private Vector2 BottomButtonSize => new Vector2(160f, 40f);

        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

        protected override float Margin => 0;

        private float MassCapacity
        {
            get
            {
                float num = 0f;
                for (int i = 0; i < transporters.Count; i++)
                {
                    num += transporters[i].Props.massCapacity;
                }
                return num;
            }
        }

        private float CaravanMassCapacity
        {
            get
            {
                if (caravanMassCapacityDirty)
                {
                    caravanMassCapacityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    cachedCaravanMassCapacity = CollectionsMassCalculator.CapacityTransferables(transferables, stringBuilder);
                    cachedCaravanMassCapacityExplanation = stringBuilder.ToString();
                }
                return cachedCaravanMassCapacity;
            }
        }

        private string TransportersLabel
        {
            get
            {
                return Find.ActiveLanguageWorker.Pluralize(transporters[0].parent.Label, -1);
            }
        }

        private BiomeDef Biome
        {
            get
            {
                return map.Biome;
            }
        }

        private float MassUsage
        {
            get
            {
                if (massUsageDirty)
                {
                    massUsageDirty = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, false);
                    cachedMassUsage += InventoryMassUsage();
                }
                return cachedMassUsage;
            }
        }

        public float CaravanMassUsage
        {
            get
            {
                if (caravanMassUsageDirty)
                {
                    caravanMassUsageDirty = false;
                    cachedCaravanMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
                    cachedCaravanMassUsage += InventoryMassUsage();
                }
                return cachedCaravanMassUsage;
            }
        }

        private float TilesPerDay
        {
            get
            {
                if (tilesPerDayDirty)
                {
                    tilesPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    cachedTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(transferables, MassUsage, MassCapacity, map.Tile, -1, stringBuilder);
                    cachedTilesPerDayExplanation = stringBuilder.ToString();
                }
                return cachedTilesPerDay;
            }
        }

        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (daysWorthOfFoodDirty)
                {
                    daysWorthOfFoodDirty = false;
                    float first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer, null, 0f, 3300);
                    cachedDaysWorthOfFood = new Pair<float, float>(first, DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, null, 0f, 3300));
                }
                return cachedDaysWorthOfFood;
            }
        }

        private Pair<ThingDef, float> ForagedFoodPerDay
        {
            get
            {
                if (foragedFoodPerDayDirty)
                {
                    foragedFoodPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(transferables, Biome, Faction.OfPlayer, stringBuilder);
                    cachedForagedFoodPerDayExplanation = stringBuilder.ToString();
                }
                return cachedForagedFoodPerDay;
            }
        }

        private float Visibility
        {
            get
            {
                if (visibilityDirty)
                {
                    visibilityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    cachedVisibility = CaravanVisibilityCalculator.Visibility(transferables, stringBuilder);
                    cachedVisibilityExplanation = stringBuilder.ToString();
                }
                return cachedVisibility;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, TitleRectHeight);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "LoadTransporters".Translate(TransportersLabel));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, string.Empty, TilesPerDay, cachedTilesPerDayExplanation, DaysWorthOfFood, ForagedFoodPerDay, 
                cachedForagedFoodPerDayExplanation, Visibility, cachedVisibilityExplanation, CaravanMassUsage, CaravanMassCapacity, cachedCaravanMassCapacityExplanation), null, map.Tile, null, lastMassFlashTime, 
                new Rect(12f, 35f, inRect.width - 24f, 40f), false, null, false);
            tabsList.Clear();
            tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate ()
            {
                tab = Tab.Pawns;
            }, tab == Tab.Pawns));
            tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate ()
            {
                tab = Tab.Items;
            }, tab == Tab.Items));
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, tabsList, 200f);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            bool flag = false;
            Tab curTab = tab;
            if (curTab != Tab.Pawns)
            {
                if (curTab == Tab.Items)
                {
                    itemsTransfer.OnGUI(inRect2, out flag);
                }
            }
            else
            {
                pawnsTransfer.OnGUI(inRect2, out flag);
            }
            if (flag)
            {
                CountToTransferChanged();
            }
            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        private void AddToTransferables(Thing t)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
        }

        private void AddToTransferablesSelected(Thing t)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if(transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
            transferableOneWay.AdjustTo(t.stackCount);
        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - BottomAreaHeight, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true))
            {
                if (CaravanMassUsage > CaravanMassCapacity && CaravanMassCapacity != 0f)
                {
                    if (CheckForErrors(TransferableUtility.GetPawnsFromTransferables(transferables)))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TransportersCaravanWillBeImmobile".Translate(), delegate
                        {
                            if (TryAccept())
                            {
                                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                                Close(false);
                            }
                        }, false, null));
                    }
                }
                else if (TryAccept())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    Close(false);
                }
            }
            Rect rect3 = new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                Close(true);
            }
            if (Prefs.DevMode)
            {
                float width = 200f;
                float num = BottomButtonSize.y / 2f;
                Rect rect5 = new Rect(0f, rect.height - BottomAreaHeight, width, num);
                if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false, true) && DebugTryLoadInstantly())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    Close(false);
                }
                Rect rect6 = new Rect(0f, rect.height - BottomAreaHeight + num, width, num);
                if (Widgets.ButtonText(rect6, "Dev: Select everything", true, false, true))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    SetToLoadEverything();
                }
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            AddPawnsToTransferables();
            AddItemsToTransferables();
            AddContentsToTransferables();
            IEnumerable<TransferableOneWay> enumerable = null;
            string text = null;
            string destinationLabel = null;
            string text2 = "FormCaravanColonyThingCountTip".Translate();
            bool flag = true;
            IgnorePawnsInventoryMode ignorePawnInventoryMass = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            bool flag2 = true;
            Func<float> availableMassGetter = () => MassCapacity - MassUsage;
            int tile = map.Tile;
            pawnsTransfer = new TransferableOneWayWidget(enumerable, text, destinationLabel, text2, flag, ignorePawnInventoryMass, flag2, availableMassGetter, 0f, false, tile, true, true, true, false, true, false, false);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
            enumerable = from x in transferables
                            where x.ThingDef.category != ThingCategory.Pawn
                            select x;
            text2 = null;
            destinationLabel = null;
            text = "FormCaravanColonyThingCountTip".Translate();
            flag2 = true;
            ignorePawnInventoryMass = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            flag = true;
            availableMassGetter = (() => MassCapacity - MassUsage);
            tile = map.Tile;
            itemsTransfer = new TransferableOneWayWidget(enumerable, text2, destinationLabel, text, flag2, ignorePawnInventoryMass, flag, availableMassGetter, 0f, false, tile, true, false, false, true, false, true, false);
            CountToTransferChanged();
        }

        private bool DebugTryLoadInstantly()
        {
            CreateAndAssignNewTransportersGroup();
            int i;
            for (i = 0; i < transferables.Count; i++)
            {
                TransferableUtility.Transfer(transferables[i].things, transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    transporters[i % transporters.Count].GetDirectlyHeldThings().TryAdd(splitPiece, true);
                });
            }
            return true;
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
            if (!CheckForErrors(pawnsFromTransferables))
            {
                return false;
            }
            int transportersGroup = CreateAndAssignNewTransportersGroup();
            KickOutFreeloadingPawns();
            RemoveUnwantedItems();
            ClearContentsNotLoaded();
            AssignTransferablesToRandomTransporters();
            IEnumerable<Pawn> enumerable = from x in pawnsFromTransferables
                                            where x.IsColonist && !x.Downed
                                            select x;
            if (enumerable.Any<Pawn>())
            {
                foreach (Pawn pawn in enumerable)
                {
                    Lord lord = pawn.GetLord();
                    if (lord != null)
                    {
                        lord.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord, null);
                    }
                }
                LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadAndEnterTransporters(transportersGroup), map, enumerable);
                foreach (Pawn pawn2 in enumerable)
                {
                    if (pawn2.Spawned)
                    {
                        pawn2.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }
            Messages.Message("MessageTransportersLoadingProcessStarted".Translate(), transporters[0].parent, MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        public void KickOutFreeloadingPawns()
        {
            int num = 0;
            foreach(CompTransporter transporter in transporters)
            {
                for(int i = transporter.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing t = transporter.innerContainer[i];
                    if(t is Pawn)
                    {
                        bool flag = transporter.innerContainer.TryDrop(t, ThingPlaceMode.Near, out Thing thing);

                        if ((t as Pawn).GetLord() != null)
                        {
                            (t as Pawn).GetLord().lordManager.RemoveLord((t as Pawn).GetLord());
                        }
                        /*For Debugging*/ 
                        //Log.Message("Dropping " + t.LabelShort + " : " + flag);
                    }
                    num++;
                }
            }
        }

        public void RemoveUnwantedItems()
        {
            foreach(CompTransporter transporter in transporters)
            {
                foreach(Thing t in transporter.innerContainer)
                {
                    if(t is Pawn && TransferableUtility.GetPawnsFromTransferables(transferables).Contains(t as Pawn))
                    {
                        Log.Warning(string.Concat(new object[]{
                            "Pawn ",
                            t.LabelShort,
                            " was still inside transporter ",
                            transporter.parent.ThingID,
                            " after pawns were dumped.",
                            "Removing ", t.LabelShort,
                            " from transporter and spawning manually. - Smash Phil"
                        }));
                        if (!t.Spawned)
                        {
                            GenSpawn.Spawn(t, transporter.parent.Position, transporter.Map, WipeMode.Vanish);
                        }
                        transporter.innerContainer.Remove(t);
                    }
                }
            }
        }

        public void ClearContentsNotLoaded()
        {
            foreach(CompTransporter transporter in transporters)
            {
                transporter.leftToLoad?.Clear();
            }

            List<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
            for(int i = 0; i < allPawnsSpawned.Count; i++)
            {
                foreach(CompTransporter transporter in transporters)
                {
                    if(allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToTransporter)
                    {
                        JobDriver_HaulToTransporter jobDriver_HaulToTransporter = (JobDriver_HaulToTransporter)allPawnsSpawned[i].jobs.curDriver;
                        if(jobDriver_HaulToTransporter.Transporter == transporter)
                        {
                            if(jobDriver_HaulToTransporter.ThingToCarry != null)
                            {
                                allPawnsSpawned[i].jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                            }
                        }
                    }
                }
            }
        }

        private void AssignTransferablesToRandomTransporters()
        {
            TransferableOneWay transferableOneWay = transferables.MaxBy((TransferableOneWay x) => x.CountToTransfer);
            int num = 0;
            for (int i = 0; i < transferables.Count; i++)
            {
                if (transferables[i] != transferableOneWay)
                {
                    if (transferables[i].CountToTransfer > 0)
                    {
                        transporters[num % transporters.Count].AddToTheToLoadList(transferables[i], transferables[i].CountToTransfer);
                        num++;
                    }
                }
            }

            if (num < transporters.Count)
            {
                int num2 = transferableOneWay.CountToTransfer;
                int num3 = num2 / (transporters.Count - num);
                for (int j = num; j < transporters.Count; j++)
                {
                    int num4 = (j != transporters.Count - 1) ? num3 : num2;
                    if (num4 > 0)
                    {
                        transporters[j].AddToTheToLoadList(transferableOneWay, num4);
                    }
                    num2 -= num4;
                }
            }
            else
            {
                transporters[num % transporters.Count].AddToTheToLoadList(transferableOneWay, transferableOneWay.CountToTransfer);
            }
        }

        private int CreateAndAssignNewTransportersGroup()
        {
            int nextTransporterGroupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
            for (int i = 0; i < transporters.Count; i++)
            {
                transporters[i].groupID = nextTransporterGroupID;
            }
            return nextTransporterGroupID;
        }

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (!transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
            {
                Messages.Message("CantSendEmptyTransportPods".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (MassUsage > MassCapacity)
            {
                FlashMass();
                Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            Pawn pawn = pawns.Find((Pawn x) => !x.MapHeld.reachability.CanReach(x.PositionHeld, transporters[0].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)));
            if (pawn != null)
            {
                Messages.Message("PawnCantReachTransporters".Translate(pawn.LabelShort, pawn).CapitalizeFirst(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            Map map = transporters[0].parent.Map;
            for (int i = 0; i < transferables.Count; i++)
            {
                if (transferables[i].ThingDef.category == ThingCategory.Item)
                {
                    int countToTransfer = transferables[i].CountToTransfer;
                    int num = 0;
                    if (countToTransfer > 0)
                    {
                        for (int j = 0; j < transferables[i].things.Count; j++)
                        {
                            Thing thing = transferables[i].things[j];
                            if (map.reachability.CanReach(thing.Position, transporters[0].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                            {
                                num += thing.stackCount;
                                if (num >= countToTransfer)
                                {
                                    break;
                                }
                            }
                        }
                        if (num < countToTransfer)
                        {
                            if (countToTransfer == 1)
                            {
                                Messages.Message("TransporterItemIsUnreachableSingle".Translate(transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, false);
                            }
                            else
                            {
                                Messages.Message("TransporterItemIsUnreachableMulti".Translate(countToTransfer, transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, false);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void AddPawnsToTransferables()
        {
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(map, false, false, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                AddToTransferables(list[i]);
            }
            /* Debugging */
            /*foreach (Pawn p in map.mapPawns.AllPawnsSpawned.Where(x => x.Faction == Faction.OfPlayer))
            {
                Log.Message("Testing: " + p.LabelShort);
                Log.Message("DownCheck: " + !p.Downed + " | MentalCheck: " + !p.InMentalState + " | PrisonerCheck: " + (p.IsPrisonerOfColony || p.Faction == Faction.OfPlayer) + " | LordCheck " + (p.GetLord() is null || p.GetLord().LordJob is LordJob_VoluntarilyJoinable));
                Log.Message("-------------");
            }
            Log.Message("=================");*/
        }

        private void AddItemsToTransferables()
        {
            List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(map, false, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                AddToTransferables(list[i]);
            }
        }

        private void AddContentsToTransferables()
        {
            foreach(CompTransporter transporter in transporters)
            {
                foreach(Thing t in transporter.innerContainer)
                {
                    if (t is Pawn) //Remove to add items inside transporter
                    {
                        AddToTransferablesSelected(t);
                    }
                }
                if(transporter.leftToLoad != null)
                {
                    foreach (TransferableOneWay t in transporter.leftToLoad)
                    {
                        if (t.AnyThing is Pawn)
                        {
                            AddToTransferablesSelected(t.AnyThing);
                        }
                    }
                }
            }
        }

        private void FlashMass()
        {
            lastMassFlashTime = Time.time;
        }

        private float InventoryMassUsage()
        {
            float num = 0f;
            foreach(CompTransporter transporter in transporters)
            {
                foreach(Thing t in transporter.innerContainer)
                {
                    if (!(t is Pawn))
                    {
                        num += t.GetStatValue(StatDefOf.Mass, true) * t.stackCount;
                    }
                }
            }
            return num;
        }

        private void SetToLoadEverything()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                transferables[i].AdjustTo(transferables[i].GetMaximumToTransfer());
            }
            CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            massUsageDirty = true;
            caravanMassUsageDirty = true;
            caravanMassCapacityDirty = true;
            tilesPerDayDirty = true;
            daysWorthOfFoodDirty = true;
            foragedFoodPerDayDirty = true;
            visibilityDirty = true;
        }

        private enum Tab
        {
            Pawns,
            Items
        }
    }
}
