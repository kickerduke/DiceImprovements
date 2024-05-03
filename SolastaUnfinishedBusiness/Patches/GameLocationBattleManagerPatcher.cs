﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Feats;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Subclasses;
using SolastaUnfinishedBusiness.Validators;
using TA;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class GameLocationBattleManagerPatcher
{
    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.CanCharacterUsePower))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CanCharacterUsePower_Patch
    {
        [UsedImplicitly]
        public static void Postfix(
            ref bool __result,
            RulesetCharacter caster,
            RulesetUsablePower usablePower)
        {
            //PATCH: ensure Zen Archer Hail of Arrows won't trigger PowerMonkMartialArts
            if (__result &&
                usablePower.PowerDefinition == DatabaseHelper.FeatureDefinitionPowers.PowerMonkMartialArts)
            {
                var currentAttackAction = Global.CurrentAttackAction;

                if (currentAttackAction.Count > 0 &&
                    currentAttackAction.Peek().ActionParams.AttackMode.AttackTags
                        .Contains(WayOfZenArchery.HailOfArrows))
                {
                    __result = false;
                }
            }

            //PATCH: support for `IValidatePowerUse` when trying to react with power 
            if (__result && !caster.CanUsePower(usablePower.PowerDefinition))
            {
                __result = false;
            }

            //PATCH: support for `IReactionAttackModeRestriction`
            if (__result)
            {
                __result = RestrictReactionAttackMode.CanCharacterReactWithPower(usablePower);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.CanPerformReadiedActionOnCharacter))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CanPerformReadiedActionOnCharacter_Patch
    {
        [NotNull]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            //PATCH: Makes only preferred cantrip valid if it is selected and forced
            var customBindMethod =
                new Func<List<SpellDefinition>, SpellDefinition, bool>(CustomReactionsContext.CheckAndModifyCantrips)
                    .Method;

            //PATCH: allows to ready non-standard ranged attacks (like Armorer's Lightning Launcher)
            var customFindMethod =
                new Func<
                        GameLocationCharacter, // character,
                        ActionDefinitions.Id, // actionId,
                        bool, // getWithMostAttackNb,
                        bool, // onlyIfRemainingUses,
                        bool, // onlyIfCanUseAction
                        ActionDefinitions.ReadyActionType, // readyActionType
                        RulesetAttackMode //result
                    >(FindActionAttackMode)
                    .Method;

            return instructions
                .ReplaceCall(
                    "Contains",
                    -1,
                    "GameLocationBattleManager.CanPerformReadiedActionOnCharacter.Contains",
                    new CodeInstruction(OpCodes.Call, customBindMethod))
                .ReplaceCall(
                    "FindActionAttackMode",
                    -1,
                    "GameLocationBattleManager.CanPerformReadiedActionOnCharacter.FindActionAttackMode",
                    new CodeInstruction(OpCodes.Call, customFindMethod)
                );
        }

        private static RulesetAttackMode FindActionAttackMode(
            GameLocationCharacter character,
            ActionDefinitions.Id actionId,
            bool getWithMostAttackNb,
            bool onlyIfRemainingUses,
            bool onlyIfCanUseAction,
            ActionDefinitions.ReadyActionType readyActionType)
        {
            var attackMode = character.FindActionAttackMode(
                actionId, getWithMostAttackNb, onlyIfRemainingUses, onlyIfCanUseAction, readyActionType);

            if (readyActionType != ActionDefinitions.ReadyActionType.Ranged)
            {
                return attackMode;
            }

            if (attackMode != null && (attackMode.Ranged || attackMode.Thrown))
            {
                return attackMode;
            }

            return character.GetFirstRangedModeThatCanBeReadied();
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.IsValidAttackForReadiedAction))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class IsValidAttackForReadiedAction_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ref bool __result, BattleDefinitions.AttackEvaluationParams attackParams)
        {
            //PATCH: Checks if attack cantrip is valid to be cast as readied action on a target
            // Used to properly check if melee cantrip can hit target when used for readied action

            if (!DatabaseHelper.TryGetDefinition<SpellDefinition>(attackParams.effectName, out var cantrip))
            {
                return;
            }

            var canAttack = cantrip.GetFirstSubFeatureOfType<IAttackAfterMagicEffect>()?.CanAttack;

            if (canAttack != null)
            {
                __result = canAttack(attackParams.attacker, attackParams.defender);
            }
        }
    }

    // useful to debug powers that start automatically on rage
