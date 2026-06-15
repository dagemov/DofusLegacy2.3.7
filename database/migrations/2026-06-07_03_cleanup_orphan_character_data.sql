-- Libera nombres bloqueados por borrados incompletos de personajes.
-- Ejecutar una vez en bases existentes; el flujo del servidor ya hace cascade completo.

DELETE wc FROM worlds_characters wc
LEFT JOIN characters c ON c.Id = wc.Owner
WHERE c.Id IS NULL;

DELETE cs FROM characters_stats cs
LEFT JOIN characters c ON c.Id = cs.OwnerId
WHERE c.Id IS NULL;

DELETE ca FROM characters_alignments ca
LEFT JOIN characters c ON c.Id = ca.OwnerId
WHERE c.Id IS NULL;

DELETE csp FROM characters_spells csp
LEFT JOIN characters c ON c.Id = csp.OwnerId
WHERE c.Id IS NULL;

DELETE ci FROM characters_items ci
LEFT JOIN characters c ON c.Id = ci.OwnerId
WHERE c.Id IS NULL;

DELETE cim FROM characters_items_merchant cim
LEFT JOIN characters c ON c.Id = cim.OwnerId
WHERE c.Id IS NULL;

DELETE cip FROM characters_items_presets cip
LEFT JOIN characters c ON c.Id = cip.OwnerId
WHERE c.Id IS NULL;

DELETE csi FROM characters_shortcuts_items csi
LEFT JOIN characters c ON c.Id = csi.OwnerId
WHERE c.Id IS NULL;

DELETE css FROM characters_shortcuts_spells css
LEFT JOIN characters c ON c.Id = css.OwnerId
WHERE c.Id IS NULL;

DELETE csip FROM characters_shortcuts_items_presets csip
LEFT JOIN characters c ON c.Id = csip.OwnerId
WHERE c.Id IS NULL;

DELETE cj FROM characters_jobs cj
LEFT JOIN characters c ON c.Id = cj.OwnerId
WHERE c.Id IS NULL;

DELETE cq FROM characters_quests cq
LEFT JOIN characters c ON c.Id = cq.OwnerId
WHERE c.Id IS NULL;

DELETE cqo FROM characters_quests_objectives cqo
LEFT JOIN characters c ON c.Id = cqo.OwnerId
WHERE c.Id IS NULL;

DELETE cqs FROM characters_quests_steps cqs
LEFT JOIN characters c ON c.Id = cqs.OwnerId
WHERE c.Id IS NULL;

DELETE cf FROM character_friends cf
LEFT JOIN characters c ON c.Id = cf.CharacterId
WHERE c.Id IS NULL;

DELETE cf2 FROM character_friends cf2
LEFT JOIN characters c ON c.Id = cf2.FriendCharacterId
WHERE c.Id IS NULL;

DELETE ce FROM character_enemies ce
LEFT JOIN characters c ON c.Id = ce.CharacterId
WHERE c.Id IS NULL;

DELETE ce2 FROM character_enemies ce2
LEFT JOIN characters c ON c.Id = ce2.EnemyCharacterId
WHERE c.Id IS NULL;

DELETE cdc FROM characters_dopeul_cooldown cdc
LEFT JOIN characters c ON c.Id = cdc.CharacterId
WHERE c.Id IS NULL;

DELETE wmm FROM world_maps_merchant wmm
LEFT JOIN characters c ON c.Id = wmm.CharacterId
WHERE c.Id IS NULL;

-- Personajes sin enlace de cuenta (huérfanos en characters)
DELETE c FROM characters c
LEFT JOIN worlds_characters wc ON wc.Owner = c.Id
WHERE wc.Owner IS NULL;
