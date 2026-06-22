package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.SoundEnum;
   import com.ankamagames.dofusModuleLibrary.enum.components.GridItemSelectMethodEnum;
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.UIEnum;
   import d2actions.BidHouseStringSearch;
   import d2actions.BidSwitchToBuyerMode;
   import d2actions.BidSwitchToSellerMode;
   import d2actions.CloseInventory;
   import d2actions.ExchangeBidHouseList;
   import d2actions.ExchangeBidHouseSearch;
   import d2actions.ExchangeBidHouseType;
   import d2actions.LeaveShopStock;
   import d2api.ContextMenuApi;
   import d2api.DataApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2api.UtilApi;
   import d2hooks.BidObjectTypeListUpdate;
   import d2hooks.KeyUp;
   import d2hooks.OpenBidHouse;
   import d2hooks.SellerObjectListUpdate;
   import flash.utils.Dictionary;
   
   public class StockBidHouse
   {
      
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
      
      public var sysApi:SystemApi;
      
      public var uiApi:UiApi;
      
      public var dataApi:DataApi;
      
      public var utilApi:UtilApi;
      
      public var menuApi:ContextMenuApi;
      
      [Module(name="Ankama_Common")]
      public var modCommon:Object;
      
      [Module(name="Ankama_ContextMenu")]
      public var modContextMenu:Object;
      
      public var searchCtr:Object;
      
      public var ctr_filters:Object;
      
      public var ctr_content:Object;
      
      public var ctr_bottomInfos:Object;
      
      public var lbl_title:Object;
      
      public var lbl_quantityObject:Object;
      
      public var searchInput:Object;
      
      public var btnSearch:Object;
      
      public var btnAll:Object;
      
      public var btnEquipable:Object;
      
      public var btnConsumables:Object;
      
      public var btnRessources:Object;
      
      public var btn_info:Object;
      
      public var btnClose:Object;
      
      public var gd_shop:Object;
      
      public var cbFilter:Object;
      
      public var tx_icon:Object;
      
      public var btn_center:Object;
      
      public var btn_lbl_btn_center:Object;
      
      private var _sellerBuyerDescriptor:Object;
      
      private var _currentTypeObject:int;
      
      protected var _searchCriteria:String;
      
      protected var _searchResult:Array;
      
      protected var _filterAssoc:Object = new Object();
      
      protected var _subFilterIndex:Object = new Object();
      
      protected var _itemsStock:Array;
      
      protected var _category:Array;
      
      protected var _currentFilterBtn:Object;
      
      private var _bidTooltipText:String = "";
      
      private var _totalObjectSold:uint;
      
      private var _slotList:Dictionary = new Dictionary(true);
      
      public function StockBidHouse()
      {
         super();
      }
      
      public function main(param1:Object) : void
      {
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         this.sysApi.dispatchHook(OpenBidHouse);
         this.btnAll.soundId = SoundEnum.TAB;
         this.btnConsumables.soundId = SoundEnum.TAB;
         this.btnEquipable.soundId = SoundEnum.TAB;
         this.btnRessources.soundId = SoundEnum.TAB;
         this._sellerBuyerDescriptor = param1.sellerBuyerDescriptor;
         this.sysApi.addHook(BidObjectTypeListUpdate,this.onBidObjectTypeListUpdate);
         this.sysApi.addHook(SellerObjectListUpdate,this.onSellerObjectListUpdate);
         this.sysApi.addHook(KeyUp,this.onKeyUp);
         this.uiApi.addShortcutHook("validUi",this.onShortcut);
         this.gd_shop.scrollDisplay = "always";
         this.gd_shop.autoSelect = false;
         this.ctr_filters.visible = false;
         this.ctr_content.y = -37;
         this.ctr_bottomInfos.visible = true;
         this._currentFilterBtn = this.btnAll;
         this._currentFilterBtn.selected = true;
         this._filterAssoc[this.btnEquipable.name] = EQUIPEMENT_CATEGORY;
         this._filterAssoc[this.btnConsumables.name] = CONSUMABLES_CATEGORY;
         this._filterAssoc[this.btnRessources.name] = RESSOURCES_CATEGORY;
         this._filterAssoc[this.btnAll.name] = ALL_CATEGORY;
         this._totalObjectSold = param1.objectsInfos ? uint(param1.objectsInfos.length) : 0;
         this.tx_icon.uri = this.uiApi.createUri(this.uiApi.me().getConstant("illus") + "Illus_marchands.swf|Marchand_tx_Illus_png");
         this.tx_icon.gotoAndStop = 8;
         this.changeBidTooltip(this._sellerBuyerDescriptor);
         this._itemsStock = new Array();
         this._category = new Array();
         if(param1.inventory != null)
         {
            _loc2_ = int(param1.inventory.length);
            _loc3_ = 0;
            while(_loc3_ < _loc2_)
            {
               this.addItemInStock(param1.inventory[_loc3_],false);
               _loc3_++;
            }
            this.updateStockInventory();
         }
         if(TradeCenter.BID_HOUSE_BUY_MODE)
         {
            this.btn_lbl_btn_center.text = this.uiApi.getText("ui.bidhouse.bigStoreModeSell");
         }
         else
         {
            this.btn_lbl_btn_center.text = this.uiApi.getText("ui.bidhouse.bigStoreModeBuy");
         }
         this.changeBidHouseMode(true);
      }
      
      public function updateItemLine(param1:*, param2:*, param3:Boolean) : void
      {
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc6_:uint = 0;
         param2.slot_item.allowDrag = false;
         if(param1)
         {
            if(!this._slotList[param2.slot_item.name])
            {
               this.uiApi.addComponentHook(param2.slot_item,"onRightClick");
               this.uiApi.addComponentHook(param2.slot_item,"onRollOut");
               this.uiApi.addComponentHook(param2.slot_item,"onRollOver");
            }
            this._slotList[param2.slot_item.name] = param1;
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
      
      public function changeBidTooltip(param1:Object) : void
      {
         var _loc2_:int = int(param1.types.length);
         var _loc3_:Array = new Array(_loc2_);
         var _loc4_:int = 0;
         while(_loc4_ < _loc2_)
         {
            _loc3_[_loc4_] = this.dataApi.getItemType(param1.types[_loc4_]);
            _loc4_++;
         }
         _loc3_.sort();
         var _loc5_:String = " - " + _loc3_.join("\n - ");
         this._bidTooltipText = this.uiApi.getText("ui.common.maxLevel") + " : " + param1.maxItemLevel + "\n" + this.uiApi.getText("ui.bidhouse.bigStoreTax") + " : " + param1.taxPercentage + "%" + "\n" + this.uiApi.getText("ui.bidhouse.bigStoreMaxItemPerAccount") + " : " + param1.maxItemPerAccount + "\n" + this.uiApi.getText("ui.bidhouse.bigStoreMaxSellTime") + " : " + param1.unsoldDelay + " " + this.uiApi.processText(this.uiApi.getText("ui.time.hours"),"n",param1.unsoldDelay < 2) + "\n\n" + this.uiApi.getText("ui.bidhouse.bigStoreTypes") + " : \n" + _loc5_;
      }
      
      public function changeBidHouseMode(param1:Boolean = false) : void
      {
         this.searchCtr.visible = false;
         this.cbFilter.visible = true;
         this._searchCriteria = null;
         this.searchInput.text = "";
         this.btnSearch.selected = false;
         if(TradeCenter.BID_HOUSE_BUY_MODE)
         {
            if(!param1)
            {
               this.uiApi.getUi("itemBidHouseSell").uiClass.displayUi(false);
               this.uiApi.getUi("itemBidHouseBuy").uiClass.displayUi(true);
            }
            this.btn_lbl_btn_center.text = this.uiApi.getText("ui.bidhouse.bigStoreModeSell");
            this.lbl_title.text = this.uiApi.getText("ui.bidhouse.bigStoreItemList");
            this.updateLabelQuantitySoldObject();
            this.comboBoxBuyMode();
            this.btnEquipable.disabled = true;
            this.btnConsumables.disabled = true;
            this.btnRessources.disabled = true;
            this.btnAll.disabled = true;
            this.gd_shop.dataProvider = new Array();
         }
         else
         {
            if(!param1)
            {
               this.uiApi.getUi("itemBidHouseSell").uiClass.displayUi(true);
               this.uiApi.getUi("itemBidHouseBuy").uiClass.displayUi(false);
            }
            this.gd_shop.dataProvider = new Array();
            this.btn_lbl_btn_center.text = this.uiApi.getText("ui.bidhouse.bigStoreModeBuy");
            this.lbl_title.text = this.uiApi.getText("ui.common.shopStock");
            this.updateLabelQuantitySoldObject();
            this.btnEquipable.disabled = false;
            this.btnConsumables.disabled = false;
            this.btnRessources.disabled = false;
            this.btnAll.disabled = false;
         }
      }
      
      protected function addItemInStock(param1:Object, param2:Boolean = true) : void
      {
         this._itemsStock.push(param1);
         var _loc3_:Object = this.dataApi.getItem(param1.itemWrapper.objectGID).category;
         this._category[param1.itemWrapper.objectUID] = _loc3_;
         if(param2)
         {
            this.selectTab(param1);
            this.updateStockInventory();
         }
      }
      
      private function comboBoxBuyMode() : void
      {
         var _loc4_:int = 0;
         var _loc1_:int = int(this._sellerBuyerDescriptor.types.length);
         var _loc2_:Array = new Array(_loc1_);
         var _loc3_:int = 0;
         while(_loc3_ < _loc1_)
         {
            _loc4_ = int(this._sellerBuyerDescriptor.types[_loc3_]);
            _loc2_[_loc3_] = {
               "label":this.dataApi.getItemType(_loc4_),
               "type":_loc4_
            };
            _loc3_++;
         }
         _loc2_ = _loc2_.sortOn("label");
         this.cbFilter.dataProvider = _loc2_;
         this.cbFilter.value = _loc2_[0];
      }
      
      protected function updateCombobox() : void
      {
         var _loc3_:Object = null;
         var _loc4_:Array = null;
         var _loc5_:Object = null;
         var _loc6_:Object = null;
         var _loc7_:Object = null;
         var _loc8_:Object = null;
         var _loc1_:Array = new Array();
         var _loc2_:uint = uint(this._filterAssoc[this._currentFilterBtn.name]);
         for each(_loc3_ in this._itemsStock)
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
      }
      
      protected function updateStockInventory() : void
      {
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc1_:uint = uint(this._filterAssoc[this._currentFilterBtn.name]);
         if(TradeCenter.BID_HOUSE_BUY_MODE)
         {
            this.comboBoxBuyMode();
         }
         else
         {
            this.updateCombobox();
         }
         var _loc2_:Array = new Array();
         var _loc3_:Array = new Array();
         for each(_loc4_ in this._itemsStock)
         {
            _loc5_ = this.dataApi.getItem(_loc4_.itemWrapper.objectGID);
            if((this._category[_loc4_.itemWrapper.objectUID] == _loc1_ || _loc1_ == ALL_CATEGORY) && (!this.cbFilter.value || this.cbFilter.value.filterType == -1 || this.cbFilter.value.filterType == _loc5_.typeId) && (!this._searchCriteria || _loc5_.name.toLowerCase().indexOf(this._searchCriteria) != -1))
            {
               _loc3_[_loc5_.typeId] = _loc5_.type;
               _loc2_.push(_loc4_);
            }
         }
         this.gd_shop.dataProvider = _loc2_;
      }
      
      protected function updateLabelQuantitySoldObject() : void
      {
         if(TradeCenter.BID_HOUSE_BUY_MODE)
         {
            this.lbl_quantityObject.visible = false;
         }
         else
         {
            this.lbl_quantityObject.visible = true;
            this.lbl_quantityObject.text = this._totalObjectSold + "/" + this._sellerBuyerDescriptor.maxItemPerAccount;
         }
      }
      
      public function onSellerObjectListUpdate(param1:Object) : void
      {
         this._totalObjectSold = param1.length;
         this.updateLabelQuantitySoldObject();
         this._itemsStock = new Array();
         this._category = new Array();
         var _loc2_:int = int(param1.length);
         var _loc3_:int = 0;
         while(_loc3_ < _loc2_)
         {
            this.addItemInStock(param1[_loc3_],false);
            _loc3_++;
         }
         this.updateStockInventory();
      }
      
      public function onBidObjectTypeListUpdate(param1:Object, param2:Boolean = false) : void
      {
         var _loc3_:int = 0;
         TradeCenter.SEARCH_MODE = param2;
         var _loc4_:int = int(param1.length);
         var _loc5_:Array = new Array(_loc4_);
         if(param2)
         {
            this._searchResult = new Array();
            _loc3_ = 0;
            while(_loc3_ < _loc4_)
            {
               _loc5_[_loc3_] = {"itemWrapper":this.dataApi.getItemWrapper(param1[_loc3_])};
               this._searchResult.push({"itemWrapper":this.dataApi.getItemWrapper(param1[_loc3_])});
               _loc3_++;
            }
         }
         else
         {
            _loc3_ = 0;
            while(_loc3_ < _loc4_)
            {
               _loc5_[_loc3_] = {"itemWrapper":this.dataApi.getItemWrapper(param1[_loc3_].GIDObject)};
               _loc3_++;
            }
         }
         _loc5_.sort(this.sortShop);
         if(Boolean(this._searchResult) && Boolean(this._searchResult.length))
         {
            this._searchResult.sort(this.sortShop);
         }
         this.gd_shop.dataProvider = _loc5_;
      }
      
      private function sortShop(param1:Object, param2:Object) : int
      {
         if(param1.itemWrapper.level < param2.itemWrapper.level)
         {
            return -1;
         }
         if(param1.itemWrapper.level > param2.itemWrapper.level)
         {
            return 1;
         }
         if(param1.itemWrapper.name < param2.itemWrapper.name)
         {
            return -1;
         }
         if(param1.itemWrapper.name > param2.itemWrapper.name)
         {
            return 1;
         }
         return 0;
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         if(this.searchCtr)
         {
            if(Boolean(this.searchCtr.visible) && Boolean(this.searchInput.haveFocus))
            {
               if(this.searchInput.text.length > 2)
               {
                  this._searchCriteria = this.searchInput.text.toLowerCase();
               }
               else
               {
                  if(this._searchCriteria)
                  {
                     this._searchCriteria = null;
                  }
                  this.gd_shop.dataProvider = new Array();
               }
               if(this._searchCriteria)
               {
                  if(TradeCenter.BID_HOUSE_BUY_MODE)
                  {
                     this.sysApi.sendAction(new BidHouseStringSearch(this._searchCriteria));
                  }
                  else
                  {
                     this.updateStockInventory();
                  }
               }
            }
         }
      }
      
      public function onSelectItem(param1:Object, param2:uint, param3:Boolean) : void
      {
         var _loc4_:Object = null;
         var _loc5_:* = undefined;
         switch(param1)
         {
            case this.gd_shop:
               if(TradeCenter.BID_HOUSE_BUY_MODE)
               {
                  if(param1)
                  {
                     _loc4_ = this.gd_shop.selectedItem.itemWrapper;
                     if(TradeCenter.SEARCH_MODE)
                     {
                        if(param2 != GridItemSelectMethodEnum.AUTO)
                        {
                           this.sysApi.sendAction(new ExchangeBidHouseSearch(this.dataApi.getItem(_loc4_.objectGID).typeId,_loc4_.objectGID));
                        }
                     }
                     else
                     {
                        this.sysApi.sendAction(new ExchangeBidHouseList(_loc4_.objectGID));
                     }
                  }
               }
               else
               {
                  this.uiApi.getUi("itemBidHouseSell").uiClass.onSelectItemFromStockBidHouse(this.gd_shop.selectedItem);
               }
               break;
            case this.cbFilter:
               if(TradeCenter.BID_HOUSE_BUY_MODE)
               {
                  if(param1.value.type != 0)
                  {
                     this._currentTypeObject = param1.value.type;
                  }
                  this.sysApi.sendAction(new ExchangeBidHouseType(this._currentTypeObject));
               }
               else if(param3 && param2 != 2)
               {
                  _loc5_ = param1.value;
                  this._subFilterIndex[this._currentFilterBtn.name] = param1.value.filterType;
                  this.updateStockInventory();
               }
         }
      }
      
      public function onRelease(param1:Object) : void
      {
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
               TradeCenter.SEARCH_MODE = this.searchCtr.visible;
               if(TradeCenter.SEARCH_MODE)
               {
                  this._searchCriteria = this.searchInput.text.toLowerCase();
                  this.searchInput.focus();
                  this.tx_icon.gotoAndStop = 9;
                  if(this._searchCriteria.length > 2)
                  {
                     if(TradeCenter.BID_HOUSE_BUY_MODE)
                     {
                        this.gd_shop.dataProvider = this._searchResult;
                     }
                     else
                     {
                        this.updateStockInventory();
                     }
                  }
                  else
                  {
                     this.gd_shop.dataProvider = new Array();
                  }
               }
               else
               {
                  this.tx_icon.gotoAndStop = 8;
                  this._searchCriteria = null;
                  this.updateStockInventory();
               }
               break;
            case this.btnClose:
               this.sysApi.sendAction(new LeaveShopStock());
               this.uiApi.unloadUi(this.uiApi.me().name);
               break;
            case this.btn_center:
               TradeCenter.SWITCH_MODE = true;
               TradeCenter.BID_HOUSE_BUY_MODE = !TradeCenter.BID_HOUSE_BUY_MODE;
               if(TradeCenter.BID_HOUSE_BUY_MODE)
               {
                  this.sysApi.sendAction(new BidSwitchToBuyerMode());
               }
               else
               {
                  this.sysApi.sendAction(new BidSwitchToSellerMode());
               }
         }
      }
      
      public function isSwitching() : Boolean
      {
         return TradeCenter.SWITCH_MODE;
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
               _loc2_ = this.uiApi.getText("ui.common.allTypes");
               break;
            case this.btn_info:
               this.uiApi.showTooltip(this.uiApi.textTooltipInfo(this._bidTooltipText),param1,false,"standard",_loc3_.point,_loc3_.relativePoint,3,null,null,null,"TextInfo");
               break;
            case this.lbl_quantityObject:
               _loc2_ = this.uiApi.getText("ui.bidhouse.quantityObjectSold",this._totalObjectSold,this._sellerBuyerDescriptor.maxItemPerAccount);
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
                     this.uiApi.showTooltip(this._slotList[param1.name].itemWrapper,param1,false,"standard",3,3,0,null,null,_loc4_);
                  }
               }
         }
         if(_loc2_)
         {
            this.uiApi.showTooltip(this.uiApi.textTooltipInfo(_loc2_),param1,false,"standard",_loc3_.point,_loc3_.relativePoint,3,null,null,null,"TextInfo");
         }
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
            _loc2_ = this._slotList[param1.name];
            _loc3_ = this.menuApi.create(_loc2_.itemWrapper);
            if(_loc3_.content.length > 0)
            {
               this.modContextMenu.createContextMenu(_loc3_);
            }
         }
      }
      
      public function onShortcut(param1:String) : Boolean
      {
         if(param1 == "validUi")
         {
            if(this.searchInput.haveFocus)
            {
               this.searchInput.focus();
               return true;
            }
         }
         return false;
      }
      
      public function unload() : void
      {
         this.uiApi.unloadUi(UIEnum.BIDHOUSE_BUY);
         this.uiApi.unloadUi(UIEnum.BIDHOUSE_SELL);
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
