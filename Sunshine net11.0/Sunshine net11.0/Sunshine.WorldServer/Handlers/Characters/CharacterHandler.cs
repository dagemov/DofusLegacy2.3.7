using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Types.Custom;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Handlers;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Handlers.Characters;
using Sunshine.MySql.Database.Auth;
using Sunshine.WorldServer.Game;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Chat;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Context.RolePlay.Party;
using Sunshine.WorldServer.Handlers.Characters.Shorcuts;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Handlers.PvP;
using Sunshine.WorldServer.Handlers.Characters.Jobs;
using Sunshine.WorldServer.Handlers.Guilds;
using Sunshine.WorldServer.Handlers.Mounts;
using Sunshine.WorldServer.Game.Actors.Look;

namespace Sunshine.WorldServer.Handlers.Characters
{
    public class CharacterHandler : WorldPacketHandler
    {
        private static readonly System.Text.RegularExpressions.Regex CharacterNameRegex =
            new System.Text.RegularExpressions.Regex("^[A-Z][a-z]{2,9}(?:-[A-Z][a-z]{2,9}|[a-z]{1,10})$", System.Text.RegularExpressions.RegexOptions.Compiled);

        [WorldHandler(150)]
        public static void HandleCharactersListRequestMessage(WorldClient client, CharactersListRequestMessage message)
        {
            if (client.Account != null && client.Account.Username != "")
            {
                if (client.Characters.Length <= 0)
                    client.Send(new CharactersListMessage(false, new List<CharacterBaseInformations>()));
                else
                    CharacterHandler.SendCharactersListMessage(client, false);
            }
            else
                client.Send(new IdentificationFailedMessage(4));
        }

        [WorldHandler(160)]
        public static void HandleCharacterCreationRequestMessage(WorldClient client, CharacterCreationRequestMessage message)
        {
            if (client.Characters.Length < 5)
            {
                if (string.IsNullOrWhiteSpace(message.name) || !CharacterNameRegex.IsMatch(message.name.Trim()))
                {
                    client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_INVALID_NAME));
                    return;
                }

