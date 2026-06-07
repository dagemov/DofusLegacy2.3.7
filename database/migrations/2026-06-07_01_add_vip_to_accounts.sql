-- Migration: add VIP flag to accounts
-- Idempotent at runtime via AccountVipBootstrap.EnsureVipColumn().
-- Apply manually if you prefer a controlled migration over the runtime bootstrap.

ALTER TABLE `accounts`
    ADD COLUMN IF NOT EXISTS `Vip` tinyint(1) NOT NULL DEFAULT 0 AFTER `NewTokens`;