#if false
    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.HandleReactionToRageStart))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleReactionToRageStart_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter rager)
        {
            var actionService = ServiceRepository.GetService<IGameLocationActionService>();
            var implementationService = ServiceRepository.GetService<IRulesetImplementationService>();

            rager.RulesetCharacter.EnumerateFeaturesToBrowse<FeatureDefinitionPower>(
                rager.RulesetCharacter.FeaturesToBrowse);
            
            foreach (var usablePower in rager.RulesetCharacter.UsablePowers)
            {
                if (rager.RulesetCharacter.GetRemainingUsesOfPower(usablePower) > 0 &&
                    usablePower.PowerDefinition.ActivationTime == ActivationTime.OnRageStartAutomatic)
                {
                    var characterActionParams = new CharacterActionParams(rager, ActionDefinitions.Id.SpendPower)
                    {
                        StringParameter = usablePower.PowerDefinition.Name,
                        RulesetEffect = implementationService
                            .InstantiateEffectPower(rager.RulesetCharacter, usablePower, false),
                        TargetCharacters = { rager }
                    };

                    actionService.ExecuteAction(characterActionParams, null, true);
                }
                else if (rager.RulesetCharacter.GetRemainingUsesOfPower(usablePower) > 0 &&
                         usablePower.PowerDefinition.ActivationTime == ActivationTime.OnRageStartChoice)
                {
                    var reactionParams = new CharacterActionParams(rager, ActionDefinitions.Id.SpendPower)
                    {
                        StringParameter = usablePower.PowerDefinition.Name,
                        RulesetEffect = implementationService
                            .InstantiateEffectPower(rager.RulesetCharacter, usablePower, false),
                        TargetCharacters = { rager },
                        IsReactionEffect = true
                    };

                    var count = actionService.PendingReactionRequestGroups.Count;

                    actionService.ReactToSpendPower(reactionParams);

                    yield return __instance.WaitForReactions(rager, actionService, count);
                }
            }
        }
    }
