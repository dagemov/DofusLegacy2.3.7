-- Controlled restore: revert the visible QA tester vendor fallback.
--
-- Restores:
-- - template 7754 original base effects
-- - vendor row for NPC 1053 selling template 7754

SET @visible_template_id := 7754;
SET @vendor_npc_id := 1053;
SET @original_effects_hex := '00010000006F00000001000000000000000000000000000000000000000000096E756C6C207A6F6E650000000000000000000000000000000000000000000000000000';

UPDATE items
SET Effects = @original_effects_hex
WHERE Id = @visible_template_id
  AND TypeId = 23;

DELETE FROM npcs_items
WHERE NpcId = @vendor_npc_id
  AND Item = @visible_template_id;
