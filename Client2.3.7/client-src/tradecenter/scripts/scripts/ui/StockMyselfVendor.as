package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.SoundEnum;
   import com.ankamagames.dofusModuleLibrary.enum.SoundTypeEnum;
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.GridSelectMethodEnum;
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.UIEnum;
   import d2actions.CloseInventory;
   import d2actions.ExchangeShopStockModifyObject;
   import d2actions.ExchangeShopStockMouvmentRemove;
   import d2actions.LeaveDialogRequest;
   import d2api.ContextMenuApi;
   import d2api.DataApi;
   import d2api.SoundApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2api.UtilApi;
   import d2hooks.ClickItemInventory;
   import d2hooks.ClickItemShopHV;
   import d2hooks.CloseHumanVendor;
   import d2hooks.ExchangeShopStockAddQuantity;
   import d2hooks.ExchangeShopStockMovementRemoved;
   import d2hooks.ExchangeShopStockRemoveQuantity;
   import d2hooks.ExchangeShopStockUpdate;
   import flash.ui.Keyboard;
   import flash.utils.Dictionary;
   
   public class StockMyselfVendor
   {
      
      public static var MODE:String;
      
      public static const EQUIPEMENT_CATEGORY:uint = 0;
      
      public static const CONSUMABLES_CATEGORY:uint = 1;
      
      public static const RESSOURCES_CATEGORY:uint = 2;
      
      public static const ALL_CATEGORY:uint = uint.MAX_VALUE;
      
      public static const OTHER_CATEGORY:uint = 4;
      
      private static const SORT_ON_PRICE:String = "price";
      
      private static const SORT_ON_WEIGHT:String = "weight";
      
      private static const SORT_ON_QTY:String = "quantity";
      
      private static const SORT_ON_NAME:String = "name";
      
      private static const SORT_ON_DEFAULT:String = "objectUID";
      
      public static const STOCK:String = "stock";
      
      public static const HUMAN_VENDOR:String = "human_vendor";
      
      public var sysApi:SystemApi;
      
      public var uiApi:UiApi;
      
      public var dataApi:DataApi;
      
      public var utilApi:UtilApi;
      
      public var menuApi:ContextMenuApi;
      
      public var soundApi:SoundApi;
      
      [Module(name="Ankama_ContextMenu")]
      public var modContextMenu:Object;
      
      [Module(name="Ankama_Common")]
      public var modCommon:Object;
      
      public var btn_center:Object;
      
      public var centerCtr:Object;
      
      public var btn_lbl_btn_center:Object;
      
      public var gd_shop:Object;
      
      public var lbl_title:Object;
      
      public var cbFilter:Object;
      
      public var btnEquipable:Object;
      
      public var btnConsumables:Object;
      
      public var btnRessources:Object;
      
      public var btnAll:Object;
      
      public var btnSearch:Object;
      
      public var btnClose:Object;
      
      public var ctr_bottomInfos:Object;
      
      public var searchCtr:Object;
      
      public var searchInput:Object;
      
      protected var _searchCriteria:String;
      
      protected var _filterAssoc:Object = new Object();
      
      protected var _subFilterIndex:Object = new Object();
      
      protected var _shopStock:Object;
      
      protected var _category:Object;
      
      protected var _currentFilterBtn:Object;
      
      private var _item:Object;
      
      private var _objectToRemove:Object;
      
      private var _slotList:Dictionary = new Dictionary(true);
      
      public function StockMyselfVendor()
      {
         super();
      }
      
      public function main(param1:Object = null) : void
      {
         MODE = STOCK;
         this.btnSearch.soundId = SoundEnum.CHECKBOX_CHECKED;
         this.btnEquipable.soundId = SoundEnum.TAB;
         this.btnConsumables.soundId = SoundEnum.TAB;
         this.btnRessources.soundId = SoundEnum.TAB;
         this.btnAll.soundId = SoundEnum.TAB;
         this.sysApi.addHook(ExchangeShopStockUpdate,this.onExchangeShopStockUpdate);
         this.sysApi.addHook(ExchangeShopStockMovementRemoved,this.onExchangeShopStockMovementRemoved);
         this.sysApi.addHook(ClickItemInventory,this.onClickItemInventory);
         this.sysApi.addHook(ClickItemShopHV,this.onClickItemShopHV);
         this.sysApi.addHook(ExchangeShopStockAddQuantity,this.onExchangeShopStockAddQuantity);
         this.sysApi.addHook(ExchangeShopStockRemoveQuantity,this.onExchangeShopStockRemoveQuantity);
         this._currentFilterBtn = this.btnAll;
         this.btnAll.selected = true;
         this.ctr_bottomInfos.visible = false;
         this._filterAssoc[this.btnEquipable.name] = EQUIPEMENT_CATEGORY;
         this._filterAssoc[this.btnConsumables.name] = CONSUMABLES_CATEGORY;
         this._filterAssoc[this.btnRessources.name] = RESSOURCES_CATEGORY;
         this._filterAssoc[this.btnAll.name] = ALL_CATEGORY;
         this.gd_shop.autoSelect = false;
         this.gd_shop.dropValidator = this.dropValidatorFunction as Function;
         this.gd_shop.processDrop = this.processDropFunction as Function;
         this.gd_shop.removeDropSource = this.removeDropSourceFunction as Function;
         this.lbl_title.text = this.uiApi.getText("ui.common.shop");
         this.btn_lbl_btn_center.text = this.uiApi.getText("ui.humanVendor.switchToMerchantMode");
         this._shopStock = param1;
         this._category = new Array();
         this.updateStockInventory();
         this.btnAll.selected = true;
         this.sysApi.disableWorldInteraction();
      }
      
      public function updateItemLine(param1:*, param2:*, param3:Boolean) : void
      {
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc6_:uint = 0;
         param2.slot_item.allowDrag = false;
         param2.btn_item.removeDropSource = this.removeDropSourceFunction;
         param2.btn_item.processDrop = this.processDropFunction;
         param2.btn_item.dropValidator = this.dropValidatorFunction;
         if(!this._slotList[param2.slot_item.name])
         {
            this.uiApi.addComponentHook(param2.slot_item,"onRightClick");
            this.uiApi.addComponentHook(param2.slot_item,"onRollOut");
            this.uiApi.addComponentHook(param2.slot_item,"onRollOver");
         }
         this._slotList[param2.slot_item.name] = param1;
         if(param1)
         {
            param2.btn_item.selected = param3;
            _loc4_ = param1.itemWrapper;
            _loc5_ = this.dataApi.getItem(_loc4_.objectGID);
            if(isNaN(Number(param1.price)) || param1.price == null || param1.price == 0)
            {
               param2.lbl_ItemPrice.text = "";
            }
            else
            {
               param2.lbl_ItemPrice.text = this.utilApi.kamasToString(param1.price);
            }
            _loc6_ = param2.lbl_ItemPrice.x + param2.lbl_ItemPrice.width - param2.lbl_ItemName.x - 10 - param2.lbl_ItemPrice.textfield.textWidth;
            param2.lbl_ItemName.width = _loc6_;
            param2.lbl_ItemName.text = _loc4_.name;
            param2.lbl_ItemPrice.text = this.utilApi.kamasToString(param1.price);
            param2.slot_item.data = _loc4_;
            param2.tx_backgroundItem.visible = true;
            if(_loc5_.etheral)
            {
               param2.lbl_ItemName.cssClass = "itemetheral";
            }
            else if(_loc5_.itemSetId != -1)
            {
               param2.lbl_ItemName.cssClass = "itemset";
            }
            else
            {
               param2.lbl_ItemName.cssClass = "p";
            }
         }
         else
         {
            param2.lbl_ItemName.text = "";
            param2.lbl_ItemPrice.text = "";
            param2.slot_item.data = null;
            param2.tx_backgroundItem.visible = false;
            param2.btn_item.selected = false;
         }
      }
      
      public function dropValidatorFunction(param1:Object, param2:Object, param3:Object) : Boolean
      {
         return true;
      }
      
      public function removeDropSourceFunction(param1:Object) : void
      {
      }
      
      public function processDropFunction(param1:Object, param2:Object, param3:Object) : void
      {
      }
      
      private function selectItem(param1:Object) : void
      {
         var _loc3_:Object = null;
         var _loc2_:uint = 0;
         for each(_loc3_ in this.gd_shop.dataProvider)
         {
            if(param1.objectUID == _loc3_.itemWrapper.objectUID)
            {
               this.gd_shop.selectedIndex = _loc2_;
               this.sysApi.dispatchHook(ClickItemShopHV,param1.itemWrapper,param1.price);
               this.gd_shop.dataProvider[_loc2_].select();
               return;
            }
            _loc2_++;
         }
      }
      
      protected function updateCombobox() : void
      {
         var _loc3_:Object = null;
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc6_:Object = null;
         var _loc7_:Object = null;
         var _loc8_:Object = null;
         var _loc1_:Object = new Array();
         var _loc2_:uint = uint(this._filterAssoc[this._currentFilterBtn.name]);
         for each(_loc3_ in this._shopStock)
         {
            _loc8_ = this.dataApi.getItem(_loc3_.itemWrapper.objectGID);
            if(_loc8_.category == _loc2_ || _loc2_ == ALL_CATEGORY)
            {
               _loc1_[_loc8_.typeId] = _loc8_.type;
            }
         }
         _loc4_ = new Array();
         for each(_loc7_ in _loc1_)
         {
            _loc6_ = {
               "label":_loc7_.name,
               "filterType":_loc7_.id
            };
            if(_loc7_.id == this._subFilterIndex[this._currentFilterBtn.name])
            {
               _loc5_ = _loc6_;
            }
            _loc4_.push(_loc6_);
         }
         _loc4_ = _loc4_.sort();
         _loc6_ = {
            "label":this.uiApi.getText("ui.common.allTypes"),
            "filterType":-1
         };
         if(!_loc5_)
         {
            _loc5_ = _loc6_;
         }
         _loc4_.unshift(_loc6_);
         this.cbFilter.dataProvider = _loc4_;
         this.cbFilter.value = _loc5_;
      }
      
      private function selectTab(param1:Object) : void
      {
         var _loc2_:uint = uint(this._filterAssoc[this._currentFilterBtn.name]);
         var _loc3_:Object = this.dataApi.getItem(param1.objectGID);
         if(_loc3_.category != _loc2_ && _loc2_ != ALL_CATEGORY)
         {
            switch(_loc3_.category)
            {
               case EQUIPEMENT_CATEGORY:
                  this._currentFilterBtn = this.btnEquipable;
                  this.btnEquipable.selected = true;
                  break;
               case CONSUMABLES_CATEGORY:
                  this._currentFilterBtn = this.btnConsumables;
                  this.btnConsumables.selected = true;
                  break;
               case RESSOURCES_CATEGORY:
                  this._currentFilterBtn = this.btnRessources;
                  this.btnRessources.selected = true;
                  break;
               default:
                  this._currentFilterBtn = this.btnAll;
                  this.btnAll.selected = true;
            }
         }
         this.updateStockInventory();
      }
      
      protected function updateStockInventory() : void
      {
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc1_:uint = uint(this._filterAssoc[this._currentFilterBtn.name]);
         this.updateCombobox();
         var _loc2_:Object = new Array();
         var _loc3_:Object = new Array();
         for each(_loc4_ in this._shopStock)
         {
            _loc5_ = this.dataApi.getItem(_loc4_.itemWrapper.objectGID);
            if((_loc5_.category == _loc1_ || _loc1_ == ALL_CATEGORY) && (!this.cbFilter.value || this.cbFilter.value.filterType == -1 || this.cbFilter.value.filterType == _loc5_.typeId) && (!this._searchCriteria || _loc5_.name.toLowerCase().indexOf(this._searchCriteria.toLowerCase()) != -1))
            {
               _loc3_[_loc5_.typeId] = _loc5_.type;
               _loc2_.push(_loc4_);
            }
         }
         this.gd_shop.dataProvider = _loc2_;
      }
      
      protected function onExchangeShopStockUpdate(param1:Object, param2:Object = null) : void
      {
         this._shopStock = param1;
         if(param2 != null)
         {
            this.selectTab(param2);
            this.soundApi.playSound(SoundTypeEnum.SWITCH_RIGHT_TO_LEFT);
         }
         else
         {
            this.showTransfertUI(false);
            this.updateStockInventory();
         }
      }
      
      public function onClickItemShopHV(param1:Object, param2:uint = 0) : void
      {
         this._item = param1;
      }
      
      public function onClickItemInventory(param1:Object) : void
      {
         this._item = param1;
      }
      
      public function onExchangeShopStockMovementRemoved(param1:uint) : void
      {
         if(this._item.objectUID == param1)
         {
            this._item = null;
            if(this.gd_shop.dataProvider.length > 0)
            {
               this.showTransfertUI(true);
               this.gd_shop.selectedIndex = 0;
               this.sysApi.dispatchHook(ClickItemShopHV,this.gd_shop.selectedItem.itemWrapper,this.gd_shop.selectedItem.price);
            }
            else
            {
               this.showTransfertUI(false);
            }
         }
      }
      
      public function onExchangeShopStockAddQuantity() : void
      {
         this.soundApi.playSound(SoundTypeEnum.SWITCH_RIGHT_TO_LEFT);
      }
      
      public function onExchangeShopStockRemoveQuantity() : void
      {
         this.soundApi.playSound(SoundTypeEnum.SWITCH_LEFT_TO_RIGHT);
      }
      
      private function showTransfertUI(param1:Boolean = true) : void
      {
      }
      
      public function onSelectItem(param1:Object, param2:uint, param3:Boolean) : void
      {
         var _loc4_:Object = null;
         var _loc5_:* = undefined;
         switch(param1)
         {
            case this.gd_shop:
               _loc4_ = this.gd_shop.selectedItem;
               switch(param2)
               {
                  case GridSelectMethodEnum.CLICK:
                     this.sysApi.dispatchHook(ClickItemShopHV,_loc4_.itemWrapper,_loc4_.price);
                     this.showTransfertUI(true);
                     break;
                  case GridSelectMethodEnum.DOUBLE_CLICK:
                     this.sysApi.sendAction(new ExchangeShopStockModifyObject(_loc4_.itemWrapper.objectUID,-1,_loc4_.price));
                     break;
                  case GridSelectMethodEnum.CTRL_DOUBLE_CLICK:
                     this.sysApi.sendAction(new ExchangeShopStockModifyObject(_loc4_.itemWrapper.objectUID,-_loc4_.itemWrapper.quantity,_loc4_.price));
                     break;
                  case GridSelectMethodEnum.ALT_DOUBLE_CLICK:
                     this._objectToRemove = _loc4_;
                     this.modCommon.openQuantityPopup(1,_loc4_.itemWrapper.quantity,_loc4_.itemWrapper.quantity,this.onValidQty);
               }
               break;
            case this.cbFilter:
               if(param3 && param2 != 2)
               {
                  _loc5_ = param1.value;
                  this._subFilterIndex[this._currentFilterBtn.name] = param1.value.filterType;
                  this.updateStockInventory();
               }
         }
      }
      
      private function onValidQty(param1:Number) : void
      {
         this.sysApi.sendAction(new ExchangeShopStockModifyObject(this._objectToRemove.itemWrapper.objectUID,-param1,this._objectToRemove.price));
      }
      
      public function onRelease(param1:Object) : void
      {
         var _loc2_:Object = null;
         var _loc3_:Boolean = false;
         var _loc4_:Boolean = false;
         switch(param1)
         {
            case this.btnEquipable:
            case this.btnConsumables:
            case this.btnRessources:
            case this.btnAll:
               this._currentFilterBtn = param1;
               this.updateStockInventory();
               break;
            case this.btnSearch:
               this.searchCtr.visible = !this.searchCtr.visible;
               this.cbFilter.visible = !this.searchCtr.visible;
               if(this.searchCtr.visible)
               {
                  this._searchCriteria = this.searchInput.text;
                  this.searchInput.focus();
               }
               else
               {
                  this._searchCriteria = null;
               }
               this.updateStockInventory();
               break;
            case this.gd_shop:
               _loc2_ = this.gd_shop.selectedItem;
               _loc3_ = this.uiApi.keyIsDown(Keyboard.CONTROL);
               _loc4_ = this.uiApi.keyIsDown(Keyboard.SHIFT);
               if(_loc3_ && _loc4_)
               {
                  this.sysApi.sendAction(new ExchangeShopStockMouvmentRemove(_loc2_.objectUID,_loc2_.quantity));
               }
               break;
            case this.btnClose:
               this.sysApi.dispatchHook(CloseHumanVendor);
               break;
            case this.btn_center:
               break;
         }
      }
      
      public function onRollOver(param1:Object) : void
      {
         var _loc2_:String = null;
         var _loc4_:Object = null;
         var _loc3_:Object = {
            "point":10,
            "relativePoint":1
         };
         switch(param1)
         {
            case this.btnEquipable:
               _loc2_ = this.uiApi.getText("ui.common.equipement");
               break;
            case this.btnConsumables:
               _loc2_ = this.uiApi.getText("ui.common.misc");
               break;
            case this.btnRessources:
               _loc2_ = this.uiApi.getText("ui.common.ressources");
               break;
            case this.btnAll:
               _loc2_ = this.uiApi.getText("ui.common.all");
               break;
            default:
               if(param1.name.indexOf("slot_item") != -1)
               {
                  if(this.sysApi.getOption("displayTooltips","dofus"))
                  {
                     _loc4_ = this.sysApi.getData("itemTooltipSettings",true);
                     if(_loc4_ == null)
                     {
                        _loc4_ = new ItemTooltipSettings();
                        this.sysApi.setData("itemTooltipSettings",_loc4_,true);
                     }
                     if(this._slotList[param1.name])
                     {
                        this.uiApi.showTooltip(this._slotList[param1.name].itemWrapper,param1,false,"standard",3,3,0,null,null,_loc4_);
                     }
                  }
               }
         }
         if(_loc2_)
         {
            this.uiApi.showTooltip(this.uiApi.textTooltipInfo(_loc2_),param1,false,"standard",_loc3_.point,_loc3_.relativePoint,3,null,null,null,"TextInfo");
         }
      }
      
      public function onDoubleClick(param1:Object) : void
      {
      }
      
      public function onRollOut(param1:Object) : void
      {
         this.uiApi.hideTooltip();
      }
      
      public function onRightClick(param1:Object) : void
      {
         var _loc2_:Object = null;
         var _loc3_:Object = null;
         if(param1.name.indexOf("slot_item") != -1)
         {
            _loc2_ = param1.data;
            _loc3_ = this.menuApi.create(_loc2_);
            if(_loc3_.content.length > 0)
            {
               this.modContextMenu.createContextMenu(_loc3_);
            }
         }
      }
      
      public function unload() : void
      {
         this.uiApi.unloadUi(UIEnum.MYSELF_VENDOR);
         this.sysApi.enableWorldInteraction();
         this.sysApi.sendAction(new LeaveDialogRequest());
         this.sysApi.sendAction(new CloseInventory());
      }
   }
}

class ItemTooltipSettings
{
   
   public var header:Boolean = true;
   
   public var effects:Boolean = true;
   
   public var conditions:Boolean = true;
   
   public var description:Boolean = true;
   
   public function ItemTooltipSettings()
   {
      super();
   }
}
