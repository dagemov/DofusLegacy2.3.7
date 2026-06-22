package d2api
{
   import d2data.SubArea;
   import d2data.WeaponWrapper;
   import d2data.WorldPointWrapper;
   import d2network.ActorRestrictionsInformations;
   
   public class PlayedCharacterApi
   {
      
      public function PlayedCharacterApi()
      {
         super();
      }
      
      [Untrusted]
      public function characteristics() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getPlayedCharacterInfo() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getInventory() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getEquipment() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSpellInventory() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobs() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getMount() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function inventoryWeight() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function inventoryWeightMax() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function isIncarnation() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isInHouse() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isInExchange() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isInFight() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isInPreFight() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isInParty() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isPartyLeader() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isRidding() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function id() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function restrictions() : ActorRestrictionsInformations
      {
         return null;
      }
      
      [Untrusted]
      public function isMutant() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function publicMode() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function artworkId() : int
      {
         return 0;
      }
      
      [Untrusted]
      public function getBone() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getSkin() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getColors() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSubentityColors() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getAlignmentSide() : int
      {
         return 0;
      }
      
      [Untrusted]
      public function getAlignmentValue() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getAlignmentGrade() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getMaxSummonedCreature() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getCurrentSummonedCreature() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function canSummon() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function getSpell(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function canCastThisSpell(param1:uint, param2:uint) : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function canCastThisSpellOnTarget(param1:uint, param2:uint, param3:int) : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function getSpellModification(param1:uint, param2:int) : int
      {
         return 0;
      }
      
      [Untrusted]
      public function isInHisHouse() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function currentMap() : WorldPointWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function currentSubArea() : SubArea
      {
         return null;
      }
      
      [Untrusted]
      public function state() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function isAlive() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isFollowingPlayer() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function getFollowingPlayerId() : int
      {
         return 0;
      }
      
      [Untrusted]
      public function getPlayerSet(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getWeapon() : WeaponWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function getExperienceBonusPercent() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function knowSpell(param1:uint) : int
      {
         return 0;
      }
   }
}

