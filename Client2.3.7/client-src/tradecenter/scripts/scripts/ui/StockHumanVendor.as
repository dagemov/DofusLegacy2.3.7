package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.SoundEnum;
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.UIEnum;
   import d2actions.CloseInventory;
   import d2actions.LeaveDialogRequest;
   import d2hooks.ExchangeShopStockUpdate;
   import d2hooks.KeyUp;
   
   public class StockHumanVendor extends StockMyselfVendor
   {
      
      public function StockHumanVendor()
      {
         super();
      }
      
      override public function main(param1:Object = null) : void
      {
         MODE = HUMAN_VENDOR;
         btnSearch.soundId = SoundEnum.CHECKBOX_CHECKED;
         btnEquipable.soundId = SoundEnum.TAB;
         btnConsumables.soundId = SoundEnum.TAB;
         btnRessources.soundId = SoundEnum.TAB;
         btnAll.soundId = SoundEnum.TAB;
         sysApi.addHook(ExchangeShopStockUpdate,onExchangeShopStockUpdate);
         sysApi.addHook(KeyUp,this.onKeyUp);
         gd_shop.scrollDisplay = "always";
         gd_shop.autoSelect = false;
         btnAll.selected = true;
         _currentFilterBtn = btnAll;
         _filterAssoc[btnEquipable.name] = EQUIPEMENT_CATEGORY;
         _filterAssoc[btnConsumables.name] = CONSUMABLES_CATEGORY;
         _filterAssoc[btnRessources.name] = RESSOURCES_CATEGORY;
         _filterAssoc[btnAll.name] = ALL_CATEGORY;
         centerCtr.visible = false;
         ctr_bottomInfos.visible = false;
         lbl_title.text = param1.playerName;
         _shopStock = param1.objects;
         _category = new Array();
         updateStockInventory();
         btnAll.selected = true;
      }
      
      override public function unload() : void
      {
         uiApi.unloadUi(UIEnum.HUMAN_VENDOR);
         sysApi.sendAction(new LeaveDialogRequest());
         sysApi.sendAction(new CloseInventory());
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         if(searchCtr.visible)
         {
            _searchCriteria = searchInput.text;
            updateStockInventory();
         }
      }
   }
}

