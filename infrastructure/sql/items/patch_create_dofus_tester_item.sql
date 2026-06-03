-- Controlled patch: create Dofus Tester item template if it does not already exist.
-- Audited target:
--   ItemId = 12617
--   DescriptionId = 50091
--   TypeId = 23 (DOFUS)
--   Skin source = Dofus Ocre (template 7754)
--   IconId = 23012
--   AppearanceId = 0
--   Effects hex generated from audited effect ids.

INSERT INTO items (
    Id,
    Weight,
    Name,
    TypeId,
    DescriptionId,
    IconId,
    Level,
    Cursed,
    UseAnimationId,
    Usable,
    Targetable,
    Price,
    TwoHanded,
    Etheral,
    ItemSetId,
    Criteria,
    HideEffects,
    AppearanceId,
    RecipeIdsCSV,
    FavoriteSubAreasCSV,
    BonusIsSecret,
    FavoriteSubAreasBonus,
    Effects
)
SELECT
    12617,
    10,
    'Dofus Tester',
    23,
    50091,
    23012,
    6,
    0,
    -1,
    0,
    0,
    500000.00,
    0,
    0,
    -1,
    'null',
    0,
    0,
    '',
    '',
    0,
    0,
    '000D0046006F0006004600800006004600750003004600B600030046007D01F4004600B000C80046008A01900046007000320046007C00C80046019A00280046019C0028004602F10032004602F00032'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1
    FROM items
    WHERE Id = 12617 OR Name = 'Dofus Tester'
);
