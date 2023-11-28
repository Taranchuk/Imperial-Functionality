﻿using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using VFED;

namespace ImperialFunctionality
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo")]
    public static class IncidentWorker_Raid_TryGenerateRaidInfo_Patch
    {
        public static void Postfix(IncidentParms parms, ref bool __result)
        {
            if (__result && parms.target is Map map)
            {
                var stations = map.listerBuildings.AllBuildingsColonistOfDef(IF_DefOf.VFED_SurveillanceStation).ToList();
                Log.Message("stations: " + string.Join(", ", stations));
                foreach (var station in stations)
                {
                    var comp = station.GetComp<CompSurveillanceScanner>();
                    var pointsToReduce = Mathf.Min(comp.raidPointsFound, parms.points);
                    parms.points -= pointsToReduce;
                    Log.Message("reducing points by " + pointsToReduce);
                    comp.raidPointsFound -= pointsToReduce;
                    if (parms.points <= 0)
                    {
                        __result = false;
                        Log.Message("cancelling raid ");
                    }
                }
            }
        }
    }
}