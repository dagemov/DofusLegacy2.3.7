-- Controlled patch: grant exactly one stack of 20 Dofus Tester items
-- to each character linked to account sebcos1, only if that character
-- does not already have template 12617 in characters_items.
--
-- Important operational note:
-- apply this only when target characters are offline, or while World is
-- stopped during the controlled restart window. Otherwise the live save
-- cycle can overwrite offline inventory writes.

SET @template_id := 12617;
SET @effects_hex := '000D0046006F0006004600800006004600750003004600B600030046007D01F4004600B000C80046008A01900046007000320046007C00C80046019A00280046019C0028004602F10032004602F00032';
SET @position := 63;
SET @stack := 20;

SET @base_uid := (
    SELECT COALESCE(MAX(src.ItemUid), 0)
    FROM (
        SELECT COALESCE(MAX(ItemUid), 0) AS ItemUid FROM characters_items
        UNION ALL
        SELECT COALESCE(MAX(ItemUid), 0) FROM account_bank_items
        UNION ALL
        SELECT COALESCE(MAX(ItemUid), 0) FROM characters_items_merchant
        UNION ALL
        SELECT COALESCE(MAX(ItemUid), 0) FROM house_chest_items
        UNION ALL
        SELECT COALESCE(MAX(ItemUid), 0) FROM world_trashes_items
    ) AS src
);

SET @next_uid := @base_uid;

INSERT INTO characters_items (
    OwnerId,
    Item,
    Position,
    Stack,
    Effects,
    ItemUid
)
SELECT
    target.CharacterId,
    @template_id,
    @position,
    @stack,
    @effects_hex,
    (@next_uid := @next_uid + 1)
FROM (
    SELECT DISTINCT wc.Owner AS CharacterId
    FROM accounts a
    INNER JOIN worlds_characters wc ON wc.Account = a.Id
    WHERE a.Username = 'sebcos1'
) AS target
WHERE EXISTS (
    SELECT 1
    FROM items i
    WHERE i.Id = @template_id
)
AND NOT EXISTS (
    SELECT 1
    FROM characters_items ci
    WHERE ci.OwnerId = target.CharacterId
      AND ci.Item = @template_id
);
