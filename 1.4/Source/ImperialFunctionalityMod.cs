using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ImperialFunctionality
{
    public class ImperialFunctionalityMod : Mod
    {
        public ImperialFunctionalityMod(ModContentPack pack) : base(pack)
        {
			new Harmony("ImperialFunctionalityMod").PatchAll();
        }
    }

    [DefOf]
    public static class IF_DefOf
    {
        public static ThingDef Plant_Psychoid, Plant_Cotton, Plant_Tinctoria, Plant_Healroot, VFED_WaterMain;
    }

    public class CompImperialSpawner : ThingComp
    {
        private int ticksUntilSpawn;

        private bool CanOperate => WaterMainNearby() && parent.Position.Roofed(parent.Map) is false 
            && parent.GetComp<CompPowerTrader>().PowerOn;

        private bool WaterMainNearby()
        {
            var watermains = parent.Map.listerThings.ThingsOfDef(IF_DefOf.VFED_WaterMain);
            Log.Message("watermains: " + string.Join(", ", watermains));
            foreach (var waterMain in watermains)
            {
                if (waterMain.TryGetComp<CompPowerTrader>().PowerOn)
                {
                    foreach (var cell in GenRadial.RadialCellsAround(waterMain.Position, waterMain.def.specialDisplayRadius,
                        useCenter: true))
                    {
                        if (cell.InBounds(parent.Map))
                        {
                            var thing = cell.GetFirstThing(parent.Map, parent.def);
                            Log.Message(cell + " found thing: " + thing);
                            if (thing == this.parent)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public ThingDef selectedThingDef;

        public IEnumerable<ThingDef> Candidates
        {
            get
            {
                yield return ThingDefOf.Plant_Potato;
                yield return IF_DefOf.Plant_Cotton;
                yield return IF_DefOf.Plant_Tinctoria;
                yield return IF_DefOf.Plant_Psychoid;
                yield return IF_DefOf.Plant_Healroot;
            }
        }


        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                ResetCountdown();
                selectedThingDef = Candidates.First();
            }
        }

        public override void CompTick()
        {
            TickInterval(1);
        }

        public override void CompTickRare()
        {
            TickInterval(250);
        }

        private void TickInterval(int interval)
        {
            if (!parent.Spawned)
            {
                return;
            }
            if (CanOperate)
            {
                ticksUntilSpawn -= interval;
                CheckShouldSpawn();
            }
        }

        private void CheckShouldSpawn()
        {
            if (ticksUntilSpawn <= 0)
            {
                ResetCountdown();
                TryDoSpawn();
            }
        }

        public bool TryDoSpawn()
        {
            if (!parent.Spawned)
            {
                return false;
            }
            var spawnInfo = GetSpawnInfo();
            if (TryFindSpawnCell(parent, spawnInfo.defToSpawn, spawnInfo.spawnCount, out var result))
            {
                Thing thing = ThingMaker.MakeThing(selectedThingDef);
                thing.stackCount = spawnInfo.spawnCount;
                if (thing == null)
                {
                    Log.Error("Could not spawn anything for " + parent);
                }
                GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing);
                return true;
            }
            return false;
        }

        public (ThingDef defToSpawn, int spawnCount) GetSpawnInfo()
        {
            return (selectedThingDef.plant.harvestedThingDef, GetCountToSpawn());
        }

        public int GetCountToSpawn()
        {
            if (selectedThingDef == ThingDefOf.Plant_Potato)
            {
                return 300;
            }
            if (selectedThingDef == IF_DefOf.Plant_Cotton)
            {
                return 250;
            }
            if (selectedThingDef == IF_DefOf.Plant_Psychoid)
            {
                return 150;
            }
            if (selectedThingDef == IF_DefOf.Plant_Tinctoria)
            {
                return 100;
            }
            if (selectedThingDef == IF_DefOf.Plant_Healroot)
            {
                return 75;
            }
            return 0;
        }

        public static bool TryFindSpawnCell(Thing parent, ThingDef thingToSpawn, int spawnCount, out IntVec3 result)
        {
            foreach (IntVec3 item in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
            {
                if (!item.Walkable(parent.Map))
                {
                    continue;
                }
                Building edifice = item.GetEdifice(parent.Map);
                if (edifice != null && thingToSpawn.IsEdifice())
                {
                    continue;
                }
                Building_Door building_Door = edifice as Building_Door;
                if ((building_Door != null && !building_Door.FreePassage) || (parent.def.passability != Traversability.Impassable && !GenSight.LineOfSight(parent.Position, item, parent.Map)))
                {
                    continue;
                }
                bool flag = false;
                List<Thing> thingList = item.GetThingList(parent.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing.def.category == ThingCategory.Item && (thing.def != thingToSpawn || thing.stackCount > thingToSpawn.stackLimit - spawnCount))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    result = item;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }

        private void ResetCountdown()
        {
            ticksUntilSpawn = GenDate.TicksPerDay * 7;
        }

        public override void PostExposeData()
        {
            Scribe_Defs.Look(ref selectedThingDef, "selectedThingDef");
            Scribe_Values.Look(ref ticksUntilSpawn, "ImperialFunctionality_ticksUntilSpawn", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "IF.SelectSpawnThing".Translate(selectedThingDef.label),
                action = delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var thingDef in Candidates)
                    {
                        floatList.Add(new FloatMenuOption(thingDef.LabelCap, delegate
                        {
                            selectedThingDef = thingDef;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                },
                icon = selectedThingDef.uiIcon
            };
        }

        public override string CompInspectStringExtra()
        {
            if (CanOperate)
            {
                var spawnInfo = GetSpawnInfo();
                return "NextSpawnedItemIn".Translate(GenLabel.ThingLabel(spawnInfo.defToSpawn, null, spawnInfo.spawnCount)).Resolve() + ": " + ticksUntilSpawn.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor);
            }
            else
            {
                return "IF.UnderfarmInoperable".Translate();
            }
        }
    }
}
