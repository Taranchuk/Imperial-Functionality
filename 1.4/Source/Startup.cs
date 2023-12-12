using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ImperialFunctionality
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var royalTitle in FactionDefOf.Empire.RoyalTitlesAllInSeniorityOrderForReading)
            {
                if (royalTitle.seniority >= IF_DefOf.VFEE_Despot.seniority)
                {
                    royalTitle.grantedAbilities ??= new List<AbilityDef>();
                    royalTitle.grantedAbilities.Add(IF_DefOf.IF_FormEmpireAlliance);
                }
                if (royalTitle.seniority >= IF_DefOf.VFEE_Archduke.seniority)
                {
                    royalTitle.grantedAbilities ??= new List<AbilityDef>();
                    royalTitle.grantedAbilities.Add(IF_DefOf.IF_RoyalInvitation);
                }
            }
        }
    }
}
