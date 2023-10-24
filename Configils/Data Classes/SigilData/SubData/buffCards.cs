﻿using DiskCardGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JLPlugin.Data
{
    [System.Serializable]
    public class buffCards
    {
        public string runOnCondition;
        public string targetCard;
        public slotData slot;
        public string addStats;
        public string setStats;
        public string heal;
        public List<addAbilityData> addAbilities;
        public List<removeAbilityData> removeAbilities;

        public static IEnumerator BuffCards(AbilityBehaviourData abilitydata)
        {
            foreach (buffCards buffcardsinfo in abilitydata.buffCards)
            {
                if (SigilData.ConvertArgument(buffcardsinfo.runOnCondition, abilitydata) == "false")
                {
                    continue;
                }

                // yield return new WaitForSeconds(0.3f);
                Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, false);

                PlayableCard card = GetCard(abilitydata, buffcardsinfo);
                if (card == null) 
                    continue;
                
                CardModificationInfo mod = new CardModificationInfo();
                Heal(abilitydata, buffcardsinfo, card);
                AddStats(abilitydata, buffcardsinfo, mod);
                SetStats(abilitydata, buffcardsinfo, mod, card);
                
                if (buffcardsinfo.addAbilities != null || buffcardsinfo.removeAbilities != null) 
                    yield return PlayTransformAnimation(card);
                
                RemoveAbilities(abilitydata, buffcardsinfo, card, mod);
                AddAbilities(abilitydata, buffcardsinfo, card, mod);
                
                card.AddTemporaryMod(mod);
                card.OnStatsChanged();
                if (card.Health <= 0)
                {
                    yield return card.Die(false);
                }
            }

            // yield return new WaitForSeconds(0.3f);
            yield break;
        }

        private static void AddAbilities(AbilityBehaviourData abilitydata, buffCards buffcardsinfo, PlayableCard card,
            CardModificationInfo mod)
        {
            if (buffcardsinfo.addAbilities == null) 
                return;
            
            List<Ability> addSigils = new List<Ability>();
            List<Ability> addMergedSigils = new List<Ability>();
            foreach (addAbilityData sigilData in buffcardsinfo.addAbilities)
            {
                List<Ability> sigils = new List<Ability>();
                if (sigilData.name != null)
                {
                    sigils.Add(ImportExportUtils.ParseEnum<Ability>(SigilData.ConvertArgument(sigilData.name,
                        abilitydata)));
                }

                if (sigilData.list != null)
                {
                    sigils.AddRange(
                        (List<Ability>)SigilData.ConvertArgumentToType(sigilData.list, abilitydata, typeof(List<Ability>)));
                }

                foreach (Ability sigil in sigils)
                {
                    if (card.temporaryMods.Any(x => x.negateAbilities.Contains(sigil)))
                    {
                        card.temporaryMods.ForEach(x => x.negateAbilities = x.negateAbilities.Except(sigils).ToList());
                        card.Status.hiddenAbilities = card.Status.hiddenAbilities.Except(sigils).ToList();
                    }
                    else
                    {
                        if (SigilData.ConvertArgument(sigilData.infused, abilitydata) == "true")
                        {
                            addMergedSigils.Add(sigil);
                        }
                        else
                        {
                            addSigils.Add(sigil);
                        }
                    }
                }
            }

            if (addSigils.Count > 0)
            {
                mod.abilities.AddRange(addSigils);

                if (addMergedSigils.Count > 0)
                {
                    card.renderInfo.forceEmissivePortrait = true;
                    CardModificationInfo mergeMod = new CardModificationInfo()
                    {
                        abilities = addMergedSigils,
                        fromCardMerge = true
                    };

                    card.AddTemporaryMod(mergeMod);
                }
            }
            else if (addMergedSigils.Count > 0)
            {
                card.renderInfo.forceEmissivePortrait = true;
                mod.fromCardMerge = true;
                mod.abilities.AddRange(addMergedSigils);
            }
        }

        private static void RemoveAbilities(AbilityBehaviourData abilitydata, buffCards buffcardsinfo, PlayableCard card,
            CardModificationInfo mod)
        {
            if (buffcardsinfo.removeAbilities == null) 
                return;
            
            foreach (removeAbilityData sigilData in buffcardsinfo.removeAbilities)
            {
                List<Ability> sigils = new List<Ability>();
                if (sigilData.name != null)
                {
                    sigils.Add(ImportExportUtils.ParseEnum<Ability>(SigilData.ConvertArgument(sigilData.name,
                        abilitydata)));
                }

                if (sigilData.list != null)
                {
                    sigils.AddRange(
                        (List<Ability>)SigilData.ConvertArgumentToType(sigilData.list, abilitydata, typeof(List<Ability>)));
                }

                if (SigilData.ConvertArgument(sigilData.all, abilitydata) == "true")
                {
                    card.temporaryMods.ForEach(x => x.abilities = x.abilities.Except(sigils).ToList());
                    card.Status.hiddenAbilities.AddRange(sigils);
                    mod.negateAbilities.AddRange(sigils);
                }
                else
                {
                    foreach (Ability sigil in sigils)
                    {
                        if (!card.temporaryMods.Any(x => x.abilities.Contains(sigil)) &&
                            card.Info.Abilities.Contains(sigil))
                        {
                            mod.negateAbilities.Add(sigil);
                            card.Status.hiddenAbilities.Add(sigil);
                            continue;
                        }

                        for (int i = 0; i < card.temporaryMods.Count; i++)
                        {
                            if (card.temporaryMods[i].abilities.Contains(sigil))
                            {
                                card.temporaryMods[i].abilities.Remove(sigil);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerator PlayTransformAnimation(PlayableCard card)
        {
            yield return new WaitForSeconds(0.15f);
            card.Anim.PlayTransformAnimation();
            yield return new WaitForSeconds(0.15f);
        }

        private static void SetStats(AbilityBehaviourData abilitydata, buffCards buffcardsinfo, CardModificationInfo mod,
            PlayableCard card)
        {
            if (buffcardsinfo.setStats == null) 
                return;
            
            string attackAdjustment = SigilData.ConvertArgument(buffcardsinfo.setStats.Split('/')[0], abilitydata);
            if (attackAdjustment != "?")
            {
                mod.attackAdjustment += int.Parse(attackAdjustment) - card.Info.Attack;
            }

            string healthAdjustment = SigilData.ConvertArgument(buffcardsinfo.setStats.Split('/')[1], abilitydata);
            if (healthAdjustment != "?")
            {
                mod.healthAdjustment += int.Parse(healthAdjustment) - card.Info.Health;
            }
        }

        private static void AddStats(AbilityBehaviourData abilitydata, buffCards buffcardsinfo, CardModificationInfo mod)
        {
            if (buffcardsinfo.addStats == null) 
                return;
            
            string attackAdjustment = SigilData.ConvertArgument(buffcardsinfo.addStats.Split('/')[0], abilitydata);
            if (attackAdjustment != "?")
            {
                mod.attackAdjustment += int.Parse(attackAdjustment);
            }

            string healthAdjustment = SigilData.ConvertArgument(buffcardsinfo.addStats.Split('/')[1], abilitydata);
            if (healthAdjustment != "?")
            {
                mod.healthAdjustment += int.Parse(healthAdjustment);
            }
        }

        private static void Heal(AbilityBehaviourData abilitydata, buffCards buffcardsinfo, PlayableCard card)
        {
            if (buffcardsinfo.heal == null) return;
            if (card.Status.damageTaken <= 0) return;
            
            card.HealDamage(Math.Min(card.Status.damageTaken,
                int.Parse(SigilData.ConvertArgument(buffcardsinfo.heal, abilitydata))));
        }

        private static PlayableCard GetCard(AbilityBehaviourData abilitydata, buffCards buffcardsinfo)
        {
            PlayableCard card = null;
            if (buffcardsinfo.slot != null)
            {
                CardSlot slot = slotData.GetSlot(buffcardsinfo.slot, abilitydata);
                if (slot != null)
                {
                    if (slot.Card != null)
                    {
                        card = slot.Card;
                    }
                }
            }
            else
            {
                if (buffcardsinfo.targetCard != null)
                {
                    card = (PlayableCard)SigilData.ConvertArgumentToType(buffcardsinfo.targetCard, abilitydata,
                        typeof(PlayableCard));
                }
                else
                {
                    card = abilitydata.self;
                }
            }

            return card;
        }
    }
}
