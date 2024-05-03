﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterFamilyDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.ConditionDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionActionAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionMovementAffinitys;
using MirrorImage = SolastaUnfinishedBusiness.Behaviors.Specific.MirrorImage;

namespace SolastaUnfinishedBusiness.Spells;

internal static partial class SpellBuilders
{
    #region Binding Ice

    internal static SpellDefinition BuildBindingIce()
    {
        const string NAME = "BindingIce";

        var spriteReference = Sprites.GetSprite("WinterBreath", Resources.WinterBreath, 128);

        var conditionGrappledRestrainedIceBound = ConditionDefinitionBuilder
            .Create(ConditionGrappledRestrainedRemorhaz, "ConditionGrappledRestrainedIceBound")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetFeatures(MovementAffinityConditionRestrained, ActionAffinityConditionRestrained, ActionAffinityGrappled)
            //.SetParentCondition(ConditionDefinitions.ConditionRestrained)
            .AddToDB();

        conditionGrappledRestrainedIceBound.specialDuration = false;
        conditionGrappledRestrainedIceBound.specialInterruptions.Clear();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, spriteReference)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(2)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(false)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Cone, 6)
                    .ExcludeCaster()
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Constitution,
                        true,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetParticleEffectParameters(ConeOfCold)
                    .AddEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeCold, 3, DieType.D8)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build())
                    .AddEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionGrappledRestrainedIceBound, ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .Build())
            .AddToDB();

        spell.EffectDescription.EffectParticleParameters.conditionParticleReference =
            PowerDomainElementalHeraldOfTheElementsCold.EffectDescription.EffectParticleParameters
                .conditionParticleReference;

        spell.EffectDescription.EffectParticleParameters.conditionStartParticleReference =
            PowerDomainElementalHeraldOfTheElementsCold.EffectDescription.EffectParticleParameters
                .conditionStartParticleReference;

        spell.EffectDescription.EffectParticleParameters.conditionEndParticleReference =
            PowerDomainElementalHeraldOfTheElementsCold.EffectDescription.EffectParticleParameters
                .conditionEndParticleReference;

        return spell;
    }

    #endregion

    #region Color Burst

    internal static SpellDefinition BuildColorBurst()
    {
        const string NAME = "ColorBurst";

        var spell = SpellDefinitionBuilder
            .Create(ColorSpray, NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.ColorBurst, 128))
            .SetSpellLevel(2)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(ColorSpray)
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Cube, 5)
                    .ExcludeCaster()
                    .SetParticleEffectParameters(HypnoticPattern)
                    .Build())
            .AddToDB();

        spell.EffectDescription.EffectParticleParameters.impactParticleReference =
            spell.EffectDescription.EffectParticleParameters.zoneParticleReference;
        spell.EffectDescription.EffectParticleParameters.zoneParticleReference = new AssetReference();

        return spell;
    }

    #endregion

    #region Mirror Image

    [NotNull]
    internal static SpellDefinition BuildMirrorImage()
    {
        //Use Condition directly, instead of ConditionName to guarantee it gets built
        var condition = ConditionDefinitionBuilder
            .Create("ConditionMirrorImageMark")
            .SetGuiPresentation(MirrorImage.Condition.Name, Category.Condition)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .CopyParticleReferences(ConditionBlurred)
            .AddCustomSubFeatures(MirrorImage.DuplicateProvider.Mark)
            .AddToDB();

        var spell = SpellDefinitions.MirrorImage;

        spell.contentPack = CeContentPackContext.CeContentPack; // required otherwise it messes up spells UI
        spell.implemented = true;
        spell.uniqueInstance = true;
        spell.schoolOfMagic = SchoolIllusion;
        spell.verboseComponent = true;
        spell.somaticComponent = true;
        spell.vocalSpellSemeType = VocalSpellSemeType.Defense;
        spell.materialComponentType = MaterialComponentType.None;
        spell.castingTime = ActivationTime.Action;
        spell.effectDescription = EffectDescriptionBuilder.Create()
            .SetDurationData(DurationType.Minute, 1)
            .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
            .SetEffectForms(
                EffectFormBuilder
                    .Create()
                    .SetConditionForm(condition, ConditionForm.ConditionOperation.Add)
                    .Build())
            .SetParticleEffectParameters(Blur)
            .Build();

        return spell;
    }

    #endregion

    #region Protect Threshold

    [NotNull]
    internal static SpellDefinition BuildProtectThreshold()
    {
        const string NAME = "ProtectThreshold";

        var proxyProtectThreshold = EffectProxyDefinitionBuilder
            .Create(EffectProxyDefinitions.ProxyGuardianOfFaith, $"Proxy{NAME}")
            .SetOrUpdateGuiPresentation(NAME, Category.Spell)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(GuardianOfFaith, NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.ProtectThreshold, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolAbjuration)
            .SetSpellLevel(2)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(false)
            .SetRitualCasting(ActivationTime.Minute10)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(SpikeGrowth.EffectDescription)
                    .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.Sphere, 3)
                    .SetDurationData(DurationType.Minute, 10)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetRecurrentEffect(RecurrentEffect.OnEnter)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Wisdom,
                        false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetSummonEffectProxyForm(proxyProtectThreshold)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypePsychic, 4, DieType.D6)
                            .Build(),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DangerousZone, true),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DifficultThrough, true))
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Web

    internal static SpellDefinition BuildWeb()
    {
        const string NAME = "SpellWeb";

        var conditionRestrainedBySpellWeb = ConditionDefinitionBuilder
            .Create(ConditionGrappledRestrainedRemorhaz, $"ConditionGrappledRestrained{NAME}")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetParentCondition(ConditionRestrainedByWeb)
            .AddToDB();

        conditionRestrainedBySpellWeb.specialDuration = false;
        conditionRestrainedBySpellWeb.specialInterruptions.Clear();

        var conditionAffinityGrappledRestrainedSpellWebImmunity = FeatureDefinitionConditionAffinityBuilder
            .Create($"ConditionAffinityGrappledRestrained{NAME}Immunity")
            .SetGuiPresentationNoContent(true)
            .SetConditionType(conditionRestrainedBySpellWeb)
            .SetConditionAffinityType(ConditionAffinityType.Immunity)
            .AddToDB();

        foreach (var monsterDefinition in DatabaseRepository.GetDatabase<MonsterDefinition>()
                     .Where(x => x.Name.Contains("Spider") || x.Name.Contains("spider")))
        {
            monsterDefinition.Features.Add(conditionAffinityGrappledRestrainedSpellWebImmunity);
        }

        ItemDefinitions.CloakOfArachnida.StaticProperties.Add(ItemPropertyDescriptionBuilder
            .From(conditionAffinityGrappledRestrainedSpellWebImmunity, false,
                EquipmentDefinitions.KnowledgeAffinity.InactiveAndHidden).Build());

        var proxyWeb = EffectProxyDefinitionBuilder
            .Create(EffectProxyDefinitions.ProxyEntangle, $"Proxy{NAME}")
            .SetOrUpdateGuiPresentation(NAME, Category.Spell)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.Web, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(2)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(Grease)
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.Cube, 4, 1)
                    .SetDurationData(DurationType.Hour, 1)
                    .SetRecurrentEffect(RecurrentEffect.OnTurnStart | RecurrentEffect.OnEnter)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Dexterity,
                        false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionRestrainedBySpellWeb, ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetSummonEffectProxyForm(proxyWeb)
                            .Build(),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DangerousZone, false),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DifficultThrough, false))
                    .Build())
            .AddToDB();

        spell.EffectDescription.EffectParticleParameters.conditionParticleReference =
            Entangle.EffectDescription.EffectParticleParameters.conditionParticleReference;

        spell.EffectDescription.EffectParticleParameters.conditionStartParticleReference =
            Entangle.EffectDescription.EffectParticleParameters.conditionStartParticleReference;

        spell.EffectDescription.EffectParticleParameters.conditionEndParticleReference =
            Entangle.EffectDescription.EffectParticleParameters.conditionEndParticleReference;

        return spell;
    }

    #endregion

    #region Noxious Spray

    internal static SpellDefinition BuildNoxiousSpray()
    {
        const string NAME = "NoxiousSpray";

        var actionAffinityNoxiousSpray = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetAllowedActionTypes(false, move: false)
            .AddToDB();

        var conditionNoxiousSpray = ConditionDefinitionBuilder
            .Create(ConditionPheromoned, $"Condition{NAME}")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionDiseased)
            .SetPossessive()
            .SetConditionType(ConditionType.Detrimental)
            .SetFeatures(actionAffinityNoxiousSpray)
            .AddToDB();

        conditionNoxiousSpray.specialDuration = false;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.NoxiousSpray, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(2)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1)
                    .SetTargetingData(Side.Enemy, RangeType.RangeHit, 12, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel,
                        additionalTargetsPerIncrement: 1)
                    .AddImmuneCreatureFamilies(Construct, Elemental, Undead)
                    .SetEffectForms(
                        EffectFormBuilder.DamageForm(DamageTypePoison, 4, DieType.D6),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(conditionNoxiousSpray, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetParticleEffectParameters(PowerDomainOblivionMarkOfFate)
                    .SetCasterEffectParameters(PoisonSpray)
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Cloud of Daggers

    internal static SpellDefinition BuildCloudOfDaggers()
    {
        const string Name = "CloudOfDaggers";

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.CloudOfDaggers, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(2)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.Cube, 2)
                    .SetEffectAdvancement(
                        EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 2)
                    .SetRecurrentEffect(RecurrentEffect.OnTurnStart | RecurrentEffect.OnEnter)
                    .SetEffectForms(
                        EffectFormBuilder.DamageForm(DamageTypeSlashing, 4, DieType.D4),
                        EffectFormBuilder
                            .Create()
                            .SetTopologyForm(TopologyForm.Type.DangerousZone, true)
                            .Build())
                    .SetParticleEffectParameters(BladeBarrierWallLine)
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Wither and Bloom

    internal static SpellDefinition BuildWitherAndBloom()
    {
        const string NAME = "WitherAndBloom";

        var conditionSpellCastingBonus = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetAmountOrigin(ConditionDefinition.OriginOfAmount.Fixed)
            .AddToDB();

        conditionSpellCastingBonus.AddCustomSubFeatures(
            new ModifyDiceRollHitDiceWitherAndBloom(conditionSpellCastingBonus));

        var power = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.None)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 2)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypeNecrotic, 2, DieType.D6)
                            .Build())
                    .SetImpactEffectParameters(Disintegrate)
                    .SetEffectEffectParameters(Disintegrate)
                    .Build())
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.WitherAndBloom, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(2)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetCasterEffectParameters(VampiricTouch)
                    .Build())
            .AddToDB();

        var customBehavior = new CustomBehaviorWitherAndBloom(spell, power, conditionSpellCastingBonus);

        power.AddCustomSubFeatures(customBehavior);
        spell.AddCustomSubFeatures(customBehavior);

        return spell;
    }

    private sealed class ModifyDiceRollHitDiceWitherAndBloom(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionSpellCastingBonus) : IModifyDiceRollHitDice
    {
        public void BeforeRoll(
            RulesetCharacterHero __instance,
            ref DieType die,
            ref int modifier,
            ref AdvantageType advantageType,
            ref bool healKindred,
            ref bool isBonus)
        {
            if (__instance.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionSpellCastingBonus.Name, out var activeCondition))
            {
                modifier += activeCondition.Amount;
            }
        }
    }

    private sealed class CustomBehaviorWitherAndBloom(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        SpellDefinition spellWitherAndBloom,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinitionPower powerWitherAndBloom,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionSpellCastingBonus) : IMagicEffectInitiatedByMe, IMagicEffectFinishedByMe
    {
        private int _effectLevel;

        public IEnumerator OnMagicEffectFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var actionManager =
                ServiceRepository.GetService<IGameLocationActionService>() as GameLocationActionManager;
            var battleManager =
                ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;

            if (!actionManager || !battleManager)
            {
                yield break;
            }

            if (Gui.Battle == null ||
                baseDefinition != spellWitherAndBloom ||
                action.ActionParams.activeEffect is not RulesetEffectSpell rulesetEffectSpell)
            {
                yield break;
            }

            var target = action.ActionParams.TargetCharacters[0];
            var rulesetTarget = target.RulesetCharacter.GetOriginalHero();

            if (rulesetTarget == null)
            {
                yield break;
            }

            _effectLevel = rulesetEffectSpell.EffectLevel;

            var actingCharacter = action.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;
            var effectLevel = _effectLevel;
            var modifier = AttributeDefinitions.ComputeAbilityScoreModifier(
                rulesetCharacter.TryGetAttributeValue(rulesetEffectSpell.SpellRepertoire.SpellCastingAbility));

            rulesetTarget.HitDieRolled += HitDieRolled;

            var activeCondition = rulesetTarget.InflictCondition(
                conditionSpellCastingBonus.Name,
                DurationType.Round,
                0,
                TurnOccurenceType.EndOfSourceTurn,
                AttributeDefinitions.TagEffect,
                rulesetCharacter.guid,
                rulesetCharacter.CurrentFaction.Name,
                1,
                conditionSpellCastingBonus.Name,
                modifier,
                0,
                0);

            while (--effectLevel > 0 &&
                   rulesetTarget.RemainingHitDiceCount() > 0 &&
                   rulesetTarget.MissingHitPoints > 0)
            {
                var maxHitPoints = rulesetTarget.TryGetAttributeValue(AttributeDefinitions.HitPoints);
                var remainingHitPoints = maxHitPoints - rulesetTarget.MissingHitPoints;
                var reactionParams =
                    new CharacterActionParams(target, (ActionDefinitions.Id)ExtraActionId.DoNothingFree)
                    {
                        StringParameter = Gui.Format(
                            "Reaction/&CustomReactionWitherAndBloomDescription",
                            remainingHitPoints.ToString(), maxHitPoints.ToString(), actingCharacter.Name,
                            modifier.ToString())
                    };
                var reactionRequest = new ReactionRequestCustom("WitherAndBloom", reactionParams);
                var count = actionManager.PendingReactionRequestGroups.Count;

                actionManager.AddInterruptRequest(reactionRequest);

                yield return battleManager.WaitForReactions(actingCharacter, actionManager, count);

                if (!reactionParams.ReactionValidated)
                {
                    break;
                }

                EffectHelpers.StartVisualEffect(actingCharacter, target, CureWounds, EffectHelpers.EffectType.Effect);
                rulesetTarget.RollHitDie();
            }

            rulesetTarget.RemoveCondition(activeCondition);
            rulesetTarget.HitDieRolled -= HitDieRolled;

            var attacker = action.ActionParams.ActingCharacter;
            var rulesetAttacker = attacker.RulesetCharacter;

            var implementationManager =
                ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;

            var usablePower = PowerProvider.Get(powerWitherAndBloom, rulesetAttacker);
            var targets = Gui.Battle.GetContenders(target, withinRange: 2);
            var actionParams = new CharacterActionParams(attacker, ActionDefinitions.Id.PowerNoCost)
            {
                ActionModifiers = Enumerable.Repeat(new ActionModifier(), targets.Count).ToList(),
                RulesetEffect = implementationManager
                    .MyInstantiateEffectPower(rulesetAttacker, usablePower, false),
                UsablePower = usablePower,
                targetCharacters = targets
            };

            ServiceRepository.GetService<ICommandService>()?
                .ExecuteAction(actionParams, null, true);
        }

        public IEnumerator OnMagicEffectInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (baseDefinition != powerWitherAndBloom ||
                action.ActionParams.activeEffect is not RulesetEffectPower rulesetEffectPower)
            {
                yield break;
            }

            rulesetEffectPower.EffectDescription.EffectForms[0].DamageForm.diceNumber = _effectLevel;
        }

        private void HitDieRolled(
            RulesetCharacter character,
            DieType dieType,
            int value,
            AdvantageType advantageType,
            int roll1,
            int roll2,
            int modifier,
            bool isBonus)
        {
            // reuse translation string from other feat
            const string BASE_LINE = "Feedback/&DwarvenFortitudeHitDieRolled";

            character.ShowDieRoll(
                dieType, roll1, roll2, advantage: advantageType, title: powerWitherAndBloom.GuiPresentation.Title);

            character.LogCharacterActivatesAbility(
                Gui.NoLocalization, BASE_LINE, true,
                extra:
                [
                    (ConsoleStyleDuplet.ParameterType.AbilityInfo, Gui.FormatDieTitle(dieType)),
                    (ConsoleStyleDuplet.ParameterType.Positive, $"{value - modifier}+{modifier}"),
                    (ConsoleStyleDuplet.ParameterType.Positive, $"{value}")
                ]);
        }
    }

    #endregion

    #region Petal Storm

    internal static readonly EffectProxyDefinition ProxyPetalStorm = EffectProxyDefinitionBuilder
        .Create(EffectProxyDefinitions.ProxyInsectPlague, "ProxyPetalStorm")
        .SetGuiPresentation("PetalStorm", Category.Spell, WindWall)
        .SetPortrait(WindWall.GuiPresentation.SpriteReference)
        .SetActionId(ExtraActionId.ProxyPetalStorm)
        .SetAttackMethod(ProxyAttackMethod.ReproduceDamageForms)
        .SetAdditionalFeatures(FeatureDefinitionMoveModes.MoveModeMove6)
        .SetCanMove()
        .AddToDB();

    internal static SpellDefinition BuildPetalStorm()
    {
        const string NAME = "PetalStorm";

        var spell = SpellDefinitionBuilder
            .Create(InsectPlague, NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.PetalStorm, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(2)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(InsectPlague.EffectDescription)
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.Cube, 3)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 2)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnEnter | RecurrentEffect.OnTurnStart)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Strength,
                        false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetDamageForm(DamageTypeSlashing, 3, DieType.D4)
                            .Build(),
                        EffectFormBuilder.ConditionForm(ConditionHeavilyObscured),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.SightImpaired, true),
                        EffectFormBuilder
                            .Create()
                            .SetSummonEffectProxyForm(ProxyPetalStorm)
                            .Build())
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Shadowblade

    [NotNull]
    internal static SpellDefinition BuildShadowBlade()
    {
        const string NAME = "ShadowBlade";

        var itemShadowBlade = ItemDefinitionBuilder
            .Create(ItemDefinitions.FlameBlade, $"Item{NAME}")
            .SetOrUpdateGuiPresentation(Category.Item, ItemDefinitions.Enchanted_Dagger_Souldrinker)
            .SetItemTags(TagsDefinitions.ItemTagConjured)
            .MakeMagical()
            .AddToDB();

        itemShadowBlade.activeTags.Clear();
        itemShadowBlade.isLightSourceItem = false;
        itemShadowBlade.itemPresentation.assetReference = ItemDefinitions.ScimitarPlus2.ItemPresentation.AssetReference;
        itemShadowBlade.weaponDefinition.EffectDescription.EffectParticleParameters.impactParticleReference =
            EffectProxyDefinitions.ProxyArcaneSword.attackImpactParticle;

        var weaponDescription = itemShadowBlade.WeaponDescription;

        weaponDescription.closeRange = 4;
        weaponDescription.maxRange = 12;
        weaponDescription.weaponType = WeaponTypeDefinitions.DaggerType.Name;
        weaponDescription.weaponTags.Add(TagsDefinitions.WeaponTagThrown);

        var damageForm = weaponDescription.EffectDescription.FindFirstDamageForm();

        damageForm.damageType = DamageTypePsychic;
        damageForm.dieType = DieType.D8;
        damageForm.diceNumber = 2;

        var spell = SpellDefinitionBuilder
            .Create(FlameBlade, NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.ShadeBlade, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolIllusion)
            .SetSpellLevel(2)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(FlameBlade)
                    .SetDurationData(DurationType.Minute, 1)
                    .Build())
            .AddToDB();

        var summonForm = spell.EffectDescription.EffectForms[0].SummonForm;

        summonForm.itemDefinition = itemShadowBlade;

        var itemPropertyForm = spell.EffectDescription.EffectForms[1].ItemPropertyForm;

        itemPropertyForm.featureBySlotLevel.Clear();
        itemPropertyForm.featureBySlotLevel.Add(BuildShadowBladeFeatureBySlotLevel(2, 0));
        itemPropertyForm.featureBySlotLevel.Add(BuildShadowBladeFeatureBySlotLevel(3, 1));
        itemPropertyForm.featureBySlotLevel.Add(BuildShadowBladeFeatureBySlotLevel(5, 2));
        itemPropertyForm.featureBySlotLevel.Add(BuildShadowBladeFeatureBySlotLevel(7, 3));

        var conditionShadowBlade = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddToDB();

        conditionShadowBlade.AddCustomSubFeatures(
            new ModifyAttackActionModifierShadowBlade(itemShadowBlade, conditionShadowBlade));

        spell.EffectDescription.EffectForms.Add(
            EffectFormBuilder
                .Create()
                .SetConditionForm(conditionShadowBlade, ConditionForm.ConditionOperation.Add, true)
                .Build());

        return spell;
    }

    private static FeatureUnlockByLevel BuildShadowBladeFeatureBySlotLevel(int level, int damageDice)
    {
        var attackModifierShadowBladeLevel = FeatureDefinitionAttackModifierBuilder
            .Create(FeatureDefinitionAttackModifiers.AttackModifierFlameBlade2, $"AttackModifierShadowBlade{level}")
            .AddToDB();

        attackModifierShadowBladeLevel.guiPresentation.description
            = damageDice > 0
                ? Gui.Format("Feature/&AttackModifierShadowBladeNDescription", damageDice.ToString())
                : "Feature/&AttackModifierShadowBlade0Description";
        attackModifierShadowBladeLevel.additionalDamageDice = damageDice;
        attackModifierShadowBladeLevel.impactParticleReference =
            ShadowDagger.EffectDescription.EffectParticleParameters.impactParticleReference;
        attackModifierShadowBladeLevel.abilityScoreReplacement = AbilityScoreReplacement.None;
        return new FeatureUnlockByLevel(attackModifierShadowBladeLevel, level);
    }

    private sealed class ModifyAttackActionModifierShadowBlade(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ItemDefinition itemShadowBlade,
        BaseDefinition featureAdvantage)
        : IModifyAttackActionModifier
    {
        public void OnAttackComputeModifier(
            RulesetCharacter myself,
            RulesetCharacter defender,
            BattleDefinitions.AttackProximity attackProximity,
            RulesetAttackMode attackMode,
            string effectName,
            ref ActionModifier attackModifier)
        {
            if (myself is not { IsDeadOrDyingOrUnconscious: false } ||
                defender is not { IsDeadOrDyingOrUnconscious: false })
            {
                return;
            }

            if (attackMode?.SourceDefinition != itemShadowBlade)
            {
                return;
            }

            if (!ValidatorsCharacter.IsNotInBrightLight(defender))
            {
                return;
            }

            attackModifier.attackAdvantageTrends.Add(
                new TrendInfo(1, FeatureSourceType.Condition, featureAdvantage.Name, featureAdvantage));
        }
    }

    #endregion

    #region Psychic Whip

    internal static SpellDefinition BuildPsychicWhip()
    {
        const string NAME = "PsychicWhip";

        var actionAffinityPsychicWhipNoBonus = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{NAME}NoBonus")
            .SetGuiPresentationNoContent(true)
            .SetAllowedActionTypes(bonus: false)
            .AddToDB();

        var conditionPsychicWhipNoBonus = ConditionDefinitionBuilder
            .Create($"Condition{NAME}NoBonus")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(actionAffinityPsychicWhipNoBonus)
            .AddToDB();

        var actionAffinityPsychicWhipNoMove = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{NAME}NoMove")
            .SetGuiPresentationNoContent(true)
            .SetAllowedActionTypes(move: false)
            .AddToDB();

        var conditionPsychicWhipNoMove = ConditionDefinitionBuilder
            .Create($"Condition{NAME}NoMove")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(actionAffinityPsychicWhipNoMove)
            .AddToDB();

        var actionAffinityPsychicWhipNoMain = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{NAME}NoMain")
            .SetGuiPresentationNoContent(true)
            .SetAllowedActionTypes(false)
            .AddToDB();

        var conditionPsychicWhipNoMain = ConditionDefinitionBuilder
            .Create($"Condition{NAME}NoMain")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(actionAffinityPsychicWhipNoMain)
            .AddToDB();

        var actionAffinityPsychicWhipNoReaction = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{NAME}NoReaction")
            .SetGuiPresentationNoContent(true)
            .SetAllowedActionTypes(reaction: false)
            .AddToDB();

        var conditionPsychicWhipNoReaction = ConditionDefinitionBuilder
            .Create(ConditionConfused, $"Condition{NAME}NoReaction")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetPossessive()
            .SetConditionType(ConditionType.Detrimental)
            .SetFeatures(actionAffinityPsychicWhipNoReaction)
            .AddToDB();

        conditionPsychicWhipNoReaction.AddCustomSubFeatures(new ActionFinishedByMePsychicWhip(
            conditionPsychicWhipNoBonus,
            conditionPsychicWhipNoMain,
            conditionPsychicWhipNoMove,
            conditionPsychicWhipNoReaction));

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.PsychicWhip, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEnchantment)
            .SetSpellLevel(2)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Defense)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1)
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 18, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel,
                        additionalTargetsPerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Intelligence, true,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypePsychic, 3, DieType.D6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionPsychicWhipNoReaction, ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetParticleEffectParameters(GravitySlam)
                    .Build())
            .AddToDB();

        return spell;
    }

    private sealed class ActionFinishedByMePsychicWhip(
        ConditionDefinition conditionNoBonus,
        ConditionDefinition conditionNoMain,
        ConditionDefinition conditionNoMove,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionNoReaction)
        : IActionFinishedByMe
    {
        public IEnumerator OnActionFinishedByMe(CharacterAction characterAction)
        {
            var actionType = characterAction.ActionType;
            var conditions = new List<ConditionDefinition>();

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (actionType)
            {
                case ActionDefinitions.ActionType.Main:
                    conditions.Add(conditionNoMove);
                    conditions.Add(conditionNoBonus);
                    break;
                case ActionDefinitions.ActionType.Bonus:
                    conditions.Add(conditionNoMain);
                    conditions.Add(conditionNoMove);
                    break;
                case ActionDefinitions.ActionType.Move:
                    conditions.Add(conditionNoBonus);
                    conditions.Add(conditionNoMain);
                    break;
            }

            if (characterAction.ActingCharacter.RulesetCharacter is not
                { IsDeadOrDyingOrUnconscious: false } rulesetCharacter)
            {
                yield break;
            }

            if (!rulesetCharacter.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect,
                    conditionNoReaction.Name,
                    out var activeCondition))
            {
                yield break;
            }

            var caster = EffectHelpers.GetCharacterByGuid(activeCondition.SourceGuid);

            if (caster is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            // game freezes when enemy tries to Dash so best we can do here is allow this exception on the spell
            if (characterAction is CharacterActionDash)
            {
                conditions.Remove(conditionNoMove);
            }

            if (characterAction.ActingCharacter.RulesetCharacter is
                { IsDeadOrDyingOrUnconscious: false })
            {
                conditions.ForEach(condition =>
                    rulesetCharacter.InflictCondition(
                        condition.Name,
                        DurationType.Round,
                        0,
                        TurnOccurenceType.EndOfSourceTurn,
                        AttributeDefinitions.TagEffect,
                        caster.guid,
                        caster.CurrentFaction.Name,
                        1,
                        condition.Name,
                        0,
                        0,
                        0));
            }
        }
    }

    #endregion
}
