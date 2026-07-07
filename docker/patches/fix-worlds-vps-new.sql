-- Corrige worlds.Id=18 (Helsephine) para la VPS rollblack-legacy.
-- Sin esto el cliente se congela tras seleccionar servidor (IP/puerto legacy en BD).
UPDATE worlds SET Address = '34.46.208.124', Port = 5557 WHERE Id = 18;
SELECT Id, Name, Address, Port, Status FROM worlds WHERE Id = 18;
