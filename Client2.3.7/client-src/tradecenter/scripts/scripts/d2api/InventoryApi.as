package d2api
{
   import d2data.ItemWrapper;
   
   public class InventoryApi
   {
      
      public function InventoryApi()
      {
         super();
      }
      
      [Trusted]
      public function destroy() : void
      {
      }
      
      [Untrusted]
      public function getStorageObjectGID(param1:uint, param2:uint = 1) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getItemQty(param1:uint) : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getItem(param1:uint) : ItemWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function getEquipementItemByPosition(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getEquipement() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getEquipementForPreset() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getCurrentWeapon() : ItemWrapper
      {
         return null;
      }
      
      [Untrusted]
      public function getPresets() : Object
      {
         return null;
      }
   }
}

