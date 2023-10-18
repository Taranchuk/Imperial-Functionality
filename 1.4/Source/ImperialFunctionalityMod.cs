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

        private bool CanOperate => parent.Spawned && WaterMainNearby() && parent.Position.Roofed(parent.Map) is false 
            && parent.GetComp<CompPowerTrader>().PowerOn;

        private bool WaterMainNearby()
        {
            var watermains = parent.Map.listerThings.ThingsOfDef(IF_DefOf.VFED_WaterMain);
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

        public void TryDoSpawn()
        {
            var spawnInfo = GetSpawnInfo();
            var spawnCount = spawnInfo.spawnCount;
            while (spawnCount > 0)
            {
                Thing thing = ThingMaker.MakeThing(spawnInfo.defToSpawn);
                thing.stackCount = Mathf.Min(thing.def.stackLimit, spawnCount);
                spawnCount -= thing.stackCount;
                if (TryFindRandomCellNear(parent.Position, parent.Map, 9, out var result))
                {
                    if (result.IsValid)
                    {
                        GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing);
                    }
                }
            }
        }

        public bool TryFindRandomCellNear(IntVec3 root, Map map, float radius, out IntVec3 result)
        {
            foreach (var cell in GenRadial.RadialCellsAround(root, radius, true))
            {
                if (cell.GetFirstItem(map) is null)
                {
                    result = cell;
                    return true;
                }
            }
            result = IntVec3.Invalid;
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

        private void ResetCountdown()
        {
            ticksUntilSpawn = GenDate.TicksPerDay * 7;
        }

        public override void PostExposeData()
        {
            Scribe_Defs.Look(ref selectedThingDef, "ImperialFunctionality_selectedThingDef");
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