#endif

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.HandleFailedAbilityCheck))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleFailedAbilityCheck_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            CharacterAction action,
            GameLocationCharacter checker,
            ActionModifier abilityCheckModifier)
        {
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            //PATCH: support for `ITryAlterOutcomeAttributeCheck`
            foreach (var tryAlterOutcomeSavingThrow in TryAlterOutcomeAttributeCheck.Handler(
                         __instance, action, checker, abilityCheckModifier))
            {
                yield return tryAlterOutcomeSavingThrow;
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.HandleCharacterMoveStart))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterMoveStart_Patch
    {
        [UsedImplicitly]
        public static void Prefix(
            GameLocationCharacter mover,
            int3 destination)
        {
            //PATCH: support for Polearm Expert AoO
            //Stores character movements to be processed later
            AttacksOfOpportunity.ProcessOnCharacterMoveStart(mover, destination);

            //PATCH: records on StraightLine special feature how many cells a hero moved in a straight line
            //Stores character last straight line distance to be processed later
            MeleeCombatFeats.PhysicalAttackBeforeHitConfirmedOnEnemyCharger.RecordStraightLine(mover, destination);
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.HandleCharacterMoveEnd))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterMoveEnd_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter mover)
        {
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            if (__instance.Battle == null ||
                mover.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            //PATCH: support for Polearm Expert AoO. processes saved movement to trigger AoO when appropriate
            var extraEvents = AttacksOfOpportunity.ProcessOnCharacterMoveEnd(__instance, mover);

            while (extraEvents.MoveNext())
            {
                yield return extraEvents.Current;
            }

            //PATCH: set cursor to dirty and reprocess valid positions if ally was moved by Gambit or Warlord
            if (mover.IsMyTurn() || mover.Side != Side.Ally)
            {
                yield break;
            }

            var cursorService = ServiceRepository.GetService<ICursorService>();
            var cursorLocationBattleFriendlyTurn =
                cursorService.AllCursors.OfType<CursorLocationBattleFriendlyTurn>().First();

            if (!cursorLocationBattleFriendlyTurn.Active)
            {
                yield break;
            }

            cursorLocationBattleFriendlyTurn.dirty = true;
            cursorLocationBattleFriendlyTurn.ComputeValidDestinations();
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.PrepareBattleEnd))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class PrepareBattleEnd_Patch
    {
        [UsedImplicitly]
        public static void Prefix()
        {
            //PATCH: support for Polearm Expert AoO
            //clears movement cache on battle end
            AttacksOfOpportunity.CleanMovingCache();
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleCharacterAttackHitConfirmed))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterAttackHitConfirmed_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier attackModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            RulesetEffect rulesetEffect,
            bool criticalHit,
            bool firstTarget)
        {
            if (rulesetEffect != null)
            {
                while (values.MoveNext())
                {
                    yield return values.Current;
                }

                yield break;
            }

            //PATCH: support for `IPhysicalAttackBeforeHitConfirmedOnEnemy`
            // should also happen outside battles
            foreach (var attackBeforeHitConfirmedOnEnemy in attacker.RulesetCharacter
                         .GetSubFeaturesByType<IPhysicalAttackBeforeHitConfirmedOnEnemy>())
            {
                yield return attackBeforeHitConfirmedOnEnemy.OnPhysicalAttackBeforeHitConfirmedOnEnemy(
                    __instance, attacker, defender, attackModifier, attackMode,
                    rangedAttack, advantageType, actualEffectForms, firstTarget, criticalHit);
            }

            //PATCH: support for `IPhysicalAttackBeforeHitConfirmedOnMe`
            if (__instance.Battle != null)
            {
                foreach (var attackBeforeHitConfirmedOnMe in defender.RulesetCharacter
                             .GetSubFeaturesByType<IPhysicalAttackBeforeHitConfirmedOnMe>())
                {
                    yield return attackBeforeHitConfirmedOnMe.OnPhysicalAttackBeforeHitConfirmedOnMe(
                        __instance, attacker, defender, attackModifier, attackMode,
                        rangedAttack, advantageType, actualEffectForms, firstTarget, criticalHit);
                }
            }

            while (values.MoveNext())
            {
                yield return values.Current;
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleAttackerTriggeringPowerOnCharacterAttackHitConfirmed))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleAttackerTriggeringPowerOnCharacterAttackHitConfirmed_Patch
    {
        [UsedImplicitly]
        public static void Prefix(
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect)
        {
            //PATCH: support for `IReactionAttackModeRestriction`
            RestrictReactionAttackMode.ReactionContext = (action, attacker, defender, attackMode, rulesetEffect);
        }

        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values)
        {
            //PATCH: support for `IReactionAttackModeRestriction`
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            RestrictReactionAttackMode.ReactionContext = (null, null, null, null, null);
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleDefenderBeforeDamageReceived))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleDefenderBeforeDamageReceived_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect,
            ActionModifier attackModifier,
            bool rolledSavingThrow,
            bool saveOutcomeSuccess)
        {
            //PATCH: support for features that trigger when defender gets hit, like `FeatureDefinitionReduceDamage` 
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            var defenderCharacter = defender.RulesetCharacter;

            if (defenderCharacter is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            // Not actually used currently, but may be useful for future features.
            // var selfDamage = attacker.RulesetCharacter == defenderCharacter;

            // Not actually used currently, but may be useful for future features.
            // var canPerceiveAttacker = selfDamage
            //                           || defender.PerceivedFoes.Contains(attacker)
            //                           || defender.PerceivedAllies.Contains(attacker);

            foreach (var feature in defenderCharacter
                         .GetFeaturesByType<FeatureDefinitionReduceDamage>())
            {
                var isValid = defenderCharacter.IsValid(feature.GetAllSubFeaturesOfType<IsCharacterValidHandler>());

                if (!isValid)
                {
                    continue;
                }

                var canReact = defender.CanReact();

                //TODO: add ability to specify whether this feature can reduce magic damage
                var damageTypes = feature.DamageTypes;
                var damage = attackMode?.EffectDescription?.FindFirstDamageFormOfType(damageTypes);

                // In case of a ruleset effect, check that it shall apply damage forms, otherwise don't proceed (e.g. CounterSpell)
                if (rulesetEffect?.EffectDescription != null)
                {
                    var canForceHalfDamage = false;

                    if (rulesetEffect is RulesetEffectSpell activeSpell)
                    {
                        canForceHalfDamage = attacker.RulesetCharacter.CanForceHalfDamage(activeSpell.SpellDefinition);
                    }

                    var effectDescription = rulesetEffect.EffectDescription;

                    if (rolledSavingThrow)
                    {
                        damage = saveOutcomeSuccess
                            ? effectDescription.FindFirstNonNegatedDamageFormOfType(canForceHalfDamage, damageTypes)
                            : effectDescription.FindFirstDamageFormOfType(damageTypes);
                    }
                    else
                    {
                        damage = effectDescription.FindFirstDamageFormOfType(damageTypes);
                    }
                }

                if (damage == null)
                {
                    continue;
                }

                var totalReducedDamage = 0;

                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (feature.TriggerCondition)
                {
                    // Can I always reduce a fixed damage amount (i.e.: Heavy Armor Feat)
                    case AdditionalDamageTriggerCondition.AlwaysActive:
                        totalReducedDamage = feature.ReducedDamage(attacker, defender);
                        break;

                    // Can I reduce the damage consuming slots? (i.e.: Blade Dancer)
                    case AdditionalDamageTriggerCondition.SpendSpellSlot:
                    {
                        if (!canReact)
                        {
                            continue;
                        }

                        var repertoire = defenderCharacter.SpellRepertoires
                            .Find(x => x.spellCastingClass == feature.SpellCastingClass);

                        if (repertoire == null)
                        {
                            continue;
                        }

                        if (!repertoire.AtLeastOneSpellSlotAvailable())
                        {
                            continue;
                        }

                        var actionService = ServiceRepository.GetService<IGameLocationActionService>();
                        var count = actionService.PendingReactionRequestGroups.Count;
                        var reactionParams = new CharacterActionParams(defender, ActionDefinitions.Id.SpendSpellSlot)
                        {
                            IntParameter = 1,
                            StringParameter = feature.NotificationTag,
                            SpellRepertoire = repertoire
                        };

                        actionService.ReactToSpendSpellSlot(reactionParams);

                        yield return __instance.WaitForReactions(attacker, actionService, count);

                        if (!reactionParams.ReactionValidated)
                        {
                            continue;
                        }

                        totalReducedDamage = feature.ReducedDamage(attacker, defender) * reactionParams.IntParameter;
                        break;
                    }
                }

                if (totalReducedDamage <= 0)
                {
                    continue;
                }

                var tag = $"{feature.Name}:{defender.Guid}:{totalReducedDamage}";

                attackMode?.AttackTags.Add(tag);
                rulesetEffect?.SourceTags.Add(tag);

                defenderCharacter.DamageReduced(defenderCharacter, feature, totalReducedDamage);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.CanAttack))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CanAttack_Patch
    {
        [UsedImplicitly]
        public static void Postfix(
            bool __result,
            BattleDefinitions.AttackEvaluationParams attackParams)
        {
            //PATCH: support for features removing ranged attack disadvantage
            RemoveRangedAttackInMeleeDisadvantage.CheckToRemoveRangedDisadvantage(attackParams);

            //PATCH: check if weapon has MagicAffinityInfusionEnhanceArcaneFocus Infusion
            //TODO: create an interface if ever required by other use cases
            if (attackParams.attacker.RulesetActor is RulesetCharacter rulesetCharacter &&
                rulesetCharacter.Items
                    .Any(x => x.DynamicItemProperties
                        .Any(y => y.FeatureDefinition.Name == "MagicAffinityInfusionEnhanceArcaneFocus")))
            {
                attackParams.attackModifier.coverType = CoverType.None;
            }

            if (!__result)
            {
                return;
            }

            //PATCH: supports `UseOfficialLightingObscurementAndVisionRules`
            //handle lighting and obscurement logic disabled in `GLC.ComputeLightingModifierForIlluminable`
            LightingAndObscurementContext.ApplyObscurementRules(attackParams);

            //PATCH: add modifier or advantage/disadvantage for physical and spell attack
            ApplyCustomModifiers(attackParams);
        }

        private static void ApplyCustomModifiers(BattleDefinitions.AttackEvaluationParams attackParams)
        {
            var attacker = attackParams.attacker.RulesetCharacter;
            var defender = attackParams.defender.RulesetCharacter;

            if (attacker == null || defender == null)
            {
                return;
            }

            foreach (var modifyAttackActionModifier in attacker.GetSubFeaturesByType<IModifyAttackActionModifier>())
            {
                modifyAttackActionModifier.OnAttackComputeModifier(
                    attacker,
                    defender,
                    attackParams.attackProximity,
                    attackParams.attackMode,
                    attackParams.effectName,
                    ref attackParams.attackModifier);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleAdditionalDamageOnCharacterAttackHitConfirmed))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleAdditionalDamageOnCharacterAttackHitConfirmed_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(
            GameLocationBattleManager __instance,
            out IEnumerator __result,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier attackModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            RulesetEffect rulesetEffect,
            bool criticalHit,
            bool firstTarget)
        {
            //PATCH: Completely replace this method to support several features. Modified method based on TA provided sources.
            __result = GLBM.HandleAdditionalDamageOnCharacterAttackHitConfirmed(
                __instance, attacker, defender, attackModifier, attackMode, rangedAttack, advantageType,
                actualEffectForms, rulesetEffect, criticalHit, firstTarget);

            return false;
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.ComputeAndNotifyAdditionalDamage))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class ComputeAndNotifyAdditionalDamage_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(
            GameLocationBattleManager __instance,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            IAdditionalDamageProvider provider,
            List<EffectForm> actualEffectForms,
            CharacterActionParams reactionParams,
            RulesetAttackMode attackMode,
            bool criticalHit)
        {
            //PATCH: Completely replace this method to support several features. Modified method based on TA provided sources.
            GLBM.ComputeAndNotifyAdditionalDamage(
                __instance, attacker, defender, provider, actualEffectForms, reactionParams, attackMode, criticalHit);

            return false;
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.HandleTargetReducedToZeroHP))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public static class HandleTargetReducedToZeroHP_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter attacker,
            GameLocationCharacter downedCreature,
            RulesetAttackMode rulesetAttackMode,
            RulesetEffect activeEffect)
        {
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            if (__instance.Battle == null)
            {
                yield break;
            }

            //PATCH: Support for `IOnReducedToZeroHpByMe` feature
            foreach (var onReducedToZeroHpByMe in
                     attacker.RulesetActor.GetSubFeaturesByType<IOnReducedToZeroHpByMe>())
            {
                yield return onReducedToZeroHpByMe.HandleReducedToZeroHpByMe(
                    attacker, downedCreature, rulesetAttackMode, activeEffect);
            }

            if (__instance.Battle != null)
            {
                //PATCH: Support for `IOnReducedToZeroHpByMeOrAlly` feature
                foreach (var ally in __instance.Battle
                             .GetContenders(attacker, isOppositeSide: false, excludeSelf: false))
                {
                    foreach (var onReducedToZeroHpByMeOrAlly in
                             ally.RulesetActor.GetSubFeaturesByType<IOnReducedToZeroHpByMeOrAlly>())
                    {
                        yield return onReducedToZeroHpByMeOrAlly.HandleReducedToZeroHpByMeOrAlly(
                            attacker, downedCreature, ally, rulesetAttackMode, activeEffect);
                    }
                }
            }

            // ReSharper disable once InvertIf
            if (__instance.Battle != null)
            {
                //PATCH: Support for `IOnReducedToZeroHpByEnemy` feature
                foreach (var onReducedToZeroHpByEnemy in downedCreature.RulesetActor
                             .GetSubFeaturesByType<IOnReducedToZeroHpByEnemy>())
                {
                    yield return onReducedToZeroHpByEnemy.HandleReducedToZeroHpByEnemy(
                        attacker, downedCreature, rulesetAttackMode, activeEffect);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleCharacterMagicalAttackHitConfirmed))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterMagicalAttackHitConfirmed_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier magicModifier,
            RulesetEffect rulesetEffect,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            //PATCH: support for `IMagicEffectBeforeHitConfirmedOnEnemy`
            // should also happen outside battles
            if (attacker.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false })
            {
                var controller = attacker.GetEffectControllerOrSelf();

                foreach (var magicalAttackBeforeHitConfirmedOnEnemy in controller.RulesetCharacter
                             .GetSubFeaturesByType<IMagicEffectBeforeHitConfirmedOnEnemy>())
                {
                    yield return magicalAttackBeforeHitConfirmedOnEnemy.OnMagicEffectBeforeHitConfirmedOnEnemy(
                        __instance, controller, defender, magicModifier, rulesetEffect, actualEffectForms, firstTarget,
                        criticalHit);
                }

                if (rulesetEffect is { SourceDefinition: SpellDefinition spellDefinition })
                {
                    //PATCH: illusionary spells against creatures with True Sight should automatically save
                    if (Main.Settings.IllusionSpellsAutomaticallyFailAgainstTrueSightInRange &&
                        spellDefinition.SchoolOfMagic == SchoolIllusion &&
                        spellDefinition.EffectDescription.TargetSide == Side.Enemy &&
                        spellDefinition != DatabaseHelper.SpellDefinitions.Silence)
                    {
                        var rulesetDefender = defender.RulesetCharacter;
                        var senseMode =
                            rulesetDefender.SenseModes.FirstOrDefault(x => x.SenseType == SenseMode.Type.Truesight);

                        if (senseMode != null && attacker.IsWithinRange(defender, senseMode.SenseRange))
                        {
                            var console = Gui.Game.GameConsole;
                            var entry = new GameConsoleEntry(
                                "Feedback/&TrueSightAndIllusionSpells", console.consoleTableDefinition)
                            {
                                Indent = true
                            };

                            console.AddCharacterEntry(rulesetDefender, entry);
                            console.AddEntry(entry);
                            actualEffectForms.Clear();
                        }
                    }

                    var magicalAttackBeforeHitConfirmedOnEnemy =
                        spellDefinition.GetFirstSubFeatureOfType<IMagicEffectBeforeHitConfirmedOnEnemy>();

                    yield return magicalAttackBeforeHitConfirmedOnEnemy?.OnMagicEffectBeforeHitConfirmedOnEnemy(
                        __instance, controller, defender, magicModifier, rulesetEffect, actualEffectForms, firstTarget,
                        criticalHit);
                }
            }

            //PATCH: support for `IMagicEffectBeforeHitConfirmedOnMe` on SPELLS
            // should also happen outside battles
            if (defender.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false })
            {
                foreach (var magicalAttackBeforeHitConfirmedOnMe in defender.RulesetCharacter.usableSpells
                             .Where(usableSpell =>
                                 usableSpell.ActivationTime == ActivationTime.Reaction)
                             .SelectMany(x => x.GetAllSubFeaturesOfType<IMagicEffectBeforeHitConfirmedOnMe>())
                             .ToList())
                {
                    yield return magicalAttackBeforeHitConfirmedOnMe.OnMagicEffectBeforeHitConfirmedOnMe(
                        __instance, attacker, defender, magicModifier, rulesetEffect, actualEffectForms,
                        firstTarget, criticalHit);
                }
            }

            //PATCH: support for `IMagicEffectBeforeHitConfirmedOnMe`
            // should also happen outside battles
            if (defender.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false })
            {
                foreach (var magicalAttackBeforeHitConfirmedOnMe in defender.RulesetCharacter
                             .GetSubFeaturesByType<IMagicEffectBeforeHitConfirmedOnMe>())
                {
                    yield return magicalAttackBeforeHitConfirmedOnMe.OnMagicEffectBeforeHitConfirmedOnMe(
                        __instance, attacker, defender, magicModifier, rulesetEffect, actualEffectForms,
                        firstTarget, criticalHit);
                }
            }

            while (values.MoveNext())
            {
                yield return values.Current;
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleCharacterPhysicalAttackInitiated))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterPhysicalAttackInitiated_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
#pragma warning disable IDE0060
            //values are not used but required for patch to work
            [NotNull] IEnumerator values,
