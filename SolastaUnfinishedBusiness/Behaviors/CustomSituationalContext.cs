﻿using System;
using System.Linq;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Subclasses;
using SolastaUnfinishedBusiness.Validators;
using TA;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.WeaponTypeDefinitions;

namespace SolastaUnfinishedBusiness.Behaviors;

internal static class CustomSituationalContext
{
    internal static readonly WeaponTypeDefinition[] SimpleOrMartialWeapons = DatabaseRepository
        .GetDatabase<WeaponTypeDefinition>()
        .Where(x => x.WeaponCategory is "MartialWeaponCategory" or "SimpleWeaponCategory")
        .ToArray();

    internal static bool IsContextValid(
        RulesetImplementationDefinitions.SituationalContextParams contextParams,
        bool def)
    {
        return (ExtraSituationalContext)contextParams.situationalContext switch
        {
            ExtraSituationalContext.IsRagingAndDualWielding =>
                contextParams.source.HasConditionOfTypeOrSubType(ConditionRaging) &&
                ValidatorsCharacter.HasMeleeWeaponInMainAndOffhand(contextParams.source),

            ExtraSituationalContext.IsNotInBrightLight =>
                ValidatorsCharacter.IsNotInBrightLight(contextParams.source),

            ExtraSituationalContext.HasLongswordInHands =>
                ValidatorsCharacter.HasWeaponType(LongswordType)(contextParams.source),

            ExtraSituationalContext.HasGreatswordInHands =>
                ValidatorsCharacter.HasWeaponType(GreatswordType)(contextParams.source),

            ExtraSituationalContext.HasBladeMasteryWeaponTypesInHands =>
                ValidatorsCharacter.HasWeaponType(
                    ShortswordType, LongswordType, ScimitarType, RapierType, GreatswordType)(contextParams.source),

            ExtraSituationalContext.HasSimpleOrMartialWeaponInHands =>
                ValidatorsCharacter.HasWeaponType(SimpleOrMartialWeapons)(contextParams.source),

            ExtraSituationalContext.HasMeleeWeaponInMainHandWithFreeOffhand =>
                ValidatorsCharacter.HasFreeHandWithoutTwoHandedInMain(contextParams.source) &&
                ValidatorsCharacter.HasMeleeWeaponInMainHand(contextParams.source),

            ExtraSituationalContext.WearingNoArmorOrLightArmorWithoutShield =>
                (ValidatorsCharacter.HasNoArmor(contextParams.source) ||
                 ValidatorsCharacter.HasLightArmor(contextParams.source)) &&
                ValidatorsCharacter.HasNoShield(contextParams.source),

            ExtraSituationalContext.WearingNoArmorOrLightArmorWithTwoHandedQuarterstaff =>
                (ValidatorsCharacter.HasNoArmor(contextParams.source) ||
                 ValidatorsCharacter.HasLightArmor(contextParams.source)) &&
                ValidatorsCharacter.HasTwoHandedQuarterstaff(contextParams.source),

            ExtraSituationalContext.IsNotConditionSource =>
                // this is required whenever there is a SetMyAttackAdvantage (Taunted, Illuminating Strike, Honed Bear)
                contextParams.target.Guid != contextParams.source.AllConditions.FirstOrDefault(x =>
                    x.ConditionDefinition == contextParams.condition)?.SourceGuid &&
                // this is required whenever there is a SetAttackOnMeAdvantage (Press the Advantage, Gambit Blind)
                contextParams.source.Guid != contextParams.target.AllConditions.FirstOrDefault(x =>
                    x.ConditionDefinition == contextParams.condition)?.SourceGuid,

            ExtraSituationalContext.IsNotConditionSourceNotRanged =>
                // this is required whenever there is a SetMyAttackAdvantage (Wolf Leadership)
                contextParams.source.Guid != contextParams.source.AllConditions.FirstOrDefault(x =>
                    x.ConditionDefinition == contextParams.condition)?.SourceGuid &&
                !contextParams.rangedAttack,

            ExtraSituationalContext.TargetIsFavoriteEnemy =>
                contextParams.source.IsMyFavoriteEnemy(contextParams.target),

            ExtraSituationalContext.NextToWallWithShieldAndMaxMediumArmorAndConsciousAllyNextToTarget =>
                NextToWallWithShieldAndMaxMediumArmorAndConsciousAllyNextToTarget(contextParams),

            ExtraSituationalContext.AttackerWithMeleeOrUnarmedAndTargetWithinReachOrYeomanWithLongbow =>
                AttackerNextToTargetOrYeomanWithLongbow(contextParams),

            // supports Monk Shield Expert scenarios
            (ExtraSituationalContext)SituationalContext.NotWearingArmorOrShield =>
                !contextParams.source.IsWearingArmor() &&
                (!contextParams.source.IsWearingShield() || contextParams.source.HasMonkShieldExpert()),

            // supports Monk Shield Expert scenarios
            (ExtraSituationalContext)SituationalContext.NotWearingArmorOrMageArmorOrShield =>
                !contextParams.source.IsWearingArmor() &&
                !contextParams.source.HasConditionOfTypeOrSubType(ConditionMagicallyArmored) &&
                (!contextParams.source.IsWearingShield() || contextParams.source.HasMonkShieldExpert()),

            _ => def
        };
    }

