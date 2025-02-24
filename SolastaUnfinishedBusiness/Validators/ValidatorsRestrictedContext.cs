﻿using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Subclasses;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;

namespace SolastaUnfinishedBusiness.Validators;

public static class ValidatorsRestrictedContext
{
    public static readonly IValidateContextInsteadOfRestrictedProperty IsMeleeOrUnarmedAttack =
        new ValidateContextInsteadOfRestrictedProperty((_, _, _, _, _, mode, rulesetEffect) => (OperationType.Set,
            mode is
            {
                Ranged: false, Thrown: false
            }
            || rulesetEffect?.EffectDescription.RangeType
                is RuleDefinitions.RangeType.Touch
                or RuleDefinitions.RangeType.MeleeHit));

    public static readonly IValidateContextInsteadOfRestrictedProperty IsWeaponOrUnarmedAttack =
        new ValidateContextInsteadOfRestrictedProperty((_, _, _, _, _, mode, _) =>
            (OperationType.Set, mode != null));

    public static readonly IValidateContextInsteadOfRestrictedProperty IsMeleeWeaponAttack =
        new ValidateContextInsteadOfRestrictedProperty((_, _, _, _, _, mode, _) =>
            (OperationType.Set, ValidatorsWeapon.IsMelee(mode)));

    public static readonly IValidateContextInsteadOfRestrictedProperty IsOathOfThunder =
        new ValidateContextInsteadOfRestrictedProperty((_, _, character, _, _, mode, _) =>
            (OperationType.Set, character.GetSubclassLevel(Paladin, OathOfThunder.Name) >= 3 &&
                                OathOfThunder.IsOathOfThunderWeapon(mode, null, character)));

    public static readonly IValidateContextInsteadOfRestrictedProperty IsOathOfDemonHunter =
        new ValidateContextInsteadOfRestrictedProperty((_, _, character, _, _, mode, _) =>
            (OperationType.Set, character.GetSubclassLevel(Paladin, OathOfDemonHunter.Name) >= 3 &&
                                OathOfDemonHunter.IsOathOfDemonHunterWeapon(mode, null, character)));
}
