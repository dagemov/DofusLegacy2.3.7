package d2api
{
   import d2data.AbuseReasons;
   import d2data.AlignmentBalance;
   import d2data.AlignmentSide;
   import d2data.Area;
   import d2data.Breed;
   import d2data.ChatChannel;
   import d2data.Dungeon;
   import d2data.Effect;
   import d2data.House;
   import d2data.HouseWrapper;
   import d2data.InfoMessage;
   import d2data.Item;
   import d2data.ItemSet;
   import d2data.ItemWrapper;
   import d2data.MapPosition;
   import d2data.Monster;
   import d2data.Npc;
   import d2data.NpcAction;
   import d2data.Pack;
   import d2data.PresetIcon;
   import d2data.Quest;
   import d2data.QuestObjective;
   import d2data.QuestStep;
   import d2data.RankName;
   import d2data.Server;
   import d2data.ServerPopulation;
   import d2data.Skill;
   import d2data.Spell;
   import d2data.SpellBomb;
   import d2data.SpellLevel;
   import d2data.SpellState;
   import d2data.SpellType;
   import d2data.SubArea;
   import d2data.TaxCollectorFirstname;
   import d2data.TaxCollectorName;
   import d2data.WorldMap;
   import d2data.WorldPoint;
   
   public class DataApi
   {
      
      public function DataApi()
      {
         super();
      }
      
      [Trusted]
      public function destroy() : void
      {
      }
      
      [Untrusted]
      public function getNotifications() : Object
      {
         return null;
      }
      
      [Trusted]
      public function getServer(param1:int) : Server
      {
         return null;
      }
      
      [Trusted]
      public function getServerPopulation(param1:int) : ServerPopulation
      {
         return null;
      }
      
      [Untrusted]
      public function getBreed(param1:int) : Breed
      {
         return null;
      }
      
      [Untrusted]
      public function getBreeds() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSpell(param1:int) : Spell
      {
         return null;
      }
      
      [Untrusted]
      public function getSpellItem(param1:uint, param2:uint = 1) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSpellLevel(param1:int) : SpellLevel
      {
         return null;
      }
      
      [Untrusted]
      public function getSpellType(param1:int) : SpellType
      {
         return null;
      }
      
      [Untrusted]
      public function getSpellState(param1:int) : SpellState
      {
         return null;
      }
      
      [Untrusted]
      public function getChatChannel(param1:int) : ChatChannel
      {
         return null;
      }
      
      [Untrusted]
      public function getAllChatChannels() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSubArea(param1:int) : SubArea
      {
         return null;
      }
      
      [Untrusted]
      public function getSubAreaFromMap(param1:int) : SubArea
      {
         return null;
      }
      
      [Untrusted]
      public function getArea(param1:int) : Area
      {
         return null;
      }
      
      [Untrusted]
      public function getAllArea(param1:Boolean = false, param2:Boolean = false) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getWorldPoint(param1:int) : WorldPoint
      {
         return null;
      }
      
      [Untrusted]
      public function getItem(param1:int) : Item
      {
         return null;
      }
      
      [Untrusted]
      public function getItems() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getNewGenericSlotData() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getItemIconUri(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getItemName(param1:int) : String
      {
         return null;
      }
      
      [Untrusted]
      public function getItemType(param1:int) : String
      {
         return null;
      }
      
      [Untrusted]
      public function getItemSet(param1:int) : ItemSet
      {
         return null;
      }
      
      [Untrusted]
      public function getSetEffects(param1:Object, param2:Object = null) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getMonsterFromId(param1:uint) : Monster
      {
         return null;
      }
      
      [Untrusted]
      public function getMonsters() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getNpc(param1:uint) : Npc
      {
         return null;
      }
      
      [Untrusted]
      public function getNpcAction(param1:uint) : NpcAction
      {
         return null;
      }
      
      [Untrusted]
      public function getAlignmentSide(param1:uint) : AlignmentSide
      {
         return null;
      }
      
      [Untrusted]
      public function getAlignmentBalance(param1:uint) : AlignmentBalance
      {
         return null;
      }
      
      [Untrusted]
      public function getRankName(param1:uint) : RankName
      {
         return null;
      }
      
      [Untrusted]
      public function getAllRankNames() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getItemWrapper(param1:uint, param2:int = 0, param3:uint = 0, param4:uint = 0, param5:* = null) : ItemWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function getItemFromUId(param1:uint) : ItemWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function getSkill(param1:uint) : Skill
      {
         return null;
      }
      
      [Untrusted]
      public function getHouseSkills() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getInfoMessage(param1:uint) : InfoMessage
      {
         return null;
      }
      
      [Untrusted]
      public function getAllInfoMessages() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSmilies() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getTaxCollectorName(param1:uint) : TaxCollectorName
      {
         return null;
      }
      
      [Untrusted]
      public function getTaxCollectorFirstname(param1:uint) : TaxCollectorFirstname
      {
         return null;
      }
      
      [Untrusted]
      public function getEmblems() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getQuest(param1:int) : Quest
      {
         return null;
      }
      
      [Untrusted]
      public function getQuestObjective(param1:int) : QuestObjective
      {
         return null;
      }
      
      [Untrusted]
      public function getQuestStep(param1:int) : QuestStep
      {
         return null;
      }
      
      [Untrusted]
      public function getHouse(param1:int) : House
      {
         return null;
      }
      
      [Untrusted]
      public function getLivingObjectSkins(param1:ItemWrapper) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getAbuseReasonName(param1:uint) : AbuseReasons
      {
         return null;
      }
      
      [Untrusted]
      public function getAllAbuseReasons() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getPresetIcons() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getPresetIcon(param1:uint) : PresetIcon
      {
         return null;
      }
      
      [Untrusted]
      public function getDungeons() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getDungeon(param1:uint) : Dungeon
      {
         return null;
      }
      
      [Untrusted]
      public function getMapInfo(param1:uint) : MapPosition
      {
         return null;
      }
      
      [Untrusted]
      public function getWorldMap(param1:uint) : WorldMap
      {
         return null;
      }
      
      [Untrusted]
      public function getHousesInformations() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getHouseInformations(param1:uint) : HouseWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function getBomb(param1:uint) : SpellBomb
      {
         return null;
      }
      
      [Untrusted]
      public function getPack(param1:uint) : Pack
      {
         return null;
      }
      
      [Untrusted]
      public function getEffect(param1:uint) : Effect
      {
         return null;
      }
   }
}

