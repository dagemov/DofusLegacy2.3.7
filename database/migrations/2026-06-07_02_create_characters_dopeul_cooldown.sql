-- Dopeul: tabla de cooldown por personaje y monstruo (3h por defecto).
-- El servidor tambien crea esta tabla en runtime via CharacterDopeulBootstrap.
CREATE TABLE IF NOT EXISTS `characters_dopeul_cooldown` (
    `CharacterId` int(11) NOT NULL,
    `MonsterId` int(11) NOT NULL,
    `LastFightTime` datetime NOT NULL,
    PRIMARY KEY (`CharacterId`, `MonsterId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
