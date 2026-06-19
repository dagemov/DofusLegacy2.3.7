package d2data
{
   import d2network.GuildEmblem;
   
   public class GuildWrapper
   {
      
      public function GuildWrapper()
      {
         super();
      }
      
      public function get guildId() : uint
      {
         return new uint();
      }
      
      public function set guildId(param1:uint) : void
      {
      }
      
      public function get upEmblem() : EmblemWrapper
      {
         return new EmblemWrapper();
      }
      
      public function set upEmblem(param1:EmblemWrapper) : void
      {
      }
      
      public function get backEmblem() : EmblemWrapper
      {
         return new EmblemWrapper();
      }
      
      public function set backEmblem(param1:EmblemWrapper) : void
      {
      }
      
      public function get level() : uint
      {
         return new uint();
      }
      
      public function set level(param1:uint) : void
      {
      }
      
      public function get experience() : uint
      {
         return new uint();
      }
      
      public function set experience(param1:uint) : void
      {
      }
      
      public function get expLevelFloor() : uint
      {
         return new uint();
      }
      
      public function set expLevelFloor(param1:uint) : void
      {
      }
      
      public function get expNextLevelFloor() : uint
      {
         return new uint();
      }
      
      public function set expNextLevelFloor(param1:uint) : void
      {
      }
      
      public function get guildName() : String
      {
         return null;
      }
      
      public function get realGuildName() : String
      {
         return null;
      }
      
      public function set memberRightsNumber(param1:uint) : void
      {
      }
      
      public function get memberRightsNumber() : uint
      {
         return 0;
      }
      
      public function get memberRights() : Object
      {
         return null;
      }
      
      public function get isBoss() : Boolean
      {
         return false;
      }
      
      public function get manageGuildBoosts() : Boolean
      {
         return false;
      }
      
      public function get manageRights() : Boolean
      {
         return false;
      }
      
      public function get inviteNewMembers() : Boolean
      {
         return false;
      }
      
      public function get banMembers() : Boolean
      {
         return false;
      }
      
      public function get manageXPContribution() : Boolean
      {
         return false;
      }
      
      public function get manageRanks() : Boolean
      {
         return false;
      }
      
      public function get hireTaxCollector() : Boolean
      {
         return false;
      }
      
      public function get manageMyXpContribution() : Boolean
      {
         return false;
      }
      
      public function get collect() : Boolean
      {
         return false;
      }
      
      public function get useFarms() : Boolean
      {
         return false;
      }
      
      public function get organizeFarms() : Boolean
      {
         return false;
      }
      
      public function get takeOthersRidesInFarm() : Boolean
      {
         return false;
      }
      
      public function get prioritizeMeInDefense() : Boolean
      {
         return false;
      }
      
      public function get collectMyTaxCollectors() : Boolean
      {
         return false;
      }
      
      public function update(param1:uint, param2:String, param3:GuildEmblem, param4:Number) : void
      {
      }
      
      public function hasRight(param1:String) : Boolean
      {
         return false;
      }
   }
}