    private static bool AttackerNextToTargetOrYeomanWithLongbow(
        RulesetImplementationDefinitions.SituationalContextParams contextParams)
    {
        var source = contextParams.source;
        var sourceCharacter = GameLocationCharacter.GetFromActor(source);
        var targetCharacter = GameLocationCharacter.GetFromActor(contextParams.target);

        var weapon = source.GetMainWeapon();
        var reachRange = weapon?.ItemDefinition.WeaponDescription.ReachRange ?? 1;

        if (sourceCharacter.IsWithinRange(targetCharacter, reachRange))
        {
            return true;
        }

        var pathOfTheYeomanLevels = source.GetSubclassLevel(
            DatabaseHelper.CharacterClassDefinitions.Barbarian, PathOfTheYeoman.Name);

        return pathOfTheYeomanLevels >= 6 && ValidatorsCharacter.HasLongbow(source);
    }

    private static bool NextToWallWithShieldAndMaxMediumArmorAndConsciousAllyNextToTarget(
        RulesetImplementationDefinitions.SituationalContextParams contextParams)
    {
        if (!contextParams.source.IsWearingShield())
        {
            return false;
        }

        var gameLocationCharacter = GameLocationCharacter.GetFromActor(contextParams.source);

        if (gameLocationCharacter == null)
        {
            return false;
        }

        var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();

        return HasVisibleCharactersOfSideNextToCharacter(gameLocationCharacter) ||
               positioningService.IsNextToWall(gameLocationCharacter.LocationPosition);
    }

    private static bool HasVisibleCharactersOfSideNextToCharacter(GameLocationCharacter character)
    {
        var gridAccessor = GridAccessor.Default;
        var battleSizeParameters = character.BattleSizeParameters;
        var characterLocation = character.LocationPosition;
        var minExtent = battleSizeParameters.minExtent;
        var maxExtent = battleSizeParameters.maxExtent;
        var boxInt = new BoxInt(
            minExtent - new int3(1, 1, 1) + characterLocation,
            maxExtent + new int3(1, 1, 1) + characterLocation);

        foreach (var position in boxInt.EnumerateAllPositionsWithin())
        {
            if (!gridAccessor.Occupants_TryGet(position, out var locationCharacterList))
            {
                continue;
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var locationCharacter in locationCharacterList)
            {
                if (!locationCharacter.CanAct() ||
                    locationCharacter == character ||
                    locationCharacter.Side != Side.Ally)
                {
                    continue;
                }

                var deltaX = Math.Abs(locationCharacter.LocationPosition.x - character.LocationPosition.x);
                var deltaY = Math.Abs(locationCharacter.LocationPosition.y - character.LocationPosition.y);
                var deltaZ = Math.Abs(locationCharacter.LocationPosition.z - character.LocationPosition.z);
                var deltas = deltaX + deltaY + deltaZ;

                return deltas == 1;
            }
        }

        return false;
    }
}
