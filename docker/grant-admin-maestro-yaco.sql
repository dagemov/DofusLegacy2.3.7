-- Buscar cuenta del personaje Maestro-Yaco
SELECT c.Id AS CharacterId, c.Name, wc.Account AS AccountId, a.Username, a.Nickname, a.Role
FROM characters c
JOIN worlds_characters wc ON wc.Owner = c.Id
JOIN accounts a ON a.Id = wc.Account
WHERE LOWER(c.Name) LIKE '%maestro%' OR LOWER(c.Name) LIKE '%yaco%';

-- Otorgar Administrator (Role=5)
UPDATE accounts a
JOIN worlds_characters wc ON wc.Account = a.Id
JOIN characters c ON c.Id = wc.Owner
SET a.Role = 5
WHERE LOWER(c.Name) = 'maestro-yaco';

-- Verificar
SELECT c.Name, a.Id AS AccountId, a.Username, a.Role
FROM characters c
JOIN worlds_characters wc ON wc.Owner = c.Id
JOIN accounts a ON a.Id = wc.Account
WHERE LOWER(c.Name) = 'maestro-yaco';
