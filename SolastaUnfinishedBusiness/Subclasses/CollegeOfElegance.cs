﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static AttributeDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionActionAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionSubclassChoices;
using static SolastaUnfinishedBusiness.Subclasses.CommonBuilders;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class CollegeOfElegance : AbstractSubclass
{
    private const string Name = "CollegeOfElegance";
    private const ActionDefinitions.Id AmazingDisplayToggle = (ActionDefinitions.Id)ExtraActionId.AmazingDisplayToggle;

    public CollegeOfElegance()
    {
        // LEVEL 03

        // Grace

        var dieRollModifierGrace = FeatureDefinitionDieRollModifierBuilder
            .Create($"DieRollModifier{Name}Grace")
            .SetGuiPresentation(Category.Feature)
            .SetModifiers(
                RollContext.AbilityCheck,
                0,
                10,
                0,
                "Feedback/&DieRollModifierCollegeOfEleganceGraceReroll",
                // Dexterity Checks
                SkillDefinitions.Acrobatics,
                // Charisma Checks
                SkillDefinitions.Performance)
            .AddToDB();

        // Elegant Fighting

        const string ElegantFightingName = $"FeatureSet{Name}ElegantFighting";

        var attributeModifierElegantStepsArmorClass = FeatureDefinitionAttributeModifierBuilder
            .Create($"AttributeModifier{Name}ElegantFightingArmorClass")
            .SetGuiPresentation(ElegantFightingName, Category.Feature, Gui.NoLocalization)
            .SetDexPlusAbilityScore(ArmorClass, Charisma)
            .SetSituationalContext(SituationalContext.NotWearingArmorOrShield)
            .AddToDB();

        var validator = new ValidatorsValidatePowerUse(ValidatorsCharacter.HasNoArmor, ValidatorsCharacter.HasNoShield);

        var powerDash = FeatureDefinitionPowerBuilder
            .Create(PowerMonkStepOfTheWindDash, $"Power{Name}Dash")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerEleganceDash", Resources.PowerEleganceDash, 256, 128))
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.BardicInspiration)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(PowerMonkStepOfTheWindDash)
                    .SetCasterEffectParameters(PowerBardHopeSingSongOfHope)
                    .Build())
            .AddCustomSubFeatures(validator)
            .AddToDB();

        var powerDisengage = FeatureDefinitionPowerBuilder
            .Create(PowerMonkStepOftheWindDisengage, $"Power{Name}Disengage")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerEleganceDisengage", Resources.PowerEleganceDisengage, 256, 128))
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.BardicInspiration)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(PowerMonkStepOftheWindDisengage)
                    .SetCasterEffectParameters(PowerBardHopeSingSongOfHope)
                    .Build())
            .AddCustomSubFeatures(validator)
            .AddToDB();

        var powerDodge = FeatureDefinitionPowerBuilder
            .Create(PowerMonkPatientDefense, $"Power{Name}Dodge")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerEleganceDodge", Resources.PowerEleganceDodge, 256, 128))
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.BardicInspiration)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(PowerMonkPatientDefense)
                    .SetCasterEffectParameters(PowerBardHeroismAtRoadsEnd)
                    .Build())
            .AddCustomSubFeatures(validator)
            .AddToDB();

        var featureSetElegantFighting = FeatureDefinitionFeatureSetBuilder
            .Create(ElegantFightingName)
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(attributeModifierElegantStepsArmorClass, powerDash, powerDisengage, powerDodge)
            .AddToDB();

        // LEVEL 06

        // Evasive Footwork

        var conditionEvasiveFootwork = ConditionDefinitionBuilder
            .Create($"Condition{Name}EvasiveFootwork")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(
                FeatureDefinitionAttributeModifierBuilder
                    .Create($"AttributeModifier{Name}EvasiveFootwork")
                    .SetGuiPresentationNoContent(true)
                    .SetAddConditionAmount(ArmorClass)
                    .AddToDB())
            .SetAmountOrigin(ConditionDefinition.OriginOfAmount.Fixed)
            .SetSpecialInterruptions(ExtraConditionInterruption.AfterWasAttacked)
            .AddToDB();

        var featureEvasiveFootwork = FeatureDefinitionBuilder
            .Create($"Feature{Name}EvasiveFootwork")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureEvasiveFootwork.AddCustomSubFeatures(
            new AttackBeforeHitPossibleOnMeOrAllyEvasiveFootwork(featureEvasiveFootwork, conditionEvasiveFootwork));

        // Extra Attack

        // LEVEL 14

        // Amazing Display

        const string AmazingDisplayName = $"FeatureSet{Name}AmazingDisplay";

        var conditionAmazingDisplay = ConditionDefinitionBuilder
            .Create($"Condition{Name}AmazingDisplay")
            .SetGuiPresentation(Category.Condition, Gui.NoLocalization, ConditionDefinitions.ConditionDazzled)
            .SetConditionType(ConditionType.Detrimental)
            .SetFeatures(
                FeatureDefinitionActionAffinityBuilder
                    .Create($"ActionAffinity{Name}AmazingDisplay")
                    .SetGuiPresentation(AmazingDisplayName, Category.Feature, Gui.NoLocalization)
                    .SetAllowedActionTypes(reaction: false)
                    .AddToDB(),
                FeatureDefinitionMovementAffinityBuilder
                    .Create($"MovementAffinity{Name}AmazingDisplay")
                    .SetGuiPresentation(AmazingDisplayName, Category.Feature, Gui.NoLocalization)
                    .SetBaseSpeedMultiplicativeModifier(0)
                    .AddToDB())
            .SetConditionParticleReference(ConditionDefinitions.ConditionDistracted.conditionParticleReference)
            .AddToDB();

        conditionAmazingDisplay.GuiPresentation.description = Gui.NoLocalization;

        var conditionAmazingDisplayMarker = ConditionDefinitionBuilder
            .Create($"Condition{Name}AmazingDisplayMarker")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetSpecialDuration(DurationType.UntilAnyRest)
            .AddToDB();

        var powerAmazingDisplayEnemy = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}AmazingDisplayEnemy")
            .SetGuiPresentation(AmazingDisplayName, Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, Wisdom, true, EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionAmazingDisplayMarker),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(conditionAmazingDisplay, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetCasterEffectParameters(PowerOathOfDevotionTurnUnholy)
                    .Build())
            .AddCustomSubFeatures(ModifyPowerVisibility.Hidden)
            .AddToDB();

        var powerAmazingDisplay = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}AmazingDisplay")
            .SetGuiPresentation(AmazingDisplayName, Category.Feature)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .DelegatedToAction()
            .AddToDB();

        powerAmazingDisplay.AddCustomSubFeatures(
            new PhysicalAttackFinishedByMeAmazingDisplay(
                conditionAmazingDisplayMarker, powerAmazingDisplay, powerAmazingDisplayEnemy));

        _ = ActionDefinitionBuilder
            .Create(DatabaseHelper.ActionDefinitions.MetamagicToggle, "AmazingDisplayToggle")
            .SetOrUpdateGuiPresentation(Category.Action)
            .RequiresAuthorization()
            .SetActionId(ExtraActionId.AmazingDisplayToggle)
            .SetActivatedPower(powerAmazingDisplay)
            .AddToDB();

        var actionAffinityAmazingDisplayToggle = FeatureDefinitionActionAffinityBuilder
            .Create(ActionAffinitySorcererMetamagicToggle, "ActionAffinityAmazingDisplayToggle")
            .SetGuiPresentationNoContent(true)
            .SetAuthorizedActions(AmazingDisplayToggle)
            .AddCustomSubFeatures(
                new ValidateDefinitionApplication(ValidatorsCharacter.HasAvailablePowerUsage(powerAmazingDisplay)))
            .AddToDB();

        var featureSetAmazingDisplay = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}AmazingDisplay")
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(powerAmazingDisplayEnemy, powerAmazingDisplay, actionAffinityAmazingDisplayToggle)
            .AddToDB();

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite(Name, Resources.CollegeOfWarDancer, 256))
            .AddFeaturesAtLevel(3, dieRollModifierGrace, featureSetElegantFighting)
            .AddFeaturesAtLevel(6, featureEvasiveFootwork, AttributeModifierCasterFightingExtraAttack)
            .AddFeaturesAtLevel(14, featureSetAmazingDisplay)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Bard;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice => SubclassChoiceBardColleges;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    private class AttackBeforeHitPossibleOnMeOrAllyEvasiveFootwork(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinition featureEvasiveFootwork,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionEvasiveFootwork) : IAttackBeforeHitPossibleOnMeOrAlly
    {
        public IEnumerator OnAttackBeforeHitPossibleOnMeOrAlly(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect,
            int attackRoll)
        {
            var actionManager =
                ServiceRepository.GetService<IGameLocationActionService>() as GameLocationActionManager;

            if (!actionManager)
            {
                yield break;
            }

            if (rulesetEffect != null &&
                rulesetEffect.EffectDescription.RangeType is not (RangeType.MeleeHit or RangeType.RangeHit))
            {
                yield break;
            }

            var rulesetHelper = helper.RulesetCharacter;

            if (helper != defender ||
                !defender.CanReact() ||
                !ValidatorsCharacter.HasNoArmor(rulesetHelper) ||
                !ValidatorsCharacter.HasNoShield(rulesetHelper))
            {
                yield break;
            }

            var armorClass = defender.RulesetCharacter.TryGetAttributeValue(ArmorClass);
            var totalAttack =
                attackRoll +
                (attackMode?.ToHitBonus ?? rulesetEffect?.MagicAttackBonus ?? 0) +
                actionModifier.AttackRollModifier;

            if (armorClass > totalAttack)
            {
                yield break;
            }

            var dieType = rulesetHelper.GetBardicInspirationDieValue();
            var maxDie = DiceMaxValue[(int)dieType];

            if (armorClass + maxDie <= totalAttack)
            {
                yield break;
            }

            var actionParams = new CharacterActionParams(helper, (ActionDefinitions.Id)ExtraActionId.DoNothingReaction)
            {
                StringParameter = "Reaction/&CustomReactionEvasiveFootworkDescription"
            };
            var reactionRequest = new ReactionRequestCustom("EvasiveFootwork", actionParams);
            var count = actionManager.PendingReactionRequestGroups.Count;

            actionManager.AddInterruptRequest(reactionRequest);

            yield return battleManager.WaitForReactions(attacker, actionManager, count);

            if (!actionParams.ReactionValidated)
            {
                yield break;
            }

            var dieRoll = RollDie(dieType, AdvantageType.None, out _, out _);

            rulesetHelper.InflictCondition(
                conditionEvasiveFootwork.Name,
                DurationType.Round,
                0,
                TurnOccurenceType.StartOfTurn,
                TagEffect,
                rulesetHelper.guid,
                rulesetHelper.CurrentFaction.Name,
                1,
                conditionEvasiveFootwork.Name,
                dieRoll,
                0,
                0);

            EffectHelpers.StartVisualEffect(defender, defender, PowerKnightLeadership, EffectHelpers.EffectType.Effect);
            rulesetHelper.LogCharacterUsedFeature(
                featureEvasiveFootwork,
                "Feedback/&EvasiveFootworkACIncrease", true,
                (ConsoleStyleDuplet.ParameterType.AbilityInfo, Gui.FormatDieTitle(dieType)),
                (ConsoleStyleDuplet.ParameterType.Positive, dieRoll.ToString()));
        }
    }

    private class PhysicalAttackFinishedByMeAmazingDisplay(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionAmazingDisplayMarker,
        FeatureDefinitionPower powerAmazingDisplay,
        FeatureDefinitionPower powerAmazingDisplayEnemy) : IPhysicalAttackFinishedByMe
    {
        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (action.AttackRollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (!rulesetAttacker.IsToggleEnabled(AmazingDisplayToggle) ||
                !attacker.OnceInMyTurnIsValid(powerAmazingDisplay.Name))
            {
                yield break;
            }

            if (Gui.Battle == null)
            {
                yield break;
            }

            var targets = Gui.Battle.GetContenders(attacker, hasToPerceivePerceiver: true, withinRange: 6);

            // remove enemies previously target by amazing display
            targets.RemoveAll(x =>
                x.RulesetCharacter.HasConditionOfCategoryAndType(TagEffect, conditionAmazingDisplayMarker.Name));

            // remove enemies immune to charmed
            targets.RemoveAll(x =>
                x.RulesetCharacter.GetFeaturesByType<IConditionAffinityProvider>().Any(y =>
                    y.ConditionAffinityType == ConditionAffinityType.Immunity && y.ConditionType == ConditionCharmed));

            if (targets.Count == 0)
            {
                rulesetAttacker.LogCharacterActivatesAbility(Gui.NoLocalization, "Feedback/&AmazingDisplayNotTriggered",
                    extra:
                    [
                        (ConsoleStyleDuplet.ParameterType.Player, rulesetAttacker.Name)
                    ]);

                yield break;
            }

            attacker.UsedSpecialFeatures.TryAdd(powerAmazingDisplay.Name, 0);

            var usablePower = PowerProvider.Get(powerAmazingDisplay, rulesetAttacker);

            usablePower.Consume();

            var implementationManager =
                ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;

            var usablePowerEnemy = PowerProvider.Get(powerAmazingDisplayEnemy, rulesetAttacker);
            var actionParams =
                new CharacterActionParams(attacker, ActionDefinitions.Id.PowerNoCost)
                {
                    ActionModifiers = Enumerable.Repeat(new ActionModifier(), targets.Count).ToList(),
                    RulesetEffect = implementationManager
                        .MyInstantiateEffectPower(rulesetAttacker, usablePowerEnemy, false),
                    UsablePower = usablePowerEnemy,
                    targetCharacters = targets
                };

            // must enqueue actions whenever within an attack workflow otherwise game won't consume attack
            ServiceRepository.GetService<ICommandService>()?
                .ExecuteAction(actionParams, null, true);
        }
    }
}
