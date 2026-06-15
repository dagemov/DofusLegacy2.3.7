DELETE FROM characters_items WHERE OwnerId = 358;
DELETE FROM characters_items_merchant WHERE OwnerId = 358;
DELETE FROM characters_stats WHERE OwnerId = 358;
DELETE FROM characters_spells WHERE OwnerId = 358;
DELETE FROM characters_alignments WHERE OwnerId = 358;
DELETE FROM characters_shortcuts_items WHERE OwnerId = 358;
DELETE FROM characters_shortcuts_spells WHERE OwnerId = 358;
DELETE FROM characters_jobs WHERE OwnerId = 358;
DELETE FROM characters_quests WHERE OwnerId = 358;
DELETE FROM characters_quests_objectives WHERE OwnerId = 358;
DELETE FROM characters_quests_steps WHERE OwnerId = 358;
DELETE FROM characters_items_presets WHERE OwnerId = 358;
DELETE FROM characters_shortcuts_items_presets WHERE OwnerId = 358;
DELETE FROM character_friends WHERE CharacterId = 358 OR FriendCharacterId = 358;
DELETE FROM character_enemies WHERE CharacterId = 358 OR EnemyCharacterId = 358;
DELETE FROM characters_dopeul_cooldown WHERE CharacterId = 358;
DELETE FROM world_maps_merchant WHERE CharacterId = 358;
DELETE FROM worlds_characters WHERE Owner = 358;
DELETE FROM characters WHERE Id = 358;

SELECT 'cleanup_358' AS step,
  (SELECT COUNT(*) FROM characters WHERE Id=358) AS chars,
  (SELECT COUNT(*) FROM worlds_characters WHERE Owner=358) AS links,
  (SELECT COUNT(*) FROM characters_stats WHERE OwnerId=358) AS stats,
  (SELECT COUNT(*) FROM characters_items WHERE OwnerId=358) AS items;