                if (IsCharacterNameTaken(message.name.Trim()))
                {
                    client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_NAME_ALREADY_EXISTS));
                    return;
                }

                CharacterRecord _character = new CharacterRecord
                {
                    Level = 1,
                    Name = message.name.Trim(),
                    Breed = message.breed,
                    Sex = message.sex,
                    EntityLook = EntityManager.Instance.BuildEntityLook(message.breed, message.sex, message.colors.ToList()),
                    MapId = BreedManager.Instance.GetStartMap(message.breed),
                    CellId = BreedManager.Instance.GetStartCell(message.breed),
                    Direction = BreedManager.Instance.GetStartDirection(message.breed),
                    SpellsPoints = 1
                };

                CharacterManager.Instance.CreateCharacter(client.Account.Id, _character);
                client.LastCharacter = AccountManager.Instance.GetCharacter(message.name.Trim()).Id;
                CharacterManager.Instance.Characters.Add(client.LastCharacter.Value, new Character(_character));
                client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.OK));
                CharacterHandler.SendCharactersListMessage(client);
            }
            else
                client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_TOO_MANY_CHARACTERS));
        }

        [WorldHandler(6084)]
        public static void HandleCharacterFirstSelectionMessage(WorldClient client, CharacterFirstSelectionMessage message)
        {
            if (!CharacterManager.Instance.Characters.TryGetValue(message.id, out var character) || character == null)
                return;

            // Tutorial guided mode fully disabled on this emulator.
            CommonCharacterSelection(client, character, false);
        }

        [WorldHandler(152)]
        public static void HandleCharacterSelectionMessage(WorldClient client, CharacterSelectionMessage message)
        {
            var characterId = client.LastCharacter.HasValue ? client.LastCharacter.Value : message.id;
            var character = ResolveOwnedCharacter(client, characterId);
            if (character == null)
            {
                client.Send(new CharacterSelectedErrorMessage());
                client.LastCharacter = null;
                return;
            }

            CharacterHandler.CommonCharacterSelection(client, character);
            client.LastCharacter = null;
        }


        [WorldHandler(CharacterSelectionWithRenameMessage.Id)]
        public static void HandleCharacterSelectionWithRenameMessage(WorldClient client, CharacterSelectionWithRenameMessage message)
        {
            var character = ResolveOwnedCharacter(client, message.id);
            if (character == null || !character.Record.CanUseRename)
                return;

            if (!TryValidateCharacterName(client, character, message.name, out var normalizedName))
                return;

            if (string.Equals(character.Name, normalizedName, StringComparison.Ordinal))
            {
                character.Record.CanUseRename = false;
                CharacterManager.Instance.SaveSelectionRemodel(character);
                CommonCharacterSelection(client, character);
                client.LastCharacter = null;
                return;
            }

            character.Name = normalizedName;
            character.Record.CanUseRename = false;
            CharacterManager.Instance.SaveSelectionRemodel(character);
            CommonCharacterSelection(client, character);
            client.LastCharacter = null;
        }

        [WorldHandler(CharacterSelectionWithRecolorMessage.Id)]
        public static void HandleCharacterSelectionWithRecolorMessage(WorldClient client, CharacterSelectionWithRecolorMessage message)
        {
            var character = ResolveOwnedCharacter(client, message.id);
            if (character == null || !character.Record.CanUseRecolor)
                return;

            var colors = message.indexedColor != null ? message.indexedColor.ToList() : new List<int>();

            var look = GetCharacterLook(character);
            ApplyIndexedColors(look, character.Breed, character.Sex, colors, GetIndexedColors(look));
            character.Record.EntityLook = EntityManager.Instance.ParseEntityLook(look.GetEntityLook());
            character.Look = EntityManager.Instance.GetActorLook(character.Record.EntityLook);
            character.Record.CustomLook = string.Empty;
            character.Record.CanUseRecolor = false;
            CharacterManager.Instance.SaveSelectionRemodel(character);
            CommonCharacterSelection(client, character);
            client.LastCharacter = null;
        }

        [WorldHandler(CharacterSelectionWithRemodelMessage.Id)]
        public static void HandleCharacterSelectionWithRemodelMessage(WorldClient client, CharacterSelectionWithRemodelMessage message)
        {
            var character = ResolveOwnedCharacter(client, message != null ? message.id : 0);
            if (character == null)
            {
                client.Send(new CharacterSelectedErrorMessage());
                return;
            }

            var remodel = message != null ? message.remodel : null;
            bool hasPendingChanges = character.Record.CanUseRelook || character.Record.CanUseRecolor || character.Record.CanUseRename;

            if (hasPendingChanges && remodel != null)
            {
                remodel.Name = remodel.Name ?? string.Empty;
                remodel.Colors = remodel.Colors ?? new List<int>();

                if (character.Record.CanUseRename && !string.IsNullOrWhiteSpace(remodel.Name) && !string.Equals(character.Name, remodel.Name.Trim(), StringComparison.Ordinal))
                {
                    if (!TryValidateCharacterName(client, character, remodel.Name, out var normalizedName))
                        return;

                    character.Name = normalizedName;
                    character.Record.CanUseRename = false;
                }

                if (character.Record.CanUseRelook || character.Record.CanUseRecolor)
                {
                    bool targetSex = character.Record.CanUseRelook ? remodel.Sex : character.Sex;
                    var requestedColors = remodel.Colors != null ? remodel.Colors.ToList() : new List<int>();
                    var storedColors = GetIndexedColors(GetCharacterLook(character));
                    var colorsToApply = requestedColors.Count > 0 ? requestedColors : storedColors;

                    character.Sex = targetSex;

                    // Rebuild complet à partir du look de base du breed/sex.
                    // On n'utilise volontairement pas Head/Cosmetic pour le changement de sexe en 2.3.7.
                    var rebuiltLook = GetBaseCharacterLook(character.Breed, character.Sex);
                    ApplyIndexedColors(rebuiltLook, character.Breed, character.Sex, colorsToApply, storedColors);

                    character.Record.EntityLook = EntityManager.Instance.ParseEntityLook(rebuiltLook.GetEntityLook());
                    character.Record.CustomLook = string.Empty;
                    character.Look = rebuiltLook;
                    // Ne pas appeler UpdateLook(false) pendant la sélection.
                    // Le personnage n'est pas encore entré en jeu.
                    character.Record.CanUseRelook = false;
                    character.Record.CanUseRecolor = false;
                }

                CharacterManager.Instance.SaveSelectionRemodel(character);
            }

            CommonCharacterSelection(client, character);
            client.LastCharacter = null;
        }

        [WorldHandler(162)]
        public static void HandleCharacterNameSuggestionRequestMessage(WorldClient client, CharacterNameSuggestionRequestMessage message)
        {
            string nameGenerate = CharacterManager.Instance.GenerateName();
            client.Send(new CharacterNameSuggestionSuccessMessage(nameGenerate));
        }

        [WorldHandler(165)]
        public static void HandleCharacterDeletionRequestMessage(WorldClient client, CharacterDeletionRequestMessage message)
        {
            Character character = null;
            if (!CharacterManager.Instance.Characters.TryGetValue(message.characterId, out character) || character == null)
            {
                var record = AccountManager.Instance.GetCharacter(message.characterId);
                if (record == null)
                {
                    client.Send(new CharacterDeletionErrorMessage(1));
                    return;
                }

                var ownerAccount = AccountManager.Instance.GetAccount(message.characterId);
                if (ownerAccount == null || ownerAccount.Id != client.Account.Id)
                {
                    client.Send(new CharacterDeletionErrorMessage(1));
                    return;
                }
            }
            else if (character.Account.Id != client.Account.Id)
            {
                client.Send(new CharacterDeletionErrorMessage(1));
                return;
            }

            if (string.IsNullOrEmpty(client.Account.SecretAnswer))
            {
                client.Send(new CharacterDeletionErrorMessage(3));
                return;
            }

            // Le client envoie le hash MD5 de : characterId + "~" + réponse secrète.
            // L'ancien code recalculait bien ce hash, puis l'écrasait immédiatement avec
            // MD5(réponse secrète), ce qui rendait la vérification invalide.
            var expectedSecretAnswerHash = Utils.GetMD5Hash(message.characterId + "~" + client.Account.SecretAnswer);

            if (!string.Equals(message.secretAnswerHash, expectedSecretAnswerHash, StringComparison.OrdinalIgnoreCase))
            {
                client.Send(new CharacterDeletionErrorMessage(3));
                return;
            }

            CharacterManager.Instance.DeleteCharacterOnAccount(client, message.characterId);
            SendCharactersListMessage(client, false);
        }

        public static void SendCharactersListMessage(WorldClient client)
        {
            SendCharactersListMessage(client, false);
        }

        public static void SendCharactersListMessage(WorldClient client, bool tuto)
        {
            var chrBaseInformations = new List<CharacterBaseInformations>();
            var charactersToRecolor = new List<CharacterToRecolorInformation>();
            var charactersToRename = new List<int>();
            var unusableCharacters = new List<int>();

            foreach (var character in client.Characters)
            {
                if (!CharacterManager.Instance.Characters.TryGetValue(character.Id, out var runtime) || runtime == null)
                    continue;

                // Dofus 2.3.7.35100 / DofusInvoker ne supporte pas charactersToRelook.
                // Si CanUseRelook est envoyé comme CanUseRecolor, le client ouvre le changement de couleur.
                ApplyPendingSexRelookForDofus237(runtime);

                chrBaseInformations.Add(runtime.GetCharacterBaseInformations());

                if (runtime.Record == null)
                    continue;

                // DofusInvoker 2.3.7 : modifications = recolor / rename / unusable uniquement.
                // Ne jamais ajouter CanUseRelook ici.
                if (runtime.Record.CanUseRecolor)
                    charactersToRecolor.Add(BuildCharacterToRecolorInformation(runtime));

                if (runtime.Record.CanUseRename)
                    charactersToRename.Add(runtime.Id);
            }

            if (charactersToRecolor.Count > 0 || charactersToRename.Count > 0 || unusableCharacters.Count > 0)
            {
                client.Send(new CharactersListWithModificationsMessage(
                    tuto,
                    chrBaseInformations,
                    charactersToRecolor,
                    charactersToRename,
                    unusableCharacters));
            }
            else
            {
                client.Send(new CharactersListMessage(tuto, chrBaseInformations));
            }
        }

        private static void ApplyPendingSexRelookForDofus237(Character character)
        {
            if (character == null || character.Record == null || !character.Record.CanUseRelook)
                return;

            var storedColors = GetIndexedColors(GetCharacterLook(character));

            // Dofus 2.3.7 / DofusInvoker ne fournit pas d'écran de sélection du sexe
            // dans CharactersListWithModificationsMessage. Le service relook sexe doit donc
            // être appliqué côté serveur avant l'envoi de la liste personnages.
            character.Sex = !character.Sex;

            var rebuiltLook = GetBaseCharacterLook(character.Breed, character.Sex);
            ApplyIndexedColors(rebuiltLook, character.Breed, character.Sex, storedColors, storedColors);

            character.Record.EntityLook = EntityManager.Instance.ParseEntityLook(rebuiltLook.GetEntityLook());
            character.Record.CustomLook = string.Empty;
            character.Look = rebuiltLook;
            character.Record.CanUseRelook = false;

            CharacterManager.Instance.SaveSelectionRemodel(character);
        }

        private static bool HasPendingRemodeling(Character character)
        {
            return character != null && character.Record != null &&
                   (character.Record.CanUseRename || character.Record.CanUseRecolor || character.Record.CanUseRelook);
        }

        private static CharacterToRecolorInformation BuildCharacterToRecolorInformation(Character character)
        {
            var look = GetCharacterLook(character);
            var colors = GetRecolorClientIndexedColors(character, look);
            return new CharacterToRecolorInformation(character.Id, colors);
        }

        private static List<int> GetRecolorClientIndexedColors(Character character, ActorLook look)
        {
            var colors = GetIndexedColors(look);
            var baseColors = GetBreedBaseColors(character.Breed, character.Sex)
                .Select(NormalizeClientColor)
                .ToList();

            var result = new List<int>();

            for (int index = 1; index <= 5; index++)
            {
                int color = index - 1 < colors.Count ? colors[index - 1] : -1;

                if (color == -1 && index - 1 < baseColors.Count)
                    color = baseColors[index - 1];

                if (color == -1 || color == int.MinValue)
                    continue;

                // Couleur indexée attendue par le client 2.3.7 : index << 24 | RGB.
                // Noir index 1 = 0x01000000 : il reste noir, il n'est pas transformé en gris.
                result.Add(((index & 0xFF) << 24) | (color & 0xFFFFFF));
            }

            return result;
        }

        private static CharacterToRemodelInformation BuildCharacterToRemodelInformation(Character character)
        {
            var look = GetCharacterLook(character);
            var colors = GetRemodelingClientColors(character, look);
            sbyte changeMask = BuildCharacterRemodelingMask(character);
            short cosmeticId = (short)Math.Max(1, Math.Min(short.MaxValue, ExtractCharacterCosmeticId(character)));

            return new CharacterToRemodelInformation(
                character.Id,
                character.Name,
                (sbyte)character.Breed,
                character.Sex,
                cosmeticId,
                colors,
                changeMask,
                changeMask);
        }

        private static sbyte BuildCharacterRemodelingMask(Character character)
        {
            sbyte mask = 0;

            if (character == null || character.Record == null)
                return mask;

            if (character.Record.CanUseRename)
                mask |= (sbyte)CharacterRemodelingEnum.CHARACTER_REMODELING_NAME;

            if (character.Record.CanUseRecolor)
                mask |= (sbyte)CharacterRemodelingEnum.CHARACTER_REMODELING_COLORS;

            if (character.Record.CanUseRelook)
            {
                mask |= (sbyte)CharacterRemodelingEnum.CHARACTER_REMODELING_GENDER;
                mask |= (sbyte)CharacterRemodelingEnum.CHARACTER_REMODELING_COLORS;
            }

            return mask;
        }

        private static Character ResolveOwnedCharacter(WorldClient client, int characterId)
        {
            if (client == null || client.Account == null)
                return null;

            if (!CharacterManager.Instance.Characters.TryGetValue(characterId, out var character))
                return null;

            return character != null && character.Account != null && character.Account.Id == client.Account.Id ? character : null;
        }

        private static bool TryValidateCharacterName(WorldClient client, Character character, string requestedName, out string normalizedName)
        {
            normalizedName = requestedName != null ? requestedName.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedName) || !CharacterNameRegex.IsMatch(normalizedName))
            {
                client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_INVALID_NAME));
                return false;
            }

            if (!string.Equals(character?.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                IsCharacterNameTaken(normalizedName, character))
            {
                client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_NAME_ALREADY_EXISTS));
                return false;
            }

            return true;
        }

        private static bool IsCharacterNameTaken(string name, Character exceptCharacter = null)
        {
            if (AccountManager.Instance.IsCharacterNameTaken(name, exceptCharacter?.Id))
                return true;

            return CharacterManager.Instance.Characters.Values.Any(entry =>
                entry != null &&
                (exceptCharacter == null || entry.Id != exceptCharacter.Id) &&
                string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static ActorLook GetCharacterLook(Character character)
        {
            if (character == null)
                return new ActorLook();

            var look = GetBaseCharacterLook(character.Breed, character.Sex);

            try
            {
                var stored = !string.IsNullOrWhiteSpace(character.Record?.EntityLook)
                    ? EntityManager.Instance.GetActorLook(character.Record.EntityLook)
                    : null;

                if (stored != null && stored.Colors != null)
                {
                    look.Colors.Clear();
                    foreach (var color in stored.Colors)
                        look.AddColor(color.Key, color.Value);
                }
                else if (character.Look != null && character.Look.Colors != null)
                {
                    look.Colors.Clear();
                    foreach (var color in character.Look.Colors)
                        look.AddColor(color.Key, color.Value);
                }
            }
            catch
            {
            }

            return look;
        }

        private static ActorLook GetBaseCharacterLook(int breedId, bool sex)
        {
            var breedLook = BreedManager.Instance.GetLook(breedId, sex);
            return EntityManager.Instance.GetActorLook(breedLook);
        }

        private static List<int> GetBreedBaseColors(int breedId, bool sex)
        {
            if (!BreedManager.Instance.BreedColors.ContainsKey(breedId))
                return new List<int>();

            return sex
                ? BreedManager.Instance.BreedColors[breedId].FemaleColors.ConvertAll(entry => (int)entry)
                : BreedManager.Instance.BreedColors[breedId].MaleColors.ConvertAll(entry => (int)entry);
        }

        private static int NormalizeClientColor(int color)
        {
            if (color == -1 || color == int.MinValue)
                return -1;

            return color & 0xFFFFFF;
        }

        private static List<int> NormalizeRequestedColors(IEnumerable<int> colors)
        {
            var values = colors != null ? colors.ToList() : new List<int>();
            if (values.Count == 0)
                return new List<int>();

            bool looksPackedByIndex = values.Any(value =>
            {
                if (value == -1 || value == int.MinValue)
                    return false;

                int index = (int)(((uint)value >> 24) & 0xFF);
                return index >= 1 && index <= 5;
            });

            if (!looksPackedByIndex)
                return values.Select(NormalizeClientColor).ToList();

            var result = Enumerable.Repeat(-1, 5).ToList();
            foreach (var value in values)
            {
                if (value == -1 || value == int.MinValue)
                    continue;

                int index = (int)(((uint)value >> 24) & 0xFF);
                if (index < 1 || index > 5)
                    continue;

                result[index - 1] = NormalizeClientColor(value);
            }

            return result;
        }

        private static List<int> GetIndexedColors(ActorLook look)
        {
            var result = Enumerable.Repeat(-1, 5).ToList();

            if (look?.Colors == null || look.Colors.Count == 0)
                return result;

            foreach (var entry in look.Colors)
            {
                if (entry.Key < 1 || entry.Key > 5)
                    continue;

                result[entry.Key - 1] = NormalizeClientColor(entry.Value.ToArgb());
            }

            return result;
        }

        private static List<int> GetRemodelingClientColors(Character character, ActorLook look)
        {
            var colors = GetIndexedColors(look);
            var baseColors = GetBreedBaseColors(character.Breed, character.Sex)
                .Select(NormalizeClientColor)
                .ToList();

            int count = Math.Max(colors.Count, baseColors.Count);
            count = Math.Min(Math.Max(count, 5), 5);

            var result = new List<int>();
            for (int index = 0; index < count; index++)
            {
                int color = index < colors.Count ? colors[index] : -1;
                if (color == -1 && index < baseColors.Count)
                    color = baseColors[index];

                if (color != -1)
                    result.Add(NormalizeClientColor(color));
            }

            return result;
        }

        private static void ApplyIndexedColors(ActorLook look, int breedId, bool sex, IEnumerable<int> requestedColors, IEnumerable<int> fallbackColors = null)
        {
            if (look == null)
                return;

            var requested = NormalizeRequestedColors(requestedColors);
            var fallback = NormalizeRequestedColors(fallbackColors);
            var baseColors = GetBreedBaseColors(breedId, sex).Select(NormalizeClientColor).ToList();

            look.Colors.Clear();
            int maxColors = new[] { requested.Count, fallback.Count, baseColors.Count }.Max();

            for (int index = 1; index <= maxColors; index++)
            {
                bool hasRequested = index - 1 < requested.Count;
                int requestedValue = hasRequested ? requested[index - 1] : int.MinValue;
                int fallbackValue = index - 1 < fallback.Count ? fallback[index - 1] : -1;
                int baseValue = index - 1 < baseColors.Count ? baseColors[index - 1] : 0;

                int resolved;
                if (hasRequested)
                    resolved = requestedValue == -1 ? (baseValue != -1 ? baseValue : fallbackValue) : requestedValue;
                else
                    resolved = fallbackValue != -1 ? fallbackValue : baseValue;

                if (resolved == -1 || resolved == int.MinValue)
                    continue;

                if ((resolved & unchecked((int)0xFF000000)) == 0)
                    resolved = unchecked((int)0xFF000000) | (resolved & 0xFFFFFF);

                look.AddColor(index, System.Drawing.Color.FromArgb(resolved));
            }
        }

        private static void SetPrimarySkin(ActorLook look, short cosmeticId)
        {
            if (look == null || cosmeticId <= 0)
                return;

            if (look.Skins.Count == 0)
                look.AddSkin(cosmeticId);
            else
                look.Skins[0] = cosmeticId;
        }

        private static int ExtractCharacterCosmeticId(Character character)
        {
            if (character == null)
                return 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(character.Record?.EntityLook))
                {
                    var storedLook = EntityManager.Instance.GetActorLook(character.Record.EntityLook);
                    if (storedLook != null && storedLook.Skins != null && storedLook.Skins.Count > 0 && storedLook.Skins[0] > 0)
                        return storedLook.Skins[0];
                }
            }
            catch
            {
            }

            var baseLook = GetBaseCharacterLook(character.Breed, character.Sex);
            return baseLook != null && baseLook.Skins != null && baseLook.Skins.Count > 0 ? baseLook.Skins[0] : 1;
        }

        private static readonly sbyte[] AutoLearnJobIds = new sbyte[]
        {
            2, 11, 13, 14, 15, 16, 17, 18, 19, 20, 24, 25, 26, 27, 28, 31, 36, 41, 43, 44, 45, 46, 47, 48, 49, 50, 56, 58, 60, 62, 63, 64, 65
        };

        private static void EnsureAutoLearnJobs(Character character)
        {
            if (character == null || character.Jobs == null)
                return;

            foreach (var jobId in AutoLearnJobIds)
                character.Jobs.AddJob(jobId);
        }

        private static void SyncAccountTokens(WorldClient client, Character character)
        {
            if (client == null || client.Account == null)
                return;

            var latestAccount = AccountManager.Instance.GetAccountById(client.Account.Id);
            if (latestAccount != null)
            {
                client.Account.Tokens = Math.Max(0, latestAccount.Tokens);
                client.Account.NewTokens = Math.Max(0, latestAccount.NewTokens);
            }

            if (character != null && character.Account != null)
            {
                character.Account.Tokens = client.Account.Tokens;
                character.Account.NewTokens = client.Account.NewTokens;
            }
        }

        public static void CommonCharacterSelection(WorldClient client, Character character, bool isTuto = false)
        {
            if (character != null && character.Client != null && character.Client != client)
                character.Client.Disconnect();

            client.Character = character;
            client.Character.Client = client;
            if (client.Character != null)
            {
                SyncAccountTokens(client, client.Character);
                // EnsureAutoLearnJobs disabled for P0 — blocks NPC profession learning validation. See P1 config flag.
                // EnsureAutoLearnJobs(client.Character);
                int receivedTokens = client.Character.Inventory != null ? client.Character.Inventory.MergePendingTokens(false) : 0;
                client.Send(new CharacterSelectedSuccessMessage(client.Character.GetCharacterBaseInformations()));
                InventoryHandler.SendInventoryContentMessage(client);
                if (receivedTokens > 0)
                    client.Character.SendServerMessage($"Vous avez reçu {receivedTokens} Pièce(s) de Kama Géante(s).");
                ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.SPELL);
                ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
                ContextRoleplayHandler.SendEmoteListMessage(client);
                ChatHandler.SendEnabledChannelsMessage(client);
                PvPHandler.SendAlignmentRankUpdateMessage(client);
                PvPHandler.SendAlignmentSubAreasListMessage(client);
                InventoryHandler.SendSpellListMessage(client, true);
                InventoryHandler.SendInventoryWeightMessage(client);
                CharacterHandler.SendCharacterStatsListMessage(client);
                if (client.Character.GuildMember != null)
                    GuildHandler.SendGuildMembershipMessage(client, client.Character.GuildMember);
                if (client.Character.EquippedMount != null)
                {
                    var mount = client.Character.EquippedMount;

                    client.Send(new MountSetMessage(mount.GetClientData()));

                    int xp = mount.Record.GivenExperience;
                    xp = Math.Max(0, xp);
                    xp = Math.Min(90, xp); // sécurité protocole

                    client.Send(new MountXpRatioMessage((sbyte)xp));
                    client.Send(new MountRidingMessage(client.Character.IsRiding));
                }
                client.Character.EnterWorld();
                DungeonPartyFinderHandler.RefreshPlayerDungeonFinderData(client);
            }
            else
                client.Send(new CharacterSelectedErrorMessage());
        }

        public static void CommonCharacterSelection(WorldClient client, int characterId)
        {
            if (!CharacterManager.Instance.Characters.TryGetValue(characterId, out var selectedCharacter) || selectedCharacter == null)
            {
                client.Send(new CharacterSelectedErrorMessage());
                return;
            }
            if (selectedCharacter != null && selectedCharacter.Client != null && selectedCharacter.Client != client)
                selectedCharacter.Client.Disconnect();

            client.Character = selectedCharacter;
            client.Character.Client = client;
            if (client.Character != null)
            {
                SyncAccountTokens(client, client.Character);
                // EnsureAutoLearnJobs disabled for P0 — blocks NPC profession learning validation. See P1 config flag.
                // EnsureAutoLearnJobs(client.Character);
                int receivedTokens = client.Character.Inventory != null ? client.Character.Inventory.MergePendingTokens(false) : 0;
                client.Send(new CharacterSelectedSuccessMessage(client.Character.GetCharacterBaseInformations()));
                InventoryHandler.SendInventoryContentMessage(client);
                if (receivedTokens > 0)
                    client.Character.SendServerMessage($"Vous avez reçu {receivedTokens} Pièce(s) de Kama Géante(s).");
                ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.SPELL);
                ShortcutHandler.SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
                ContextRoleplayHandler.SendEmoteListMessage(client);
                ChatHandler.SendEnabledChannelsMessage(client);
                PvPHandler.SendAlignmentRankUpdateMessage(client);
                PvPHandler.SendAlignmentSubAreasListMessage(client);
                InventoryHandler.SendSpellListMessage(client, true);
                InventoryHandler.SendInventoryWeightMessage(client);
                CharacterHandler.SendCharacterStatsListMessage(client);
                if (client.Character.GuildMember != null)
                    GuildHandler.SendGuildMembershipMessage(client, client.Character.GuildMember);
                if (client.Character.EquippedMount != null)
                {
                    var mount = client.Character.EquippedMount;

                    client.Send(new MountSetMessage(mount.GetClientData()));

                    int xp = mount.Record.GivenExperience;
                    xp = Math.Max(0, xp);
                    xp = Math.Min(90, xp); // sécurité protocole

                    client.Send(new MountXpRatioMessage((sbyte)xp));
                    client.Send(new MountRidingMessage(client.Character.IsRiding));
                }
                client.Character.EnterWorld();
                DungeonPartyFinderHandler.RefreshPlayerDungeonFinderData(client);
            }
            else
                client.Send(new CharacterSelectedErrorMessage());
        }

        public static void SendCharacterStatsListMessage(WorldClient client)
        {
            client.Send(new CharacterStatsListMessage(client.Character.GetCharacterCharacteristicsInformations()));
        }

        public static void SendLifePointsRegenBeginMessage(WorldClient client, byte regenRate)
        {
            client.Send(new LifePointsRegenBeginMessage(regenRate));
        }

        public static void SendUpdateLifePointsMessage(WorldClient client)
        {
            client.Send(new UpdateLifePointsMessage(client.Character.Stats.Health.Total,
                client.Character.Stats.Health.TotalMax));
        }

        public static void SendLifePointsRegenEndMessage(WorldClient client, int lifePointsGained)
        {
            client.Send(new LifePointsRegenEndMessage(client.Character.Stats.Health.Total,
                client.Character.Stats.Health.TotalMax, lifePointsGained));
        }

        public static void SendCharacterLevelUpMessage(WorldClient client, byte newLevel)
        {
            client.Send(new CharacterLevelUpMessage(newLevel));

        }
        public static void SendCharacterLevelUpInformationMessage(WorldClient client, Character character, byte level)
        {
            client.Send(new CharacterLevelUpInformationMessage(level, character.Name, character.Id, 0));
        }
    }
}
