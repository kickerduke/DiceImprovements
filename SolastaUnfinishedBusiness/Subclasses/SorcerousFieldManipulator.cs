﻿using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using TA;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class SorcerousFieldManipulator : AbstractSubclass
{
    private const string Name = "SorcerousFieldManipulator";

    public SorcerousFieldManipulator()
    {
        // LEVEL 01

        // Auto Prepared Spells

        var autoPreparedSpellsFieldManipulator = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation("ExpandedSpells", Category.Feature)
            .SetAutoTag("Origin")
            .SetSpellcastingClass(CharacterClassDefinitions.Sorcerer)
            .AddPreparedSpellGroup(1, Sleep)
            .AddPreparedSpellGroup(3, HoldPerson)
            .AddPreparedSpellGroup(5, Counterspell)
            .AddPreparedSpellGroup(7, Banishment)
            .AddPreparedSpellGroup(9, HoldMonster)
            .AddPreparedSpellGroup(11, GlobeOfInvulnerability)
            .AddToDB();

        // Displacement

        PowerSorcerousFieldManipulatorDisplacement = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Displacement")
            .SetGuiPresentation(Category.Feature, MistyStep)
            .SetUsesProficiencyBonus(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.Position)
                    .InviteOptionalAlly()
                    .SetParticleEffectParameters(Banishment)
                    .SetSavingThrowData(
                        true,
                        AttributeDefinitions.Charisma,
                        true,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.TeleportToDestination)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .Build())
            .AddCustomSubFeatures(new CustomBehaviorDisplacement(), ForcePushOrDragFromEffectPoint.Marker)
            .AddToDB();

        // LEVEL 06

        // Arcane Manipulation

        MagicAffinityHeightened = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}ArcaneManipulation")
            .SetGuiPresentation(Category.Feature)
            .SetWarList(1)
            .AddToDB();

        // LEVEL 14

        // Mental Resistance

        var proficiencyMentalResistance = FeatureDefinitionProficiencyBuilder
            .Create($"Proficiency{Name}MentalResistance")
            .SetGuiPresentation(Category.Feature)
            .SetProficiencies(
                ProficiencyType.SavingThrow,
                AttributeDefinitions.Intelligence,
                AttributeDefinitions.Wisdom)
            .AddToDB();

        // LEVEL 18

        // Forceful Step

        var powerForcefulStepApply = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}ForcefulStepApply")
            .SetGuiPresentation($"Power{Name}ForcefulStep", Category.Feature, hidden: true)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1)
                    .SetTargetingData(Side.Enemy, RangeType.Touch, 0, TargetType.IndividualsUnique)
                    .SetParticleEffectParameters(EldritchBlast)
                    .SetSavingThrowData(
                        true,
                        AttributeDefinitions.Strength,
                        true,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, 3, DieType.D10)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.PushFromOrigin, 2)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.FallProne)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .Build())
            .AddToDB();

        var powerForcefulStepFixed = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}ForcefulStepFixed")
            .SetGuiPresentation($"Power{Name}ForcefulStep", Category.Feature, PowerMonkStepOfTheWindDash)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.LongRest, 1, 3)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 12, TargetType.Position)
                    .SetParticleEffectParameters(PowerMelekTeleport)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.TeleportToDestination)
                            .Build())
                    .Build())
            .AddToDB();

        powerForcefulStepFixed.AddCustomSubFeatures(
            new ValidatorsValidatePowerUse(c => c.GetRemainingPowerUses(powerForcefulStepFixed) > 0),
            new MagicEffectFinishedByMeForcefulStep(powerForcefulStepApply));

        var powerForcefulStepPoints = FeatureDefinitionPowerBuilder
            .Create(powerForcefulStepFixed, $"Power{Name}ForcefulStepPoints")
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.SorceryPoints, 4, 0)
            .AddToDB();

        powerForcefulStepPoints.AddCustomSubFeatures(
            new ValidatorsValidatePowerUse(c => c.GetRemainingPowerUses(powerForcefulStepFixed) == 0),
            new MagicEffectFinishedByMeForcefulStep(powerForcefulStepApply));

        var featureSetForcefulStep = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ForcefulStep")
            .SetGuiPresentation($"Power{Name}ForcefulStep", Category.Feature)
            .AddFeatureSet(powerForcefulStepFixed, powerForcefulStepPoints, powerForcefulStepApply)
            .AddToDB();

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite(Name, Resources.SorcererFieldManipulator, 256))
            .AddFeaturesAtLevel(1,
                autoPreparedSpellsFieldManipulator,
                PowerSorcerousFieldManipulatorDisplacement)
            .AddFeaturesAtLevel(6,
                MagicAffinityHeightened)
            .AddFeaturesAtLevel(14,
                proficiencyMentalResistance)
            .AddFeaturesAtLevel(18,
                featureSetForcefulStep)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Sorcerer;

    internal static FeatureDefinitionPower PowerSorcerousFieldManipulatorDisplacement { get; private set; }

    private static FeatureDefinitionMagicAffinity MagicAffinityHeightened { get; set; }

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceSorcerousOrigin;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    internal static void LateLoad()
    {
        foreach (var spellDefinition in SpellListDefinitions.SpellListAllSpells
                     .SpellsByLevel
                     .SelectMany(x => x.Spells)
                     // don't use the constant as it has a typo
                     .Where(x => x.SchoolOfMagic is "SchoolEnchantment" or SchoolAbjuration or SchoolIllusion))
        {
            if (spellDefinition.SpellsBundle)
            {
                foreach (var spellInBundle in spellDefinition.SubspellsList)
                {
                    MagicAffinityHeightened.WarListSpells.Add(spellInBundle.Name);
                }
            }
            else
            {
                MagicAffinityHeightened.WarListSpells.Add(spellDefinition.Name);
            }
        }
    }

    //
    // Displacement
    //

    private sealed class CustomBehaviorDisplacement : IMagicEffectInitiatedByMe, IMagicEffectFinishedByMe
    {
        public IEnumerator OnMagicEffectFinishedByMe(CharacterActionMagicEffect action, BaseDefinition power)
        {
            var rulesetEffect = action.ActionParams.RulesetEffect;

            // bring back power target type to position
            rulesetEffect.EffectDescription.targetType = TargetType.Position;

            yield break;
        }

        public IEnumerator OnMagicEffectInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition power)
        {
            var rulesetEffect = action.ActionParams.RulesetEffect;
            var actionParams = action.ActionParams;

            actionParams.Positions.SetRange(
                GetFinalPosition(actionParams.TargetCharacters[0], actionParams.Positions[0]));

            // make target type individuals unique to trigger the game and only teleport targets
            rulesetEffect.EffectDescription.targetType = TargetType.IndividualsUnique;

            yield break;
        }

        private static int3 GetFinalPosition(GameLocationCharacter target, int3 position)
        {
            const string ERROR = "DISPLACEMENT: aborted as cannot place character on destination";

            var positioningManager =
                ServiceRepository.GetService<IGameLocationPositioningService>() as GameLocationPositioningManager;

            //fall back to target original position
            if (!positioningManager)
            {
                return target.LocationPosition;
            }

            var xCoordinates = new[] { 0, -1, 1, -2, 2 };
            var yCoordinates = new[] { 0, -1, 1, -2, 2 };
            var canPlaceCharacter = false;
            var finalPosition = int3.zero;

            foreach (var x in xCoordinates)
            {
                foreach (var y in yCoordinates)
                {
                    finalPosition = position + new int3(x, 0, y);

                    canPlaceCharacter = positioningManager.CanPlaceCharacterImpl(
                        target, target.RulesetCharacter.SizeParams, finalPosition, CellHelpers.PlacementMode.Station);

                    if (canPlaceCharacter)
                    {
                        break;
                    }
                }

                if (canPlaceCharacter)
                {
                    break;
                }
            }

            if (canPlaceCharacter)
            {
                return finalPosition;
            }

            //fall back to target original position
            finalPosition = target.LocationPosition;

            Gui.GuiService.ShowAlert(ERROR, Gui.ColorFailure);

            return finalPosition;
        }
    }

    //
    // Forceful Step
    //

    private sealed class MagicEffectFinishedByMeForcefulStep(FeatureDefinitionPower powerApply)
        : IMagicEffectFinishedByMe
    {
        public IEnumerator OnMagicEffectFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (Gui.Battle == null)
            {
                yield break;
            }

            var attacker = action.ActingCharacter;
            var rulesetAttacker = attacker.RulesetCharacter;

            var implementationManager =
                ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;

            var usablePower = PowerProvider.Get(powerApply, rulesetAttacker);
            var targets = Gui.Battle.GetContenders(attacker, withinRange: 2);
            var actionParams = new CharacterActionParams(attacker, ActionDefinitions.Id.PowerNoCost)
            {
                ActionModifiers = Enumerable.Repeat(new ActionModifier(), targets.Count).ToList(),
                RulesetEffect = implementationManager
                    .MyInstantiateEffectPower(rulesetAttacker, usablePower, false),
                UsablePower = usablePower,
                targetCharacters = targets
            };

            ServiceRepository.GetService<IGameLocationActionService>()?
                .ExecuteAction(actionParams, null, true);
        }
    }
}
