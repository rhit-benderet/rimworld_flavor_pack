#region Assembly Assembly-CSharp, Version=1.6.9438.37837, Culture=neutral, PublicKeyToken=null
// C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
// Decompiled with ICSharpCode.Decompiler 9.1.0.7988
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace OOFlavorPack
{

    public class PsychicRitualToil_PsylinkSkipAbductionPlayer : PsychicRitualToil
    {

        public PsychicRitualRoleDef invokerRole;

        protected PsychicRitualToil_PsylinkSkipAbductionPlayer()
        {
        }

        public PsychicRitualToil_PsylinkSkipAbductionPlayer(PsychicRitualRoleDef invokerRole)
        {
            this.invokerRole = invokerRole;
        }

        public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
            if (pawn != null)
            {
                ApplyOutcome(psychicRitual, pawn);
            }
        }
        private float ChangeWeight(PsychicRitual psychicRitual, float weight)
        {
            PsychicRitualDef_PsylinkSkipAbductionPlayer mydef = (PsychicRitualDef_PsylinkSkipAbductionPlayer)psychicRitual.def;
            return ((weight - 1.0f) * mydef.psylinkFavorabilityFromQualityCurve.Evaluate(psychicRitual.PowerPercent)) + 1.0f;
        }
        private float CalculateWeight(PsychicRitual psychicRitual, Pawn pawn, int valid_pawn_count)
        {
            SimpleCurve scaling_curve = new SimpleCurve
            {
                { 0.0f, 0.1f },
                { 4.0f, 0.4f },
                { 25.0f, 1.0f }
            };
            float level = pawn.GetPsylinkLevel();
            float sensitivity = pawn.psychicEntropy.PsychicSensitivity;
            float normalweight = level * level * sensitivity;
            if (normalweight > 1.0f)
            {
                normalweight -= 1.0f;
                normalweight *= scaling_curve.Evaluate(valid_pawn_count);
                normalweight += 1.0f;
            }
            return ChangeWeight(psychicRitual, normalweight);
        }
        private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker)
        {
            IntVec3 cell = psychicRitual.assignments.Target.Cell;
            bool flag = false;
            List<Pawn> source =Find.WorldPawns.AllPawnsAlive.Where(delegate(Pawn p)
				{
					if (p.RaceProps.Humanlike && p.HostileTo(invoker) && !p.IsSubhuman)
					{
						return true;
					}
					return false;
				}).ToList<Pawn>();
            List<Pawn> source_filter = source.Where((Pawn p) => CalculateWeight(psychicRitual, p, 1) > 0.0001f).ToList<Pawn>();
            Pawn pawn = null;
            if (source_filter.Count > 0) {
                Pawn pawn3 = source_filter.RandomElementByWeight((Pawn p) => CalculateWeight(psychicRitual, p, source_filter.Count));
                if (pawn3 != null)
                {
                    pawn = (Pawn)GenSpawn.Spawn(pawn3, cell, psychicRitual.Map, WipeMode.Vanish);
                    flag = true;
                }
            } else
            {
                Pawn pawn3 = source.RandomElementByWeight((Pawn p) => 1.0f);
                if (pawn3 != null)
                {
                    pawn = (Pawn)GenSpawn.Spawn(pawn3, cell, psychicRitual.Map, WipeMode.Vanish);
                    flag = true;
                }
            }

            if (pawn == null)
            {
                Log.Error("Could not find target pawn for player's psylink skip abduction ritual.");
                return;
            }

            if (pawn.Dead)
            {
                Log.Error($"Psylink skip abduction ritual abducted a dead pawn. World pawn abducted: {flag}");
            }

            if (pawn.IsSubhuman)
            {
                Log.Error($"Psylink skip abduction ritual abducted a mutant. World pawn abducted: {flag}");
            }

            psychicRitual.Map.effecterMaintainer.AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(cell, psychicRitual.Map), cell, 60);
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(cell, psychicRitual.Map));
            PsychicRitualDef_PsylinkSkipAbductionPlayer mydef = (PsychicRitualDef_PsylinkSkipAbductionPlayer)psychicRitual.def;
            int ticksToDisappear = Mathf.RoundToInt(mydef.comaDurationDaysFromQualityCurve.Evaluate(psychicRitual.PowerPercent) * 60000f);
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.DarkPsychicShock, pawn);
            hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = ticksToDisappear;
            pawn.health.AddHediff(hediff);
            if (pawn.guest != null)
            {
                pawn.kindDef.initialResistanceRange *= 2f;
            }
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.PsychicRitualVictim);
            TaggedString text = "SkipAbductionPlayerCompleteText".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), pawn.Named("TARGET"), pawn.Faction.Named("FACTION"));
            Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), text, LetterDefOf.NeutralEvent, new LookTargets(pawn));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref invokerRole, "invokerRole");
        }
    }
}