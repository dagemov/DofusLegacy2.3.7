-- Controlled patch: enable an immediately visible QA tester item by
-- reusing a client-known Dofus template and adding it to the Dofus vendor.
--
-- Visible fallback chosen:
--   TemplateId = 7754
--   Name       = Dofus Ocre (client-known)
--   TypeId     = 23 (DOFUS)
--   IconId     = 23012
--
-- Why this template:
-- - client already knows template 7754, so it is renderable
-- - it shares the same icon as server-only template 12617
-- - it currently has zero owned rows in production
-- - it is not currently sold by NPC 1053, so adding a QA sale row is controlled
--
-- Important:
-- - this does NOT make 12617 visible
-- - this temporarily repurposes template 7754 for QA by changing its base effects
-- - future creations of template 7754 will use the tester effects until restored

SET @visible_template_id := 7754;
SET @vendor_npc_id := 1053;
SET @vendor_price := 500000;
SET @tester_effects_hex := '00070000006F0000000A000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000500000000000000000000000000000000000800000000A000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000005000000000000000000000000000000000007500000003000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000500000000000000000000000000000000000B600000003000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000500000000000000000000000000000000000B000000096000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000005000000000000000000000000000000000008A000001F4000000000000000000000000000000000000000000096E756C6C207A6F6E6500000000000000000000005000000000000000000000000000000000007000000032000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000500000000000000000000000000000';

UPDATE items
SET Effects = @tester_effects_hex
WHERE Id = @visible_template_id
  AND TypeId = 23;

INSERT INTO npcs_items (NpcId, Item, Price, Token)
SELECT @vendor_npc_id, @visible_template_id, @vendor_price, 0
FROM DUAL
WHERE EXISTS (
    SELECT 1
    FROM items
    WHERE Id = @visible_template_id
      AND TypeId = 23
)
AND NOT EXISTS (
    SELECT 1
    FROM npcs_items
    WHERE NpcId = @vendor_npc_id
      AND Item = @visible_template_id
);

UPDATE npcs_items
SET Price = @vendor_price,
    Token = 0
WHERE NpcId = @vendor_npc_id
  AND Item = @visible_template_id;
