-- Restore npcs_items from unified9 backup
TRUNCATE TABLE npcs_items;
INSERT INTO npcs_items SELECT * FROM npcs_items_backup_unified9;