#pragma warning restore IDE0060
            GameLocationBattleManager __instance,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier attackModifier,
            RulesetAttackMode attackerAttackMode)
        {
            //PATCH: registers which weapon types were used so far on attacks
            ValidatorsCharacter.RegisterWeaponTypeUsed(attacker, attackerAttackMode);

            //PATCH: allow custom behavior when physical attack initiates
            if (__instance.Battle != null)
            {
                foreach (var attackInitiated in
                         attacker.RulesetCharacter.GetSubFeaturesByType<IPhysicalAttackInitiatedByMe>())
                {
                    yield return attackInitiated.OnPhysicalAttackInitiatedByMe(
                        __instance, action, attacker, defender, attackModifier, attackerAttackMode);
                }
            }

            //PATCH: allow custom behavior when physical attack initiates on me
            if (__instance.Battle != null)
            {
                foreach (var attackInitiated in
                         defender.RulesetCharacter.GetSubFeaturesByType<IPhysicalAttackInitiatedOnMe>())
                {
                    yield return attackInitiated.OnPhysicalAttackInitiatedOnMe(
                        __instance, action, attacker, defender, attackModifier, attackerAttackMode);
                }
            }

            //PATCH: allow custom behavior when physical attack initiates on me or ally
            if (__instance.Battle != null)
            {
                foreach (var ally in __instance.Battle.GetContenders(attacker))
                {
                    foreach (var physicalAttackInitiatedOnMeOrAlly in ally.RulesetCharacter
                                 .GetSubFeaturesByType<IPhysicalAttackInitiatedOnMeOrAlly>())
                    {
                        yield return physicalAttackInitiatedOnMeOrAlly.OnPhysicalAttackInitiatedOnMeOrAlly(
                            __instance, action, attacker, defender, ally, attackModifier, attackerAttackMode);
                    }
                }
            }

            if (__instance.Battle == null)
            {
                yield break;
            }

            // pretty much vanilla code from here

            ++defender.SustainedAttacks;

            var rulesetCharacter = attacker.RulesetCharacter;

            if (rulesetCharacter != null)
            {
                foreach (var usablePower in rulesetCharacter.UsablePowers
                             .Where(usablePower =>
                                 __instance.CanCharacterUsePower(rulesetCharacter, defender, usablePower) &&
                                 usablePower.PowerDefinition.ActivationTime ==
                                 ActivationTime.OnAttackHitMartialArts && attackerAttackMode != null &&
                                 action.ActionId != ActionDefinitions.Id.AttackReadied &&
                                 rulesetCharacter.IsWieldingMonkWeapon() &&
                                 !rulesetCharacter.IsWearingArmor() &&
                                 !rulesetCharacter.HasConditionOfTypeOrSubType(ConditionMagicallyArmored) &&
                                 // BEGIN PATCH
                                 (!rulesetCharacter.IsWearingShield() || rulesetCharacter.HasMonkShieldExpert()) &&
                                 // END PATCH
                                 !rulesetCharacter.HasConditionOfType(ConditionMonkDeflectMissile) &&
                                 !rulesetCharacter.HasConditionOfType(ConditionMonkMartialArtsUnarmedStrikeBonus) &&
                                 attacker.GetActionTypeStatus(ActionDefinitions.ActionType.Bonus) ==
                                 ActionDefinitions.ActionStatus.Available))
                {
                    __instance.PrepareAndExecuteSpendPowerAction(attacker, defender, usablePower);
                }
            }

            foreach (var opposingContender in __instance.Battle.GetContenders(attacker, withinRange: 1)
                         .Where(opposingContender =>
                             opposingContender != defender &&
                             opposingContender.GetActionTypeStatus(ActionDefinitions.ActionType.Reaction) ==
                             ActionDefinitions.ActionStatus.Available &&
                             opposingContender.GetActionStatus(ActionDefinitions.Id.BlockAttack,
                                 ActionDefinitions.ActionScope.Battle, ActionDefinitions.ActionStatus.Available) ==
                             ActionDefinitions.ActionStatus.Available))
            {
                yield return __instance.PrepareAndReact(
                    opposingContender, attacker, attacker, ActionDefinitions.Id.BlockAttack, attackModifier,
                    additionalTargetCharacter: defender);
                break;
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleCharacterPhysicalAttackFinished))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterPhysicalAttackFinished_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            CharacterAction attackAction,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackerAttackMode,
            RollOutcome attackRollOutcome,
            int damageAmount)
        {
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            if (__instance.Battle != null && attacker.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false })
            {
                //PATCH: allow custom behavior when physical attack finished
                foreach (var feature in attacker.RulesetCharacter
                             .GetSubFeaturesByType<IPhysicalAttackFinishedByMe>())
                {
                    yield return feature.OnPhysicalAttackFinishedByMe(
                        __instance, attackAction, attacker, defender, attackerAttackMode, attackRollOutcome,
                        damageAmount);
                }
            }

            if (__instance.Battle != null && defender.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false })
            {
                //PATCH: allow custom behavior when physical attack finished on defender
                foreach (var feature in defender.RulesetCharacter
                             .GetSubFeaturesByType<IPhysicalAttackFinishedOnMe>())
                {
                    yield return feature.OnPhysicalAttackFinishedOnMe(
                        __instance, attackAction, attacker, defender, attackerAttackMode, attackRollOutcome,
                        damageAmount);
                }
            }

            if (__instance.Battle != null)
            {
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var gameLocationAlly in __instance.Battle.GetContenders(attacker, isOppositeSide: false,
                             excludeSelf: false))
                {
                    var allyFeatures =
                        gameLocationAlly.RulesetCharacter.GetSubFeaturesByType<IPhysicalAttackFinishedByMeOrAlly>();

                    foreach (var feature in allyFeatures)
                    {
                        yield return feature.OnPhysicalAttackFinishedByMeOrAlly(
                            __instance, attackAction, attacker, defender, gameLocationAlly, attackerAttackMode,
                            attackRollOutcome,
                            damageAmount);
                    }
                }
            }

            // ReSharper disable once InvertIf
            if (__instance.Battle != null)
            {
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var gameLocationAlly in __instance.Battle.GetContenders(attacker))
                {
                    var allyFeatures =
                        gameLocationAlly.RulesetCharacter.GetSubFeaturesByType<IPhysicalAttackFinishedOnMeOrAlly>();

                    foreach (var feature in allyFeatures)
                    {
                        yield return feature.OnPhysicalAttackFinishedOnMeOrAlly(
                            __instance, attackAction, attacker, defender, gameLocationAlly, attackerAttackMode,
                            attackRollOutcome,
                            damageAmount);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.HandleSpellCast))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleSpellCast_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationCharacter caster,
            CharacterActionCastSpell castAction,
            RulesetEffectSpell selectEffectSpell,
            RulesetSpellRepertoire selectedRepertoire,
            SpellDefinition selectedSpellDefinition)
        {
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            // This also allows utilities out of battle
            var characterService = ServiceRepository.GetService<IGameLocationCharacterService>();
            var allyCharacters = characterService.PartyCharacters.Select(x => x.RulesetCharacter);

            foreach (var allyCharacter in allyCharacters.Where(x => x is { IsDeadOrDyingOrUnconscious: false }))
            {
                var magicalAttackCastedSpells = allyCharacter.GetSubFeaturesByType<IOnSpellCasted>();

                foreach (var magicalAttackCastedSpell in magicalAttackCastedSpells)
                {
                    yield return magicalAttackCastedSpell.OnSpellCasted(
                        allyCharacter, caster, castAction, selectEffectSpell, selectedRepertoire,
                        selectedSpellDefinition);
                }
            }

            //PATCH: support the one case we need to check a behavior on enemy so no interface unless required
            // ReSharper disable once InvertIf
            if (caster.Side == Side.Enemy && Gui.Battle != null)
            {
                foreach (var ally in Gui.Battle.GetContenders(caster, withinRange: 1)
                             .Where(x =>
                                 x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } rulesetCharacter &&
                                 rulesetCharacter.GetOriginalHero() is { } rulesetCharacterHero &&
                                 rulesetCharacterHero.TrainedFeats.Contains(OtherFeats.FeatMageSlayer)))
                {
                    yield return
                        OtherFeats.CustomBehaviorMageSlayer.HandleEnemyCastSpellWithin5Ft(caster, ally);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager),
        nameof(GameLocationBattleManager.HandleCharacterAttackHitPossible))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class HandleCharacterAttackHitPossible_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix(
            IEnumerator values,
            GameLocationBattleManager __instance,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect,
            ActionModifier attackModifier,
            int attackRoll)
        {
            while (values.MoveNext())
            {
                yield return values.Current;
            }

            // ReSharper disable once InvertIf
            if (__instance.Battle != null)
            {
                //PATCH: Support for features before hit possible, e.g. spiritual shielding
                foreach (var contender in __instance.Battle.GetContenders(attacker))
                {
                    foreach (var attackBeforeHitPossibleOnMeOrAlly in contender.RulesetCharacter
                                 .GetSubFeaturesByType<IAttackBeforeHitPossibleOnMeOrAlly>())
                    {
                        yield return attackBeforeHitPossibleOnMeOrAlly.OnAttackBeforeHitPossibleOnMeOrAlly(
                            __instance, attacker, defender, contender, attackModifier, attackMode,
                            rulesetEffect, attackRoll);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocationBattleManager), nameof(GameLocationBattleManager.ComputeCover))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class ComputeCover_Patch
    {
        [UsedImplicitly]
        public static void Prefix(
            GameLocationCharacter attacker,
            int3 attackerPosition,
            GameLocationCharacter defender,
            int3 defenderPosition,
            ActionModifier attackModifier,
            ref CoverType bestCoverType,
            ref bool ignoreCoverFromCharacters)
        {
            if (attacker.UsedSpecialFeatures.ContainsKey("FamiliarAttack"))
            {
                ignoreCoverFromCharacters = true;
            }

            var modifiers = defender.RulesetCharacter.GetSubFeaturesByType<IModifyCoverType>();

            foreach (var modifier in modifiers)
            {
                modifier.ModifyCoverType(
                    attacker, attackerPosition,
                    defender, defenderPosition,
                    attackModifier, ref bestCoverType, ignoreCoverFromCharacters);
            }
        }
    }
}
