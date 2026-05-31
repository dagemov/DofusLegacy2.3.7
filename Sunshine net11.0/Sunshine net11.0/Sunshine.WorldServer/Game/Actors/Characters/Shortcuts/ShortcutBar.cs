using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters.Shortcuts;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Characters.Shorcuts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Characters.Shortcuts
{
    public class ShortcutBar
    {
        private readonly Character _character;

        public List<SpellShortcut> SpellShortcuts { get; set; }
        public List<ItemShortcut> ItemShortcuts { get; set; }
        public List<PresetShortcut> PresetShortcuts { get; set; }

        public ShortcutBar(Character character)
        {
            _character = character;
            SpellShortcuts = CharacterManager.Instance.GetShortcuts<SpellShortcut>(character.Id) ?? new List<SpellShortcut>();
            ItemShortcuts = CharacterManager.Instance.GetShortcuts<ItemShortcut>(character.Id) ?? new List<ItemShortcut>();
            PresetShortcuts = CharacterManager.Instance.GetShortcuts<PresetShortcut>(character.Id) ?? new List<PresetShortcut>();
            NormalizeObjectShortcuts(false);
        }

        public IEnumerable<Protocol.Types.Shortcut> GetShortcuts(ShortcutBarEnum barType)
        {
            switch (barType)
            {
                case ShortcutBarEnum.SPELL:
                    return (SpellShortcuts ?? new List<SpellShortcut>()).Where(x => x != null).OrderBy(x => x.Slot).Select(x => x.GetNetworkShortcut()).Where(x => x != null).ToArray();
                case ShortcutBarEnum.OBJECT:
                    NormalizeObjectShortcuts(false);
                    return (ItemShortcuts ?? new List<ItemShortcut>()).Where(x => x != null).Select(x => x.GetNetworkShortcut())
                        .Concat((PresetShortcuts ?? new List<PresetShortcut>()).Where(x => x != null).Select(x => x.GetNetworkShortcut()))
                        .Where(x => x != null).OrderBy(x => x.slot).ToArray();
                default:
                    return Array.Empty<Protocol.Types.Shortcut>();
            }
        }

        public Shortcut GetShortcut(ShortcutBarEnum barType, int slot)
        {
            switch (barType)
            {
                case ShortcutBarEnum.SPELL:
                    return SpellShortcuts.FirstOrDefault(x => x.Slot == slot);
                case ShortcutBarEnum.OBJECT:
                    return (Shortcut)ItemShortcuts.FirstOrDefault(x => x.Slot == slot) ?? PresetShortcuts.FirstOrDefault(x => x.Slot == slot);
                default:
                    return null;
            }
        }

        public void AddShortcut(Protocol.Types.Shortcut shortcut, ShortcutBarEnum barType)
        {
            if (shortcut == null)
                return;

            if (barType == ShortcutBarEnum.SPELL)
            {
                var networkShortcut = shortcut as Protocol.Types.ShortcutSpell;
                if (networkShortcut == null)
                    return;

                if (!IsSlotFree(networkShortcut.slot, barType))
                    RemoveShortcut(barType, networkShortcut.slot);

                var spellShortcut = new SpellShortcut
                {
                    OwnerId = _character.Id,
                    Spell = networkShortcut.spellId,
                    Slot = networkShortcut.slot
                };

                SpellShortcuts.RemoveAll(x => x.Slot == spellShortcut.Slot || x.Spell == spellShortcut.Spell);
                SpellShortcuts.Add(spellShortcut);
                ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, barType, spellShortcut);
                return;
            }

            var itemShortcutNetwork = shortcut as Protocol.Types.ShortcutObjectItem;
            if (itemShortcutNetwork != null)
            {
                NormalizeObjectShortcuts(false);
                var item = _character.Inventory.GetItemUid(itemShortcutNetwork.itemUID);
                if (item == null)
                {
                    ShortcutHandler.SendShortcutBarAddErrorMessage(_character.Client);
                    return;
                }

                int slot = itemShortcutNetwork.slot;
                if (slot < 0)
                {
                    ShortcutHandler.SendShortcutBarAddErrorMessage(_character.Client);
                    return;
                }

                var existingShortcut = ItemShortcuts.FirstOrDefault(x => x.ItemUid == item.Id);
                if (existingShortcut != null)
                {
                    if (existingShortcut.Slot == slot)
                    {
                        existingShortcut.ItemUid = item.Id;
                        existingShortcut.ItemGid = item.Template.Id;
                        ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, barType, existingShortcut);
                        ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
                        return;
                    }

                    ItemShortcuts.Remove(existingShortcut);
                    ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, ShortcutBarEnum.OBJECT, existingShortcut.Slot);
                }

                if (!IsSlotFree(slot, barType))
                    RemoveShortcut(barType, slot);

                var itemShortcut = new ItemShortcut
                {
                    OwnerId = _character.Id,
                    ItemUid = item.Id,
                    ItemGid = item.Template.Id,
                    Slot = slot
                };

                ItemShortcuts.Add(itemShortcut);
                ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, barType, itemShortcut);
                ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
                return;
            }

            var presetShortcutNetwork = shortcut as Protocol.Types.ShortcutObjectPreset;
            if (presetShortcutNetwork == null || _character.Inventory.GetPreset(presetShortcutNetwork.presetId) == null)
                return;

            if (!IsSlotFree(presetShortcutNetwork.slot, barType))
                RemoveShortcut(barType, presetShortcutNetwork.slot);

            PresetShortcuts.RemoveAll(x => x.PresetId == presetShortcutNetwork.presetId || x.Slot == presetShortcutNetwork.slot);
            var presetShortcut = new PresetShortcut
            {
                OwnerId = _character.Id,
                PresetId = presetShortcutNetwork.presetId,
                Slot = presetShortcutNetwork.slot
            };

            PresetShortcuts.Add(presetShortcut);
            ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, barType, presetShortcut);
            ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
        }

        public void RemoveShortcut(ShortcutBarEnum barType, int slot)
        {
            var shortcut = GetShortcut(barType, slot);
            if (shortcut == null)
                return;

            if (barType == ShortcutBarEnum.SPELL)
                SpellShortcuts.Remove((SpellShortcut)shortcut);
            else if (shortcut is ItemShortcut)
                ItemShortcuts.Remove((ItemShortcut)shortcut);
            else if (shortcut is PresetShortcut)
                PresetShortcuts.Remove((PresetShortcut)shortcut);

            ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, barType, slot);
            if (barType == ShortcutBarEnum.OBJECT)
                ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
        }

        public void RemoveItemShortcutsByItemUid(int itemUid)
        {
            var shortcuts = ItemShortcuts.Where(x => x.ItemUid == itemUid).ToList();
            foreach (var shortcut in shortcuts)
            {
                ItemShortcuts.Remove(shortcut);
                ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, ShortcutBarEnum.OBJECT, shortcut.Slot);
            }
            ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
        }

        public void RebindItemShortcuts(int previousItemUid, Game.Items.BasePlayerItem item, bool notifyClient = true)
        {
            if (item == null)
                return;

            var shortcuts = ItemShortcuts.Where(x => x.ItemUid == previousItemUid).ToList();
            foreach (var shortcut in shortcuts)
            {
                shortcut.ItemUid = item.Id;
                shortcut.ItemGid = item.Template.Id;
                if (notifyClient && _character.Client != null)
                    ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, ShortcutBarEnum.OBJECT, shortcut);
            }
        }

        public void CleanupInvalidItemShortcuts(bool notifyClient = true) => NormalizeObjectShortcuts(notifyClient);
        public void SynchronizeItemShortcuts(bool notifyClient = true) => NormalizeObjectShortcuts(notifyClient);

        public void SynchronizePresetShortcuts(bool notifyClient = true)
        {
            foreach (var shortcut in PresetShortcuts.Where(x => _character.Inventory.GetPreset(x.PresetId) == null).ToList())
            {
                PresetShortcuts.Remove(shortcut);
                if (notifyClient && _character.Client != null)
                    ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, ShortcutBarEnum.OBJECT, shortcut.Slot);
            }
        }

        public void SwitchShortcut(ShortcutBarEnum barType, int slot, int newSlot)
        {
            if (slot == newSlot)
                return;

            var shortcut = GetShortcut(barType, slot);
            if (shortcut == null)
            {
                ShortcutHandler.SendShortcutBarSwapErrorMessage(_character.Client);
                return;
            }

            var destinationShortcut = GetShortcut(barType, newSlot);
            SetShortcutSlot(shortcut, newSlot);
            if (destinationShortcut != null)
                SetShortcutSlot(destinationShortcut, slot);

            if (destinationShortcut != null)
                ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, barType, destinationShortcut);
            else
                ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, barType, slot);

            ShortcutHandler.SendShortcutBarRefreshMessage(_character.Client, barType, shortcut);
            if (barType == ShortcutBarEnum.OBJECT)
                ShortcutHandler.SendShortcutBarContentMessage(_character.Client, ShortcutBarEnum.OBJECT);
        }

        private void SetShortcutSlot(Shortcut shortcut, int slot)
        {
            if (shortcut is ItemShortcut itemShortcut)
            {
                itemShortcut.Slot = slot;
                return;
            }
            if (shortcut is SpellShortcut spellShortcut)
            {
                spellShortcut.Slot = slot;
                return;
            }
            if (shortcut is PresetShortcut presetShortcut)
            {
                presetShortcut.Slot = slot;
                return;
            }
            throw new Exception("Unknown shortcut type: " + shortcut.GetType().FullName);
        }

        public void AddNextShortcuts(ShortcutBarEnum barType)
        {
            if (barType != ShortcutBarEnum.SPELL)
                return;

            int nextFreeSlot;
            var breedSpells = BreedManager.Instance.GetSpells(_character.Breed).Where(x => x.ObtainLevel <= _character.Level);
            var spellShorcuts = GetShortcuts(barType).ToDictionary(entry => (entry as Protocol.Types.ShortcutSpell).spellId);
            foreach (var breed in breedSpells)
            {
                if (spellShorcuts.ContainsKey((short)breed.Spell))
                    continue;

                nextFreeSlot = GetNextFreeSlot(barType);
                AddShortcut(new Protocol.Types.ShortcutSpell(nextFreeSlot, (short)breed.Spell), barType);
            }
        }

        public void EnsurePresetShortcut(int presetId)
        {
            if (PresetShortcuts.Any(x => x.PresetId == presetId))
                return;
            int slot = GetNextFreeSlot(ShortcutBarEnum.OBJECT);
            if (slot >= 40)
                return;
            AddShortcut(new Protocol.Types.ShortcutObjectPreset(slot, (sbyte)presetId), ShortcutBarEnum.OBJECT);
        }

        public void RemovePresetShortcut(int presetId)
        {
            foreach (var shortcut in PresetShortcuts.Where(x => x.PresetId == presetId).ToList())
            {
                PresetShortcuts.Remove(shortcut);
                ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, ShortcutBarEnum.OBJECT, shortcut.Slot);
            }
        }

        public bool IsSlotFree(int slot, ShortcutBarEnum barType)
        {
            if (barType == ShortcutBarEnum.SPELL)
                return SpellShortcuts.FirstOrDefault(x => x.Slot == slot) == null;
            if (barType != ShortcutBarEnum.OBJECT)
                return false;
            return ItemShortcuts.FirstOrDefault(x => x.Slot == slot) == null && PresetShortcuts.FirstOrDefault(x => x.Slot == slot) == null;
        }

        public int GetNextFreeSlot(ShortcutBarEnum barType)
        {
            for (int i = 0; i < 40; i++)
                if (IsSlotFree(i, barType))
                    return i;
            return 40;
        }

        private void NormalizeObjectShortcuts(bool notifyClient)
        {
            ItemShortcuts ??= new List<ItemShortcut>();
            PresetShortcuts ??= new List<PresetShortcut>();

            var removedSlots = new HashSet<int>();

            foreach (var duplicate in ItemShortcuts.Where(x => x != null).GroupBy(x => x.Slot).Where(x => x.Count() > 1))
                foreach (var extra in duplicate.Skip(1).ToList())
                {
                    ItemShortcuts.Remove(extra);
                    removedSlots.Add(extra.Slot);
                }

            foreach (var duplicate in PresetShortcuts.Where(x => x != null).GroupBy(x => x.Slot).Where(x => x.Count() > 1))
                foreach (var extra in duplicate.Skip(1).ToList())
                {
                    PresetShortcuts.Remove(extra);
                    removedSlots.Add(extra.Slot);
                }

            foreach (var shortcut in ItemShortcuts.ToList())
            {
                var item = _character.Inventory != null ? _character.Inventory.GetItemUid(shortcut.ItemUid) : null;
                if (item == null)
                {
                    ItemShortcuts.Remove(shortcut);
                    removedSlots.Add(shortcut.Slot);
                    continue;
                }

                if (shortcut.ItemGid != item.Template.Id)
                    shortcut.ItemGid = item.Template.Id;
            }

            foreach (var duplicate in ItemShortcuts.Where(x => x != null).GroupBy(x => x.ItemUid).Where(x => x.Count() > 1))
                foreach (var extra in duplicate.Skip(1).ToList())
                {
                    ItemShortcuts.Remove(extra);
                    removedSlots.Add(extra.Slot);
                }

            foreach (var presetShortcut in PresetShortcuts.ToList())
            {
                if (_character.Inventory != null && _character.Inventory.GetPreset(presetShortcut.PresetId) != null)
                    continue;

                PresetShortcuts.Remove(presetShortcut);
                removedSlots.Add(presetShortcut.Slot);
            }

            if (notifyClient && _character.Client != null)
                foreach (var slot in removedSlots)
                    ShortcutHandler.SendShortcutBarRemovedMessage(_character.Client, ShortcutBarEnum.OBJECT, slot);
        }
    }
}
