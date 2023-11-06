using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace ImperialFunctionality
{
    [HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
    public static class FactionDialogMaker_FactionDialogFor_Patch
    {
        public static void Postfix(Pawn negotiator, Faction faction, ref DiaNode __result)
        {
            if (faction == Faction.OfEmpire)
            {
                var node = new DiaOption("IF.GetHonorQuestForSilver".Translate());
                if (!TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, 5000))
                {
                    node.Disable("IF.GetHonorQuestForSilverNotEnoughMoney".Translate());
                };
                node.action = delegate
                {
                    TradeUtility.LaunchSilver(Find.CurrentMap, 5000);
                    var slate = new Slate();
                    slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.World));
                    var questDef = DefDatabase<QuestScriptDef>.AllDefs.Where(x => x.canGiveRoyalFavor && x.CanRun(slate)).RandomElement();
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                    if (!quest.hidden && quest.root.sendAvailableLetter)
                    {
                        QuestUtility.SendLetterQuestAvailable(quest);
                    }
                };
                node.resolveTree = true;
                var disconnectOption = __result.options.FirstIndexOf(x => x.text == "(" + "Disconnect".Translate() + ")");
                if (disconnectOption >= 0)
                {
                    __result.options.Insert(disconnectOption, node);
                }
                else
                {
                    __result.options.Add(node);
                }
            }
        }
    }
}
