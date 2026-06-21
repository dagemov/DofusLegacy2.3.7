package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.SoundEnum;
   import com.ankamagames.dofusModuleLibrary.enum.SoundTypeEnum;
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.UIEnum;
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.tooltip.LocationEnum;
   import d2actions.CloseInventory;
   import d2actions.ExchangeShopStockMouvmentRemove;
   import d2actions.LeaveDialogRequest;
   import d2actions.NpcGenericActionRequest;
   import d2api.ContextMenuApi;
   import d2api.DataApi;
   import d2api.SoundApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2api.UtilApi;
   import d2components.ButtonContainer;
   import d2components.ComboBox;
   import d2components.EntityDisplayer;
   import d2components.GraphicContainer;
   import d2components.Grid;
   import d2components.Input;
   import d2components.Label;
   import d2data.ItemWrapper;
   import d2hooks.ClickItemStore;
   import d2hooks.CloseStore;
   import d2hooks.KeyUp;
   import flash.ui.Keyboard;
   import flash.utils.Dictionary;
   
   public class StockNpcStore
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
      
      private static const VIRTUAL_SHOP_MIN_ID:int = 9000;
      
      private static const VIRTUAL_SHOP_MAX_ID:int = 9042;
      
      private static const ACTION_BUY_SELL:int = 1;
      
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
      
      public var gd_shop:Grid;
      
      public var lbl_title:Label;
      
      public var cbFilter:ComboBox;
      
      public var cbNpc:ComboBox;
      
      public var btnEquipable:ButtonContainer;
      
      public var btnConsumables:ButtonContainer;
      
      public var btnRessources:ButtonContainer;
      
      public var btnAll:ButtonContainer;
      
      public var btnSearch:ButtonContainer;
      
      public var btnClose:ButtonContainer;
      
      public var searchCtr:GraphicContainer;
      
      public var searchInput:Input;
      
      public var merchantLook:EntityDisplayer;
      
      public var centerCtr:GraphicContainer;
      
      public var ctr_bottomInfos:GraphicContainer;
      
      protected var _searchCriteria:String;
      
      protected var _filterAssoc:Object = new Object();
      
      protected var _subFilterIndex:Object = new Object();
      
      protected var _shopStock:Object;
      
      protected var _category:Object;
      
      protected var _currentFilterBtn:Object;
      
      private var _waitingObject:Object;
      
      private var _slotList:Dictionary = new Dictionary(true);
      
      private var _currentNpcId:int;
      
      private var _virtualShopMode:Boolean;
      
      private var _suppressNpcSelect:Boolean;
      
      public function StockNpcStore()
      {
         super();
      }
      
      public function main(param1:Object) : void
      {
         this.uiApi.loadUi(UIEnum.NPC_ITEM,UIEnum.NPC_ITEM);
         this.btnEquipable.soundId = SoundEnum.TAB;
         this.btnConsumables.soundId = SoundEnum.TAB;
         this.btnRessources.soundId = SoundEnum.TAB;
         this.btnAll.soundId = SoundEnum.TAB;
         this.uiApi.addShortcutHook("closeUi",this.onShortCut);
         this.sysApi.addHook(KeyUp,this.onKeyUp);
         this.uiApi.addComponentHook(this.btnSearch,"onRelease");
         this.uiApi.addComponentHook(this.btnSearch,"onRollOver");
         this.uiApi.addComponentHook(this.btnSearch,"onRollOut");
         this.uiApi.addComponentHook(this.searchInput,"onRollOver");
         this.uiApi.addComponentHook(this.searchInput,"onRollOut");
         this.centerCtr.visible = false;
         this.ctr_bottomInfos.visible = false;
         this.gd_shop.scrollDisplay = "always";
         this.gd_shop.autoSelect = false;
         this._currentFilterBtn = this.btnAll;
         this.btnAll.selected = true;
         this._filterAssoc[this.btnEquipable.name] = EQUIPEMENT_CATEGORY;
         this._filterAssoc[this.btnConsumables.name] = CONSUMABLES_CATEGORY;
         this._filterAssoc[this.btnRessources.name] = RESSOURCES_CATEGORY;
         this._filterAssoc[this.btnAll.name] = ALL_CATEGORY;
         this.lbl_title.text = this.uiApi.getText("ui.common.shop");
         this._currentNpcId = int(param1.NPCSellerId);
         this._virtualShopMode = this._currentNpcId >= VIRTUAL_SHOP_MIN_ID;
         if(this.cbNpc)
         {
            this.cbNpc.visible = this._virtualShopMode;
            if(this._virtualShopMode)
            {
               this.populateNpcCombo(this._currentNpcId);
            }
         }
         this._shopStock = param1.Objects;
         this._category = new Array();
         this.merchantLook.look = param1.Look;
         this.updateStockInventory();
      }
      
      public function refreshShop(param1:int, param2:Object, param3:Object) : void
      {
         this._currentNpcId = param1;
         this._shopStock = param2;
         this.merchantLook.look = param3;
         this._searchCriteria = null;
         this._currentFilterBtn = this.btnAll;
         this.btnAll.selected = true;
         this.btnEquipable.selected = false;
         this.btnConsumables.selected = false;
         this.btnRessources.selected = false;
         if(this.searchCtr.visible)
         {
            this.searchCtr.visible = false;
            this.cbFilter.visible = true;
            this.searchInput.text = "";
         }
         if(this._virtualShopMode && this.cbNpc)
         {
            this.populateNpcCombo(param1);
         }
         this.updateStockInventory();
         if(this.gd_shop.dataProvider.length > 0)
         {
            this.gd_shop.selectedIndex = -1;
         }
      }
      
      protected function populateNpcCombo(param1:int) : void
      {
         var _loc2_:Array = new Array();
         var _loc3_:Object = null;
         var _loc4_:int = 0;
         var _loc5_:Object = null;
         var _loc6_:Object = null;
         for(_loc4_ = VIRTUAL_SHOP_MIN_ID + 1; _loc4_ <= VIRTUAL_SHOP_MAX_ID; _loc4_++)
         {
            _loc5_ = this.dataApi.getNpc(uint(_loc4_));
            if(!_loc5_ || !_loc5_.name)
            {
               continue;
            }
            _loc6_ = {
               "label":_loc5_.name,
               "npcId":_loc4_
            };
            _loc2_.push(_loc6_);
            if(_loc4_ == param1)
            {
               _loc3_ = _loc6_;
            }
         }
         _loc2_.sortOn("label",Array.CASEINSENSITIVE);
         this._suppressNpcSelect = true;
         this.cbNpc.dataProvider = _loc2_;
         if(_loc3_)
         {
            this.cbNpc.value = _loc3_;
         }
         else if(_loc2_.length > 0)
         {
            this.cbNpc.value = _loc2_[0];
         }
         this._suppressNpcSelect = false;
      }
      
      public function updateItemLine(param1:*, param2:*, param3:Boolean) : void
      {
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc6_:uint = 0;
         param2.slot_item.allowDrag = false;
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
            _loc4_ = param1;
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
      
      public function unload() : void
      {
         this.uiApi.unloadUi(UIEnum.NPC_ITEM);
         this.sysApi.sendAction(new LeaveDialogRequest());
         this.sysApi.sendAction(new CloseInventory());
         this.sysApi.enableWorldInteraction();
         this.uiApi.hideTooltip();
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
            _loc5_ = this.dataApi.getItem(_loc4_.objectGID);
            if((_loc4_.category == _loc1_ || _loc1_ == ALL_CATEGORY) && (!this.cbFilter.value || this.cbFilter.value.filterType == -1 || this.cbFilter.value.filterType == _loc5_.typeId) && (!this._searchCriteria || _loc5_.name.toLowerCase().indexOf(this._searchCriteria) != -1))
            {
               _loc3_[_loc5_.typeId] = _loc5_.type;
               _loc2_.push(_loc4_);
            }
         }
         this.gd_shop.dataProvider = _loc2_;
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
            _loc8_ = this.dataApi.getItem(_loc3_.objectGID);
            if(_loc3_.category == _loc2_ || _loc2_ == ALL_CATEGORY)
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
      
      protected function dropValidator(param1:Object, param2:Object, param3:Object) : Boolean
      {
         if(param2 == null)
         {
            return false;
         }
         if(param2 is ItemWrapper)
         {
            if(param2.position != 63)
            {
               return true;
            }
         }
         return false;
      }
      
      protected function processDrop(param1:Object, param2:Object, param3:Object) : void
      {
         if(Boolean(param2) && this.dropValidator(param1,param2,param3))
         {
         }
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
               if(this.gd_shop.dataProvider.length > 0)
               {
                  this.gd_shop.selectedIndex = -1;
               }
               break;
            case this.btnSearch:
               this.searchCtr.visible = !this.searchCtr.visible;
               this.cbFilter.visible = !this.searchCtr.visible;
               if(this.searchCtr.visible)
               {
                  this.searchInput.focus();
                  this._searchCriteria = this.searchInput.text.toLowerCase();
                  if(this._searchCriteria.length < 3)
                  {
                     this.gd_shop.dataProvider = new Array();
                  }
                  else
                  {
                     this.updateStockInventory();
                  }
               }
               else
               {
                  this._searchCriteria = null;
                  this.updateStockInventory();
               }
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
               this.sysApi.dispatchHook(CloseStore);
         }
      }
      
      public function onRollOver(param1:Object) : void
      {
         var _loc2_:String = null;
         var _loc5_:Object = null;
         var _loc3_:Object = {
            "point":LocationEnum.POINT_RIGHT,
            "relativePoint":LocationEnum.POINT_RIGHT
         };
         var _loc4_:int = 9;
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
            case this.btnSearch:
               _loc2_ = this.uiApi.getText("ui.common.sortOrSearch");
               _loc3_.point = LocationEnum.POINT_BOTTOM;
               _loc3_.relativePoint = LocationEnum.POINT_TOP;
               _loc4_ = 3;
               break;
            case this.searchInput:
               _loc2_ = this.uiApi.getText("ui.common.searchFilterTooltip");
               _loc3_.point = LocationEnum.POINT_BOTTOM;
               _loc3_.relativePoint = LocationEnum.POINT_TOP;
               _loc4_ = 3;
               break;
            default:
               if(param1.name.indexOf("slot_item") != -1)
               {
                  if(this.sysApi.getOption("displayTooltips","dofus"))
                  {
                     _loc5_ = this.sysApi.getData("itemTooltipSettings",true);
                     if(_loc5_ == null)
                     {
                        _loc5_ = new ItemTooltipSettings();
                        this.sysApi.setData("itemTooltipSettings",_loc5_,true);
                     }
                     this.uiApi.showTooltip(this._slotList[param1.name],param1,false,"standard",3,3,0,null,null,_loc5_);
                  }
               }
         }
         if(_loc2_)
         {
            this.uiApi.showTooltip(this.uiApi.textTooltipInfo(_loc2_),param1,false,"standard",_loc3_.point,_loc3_.relativePoint,_loc4_,null,null,null,"TextInfo");
         }
      }
      
      public function onRollOut(param1:Object) : void
      {
         this.uiApi.hideTooltip();
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
                  case 0:
                     this.sysApi.log(2,"select item shop");
                     this.sysApi.dispatchHook(ClickItemStore,_loc4_);
                     break;
                  case 1:
               }
               break;
            case this.cbFilter:
               if(param3 && param2 != 2)
               {
                  _loc5_ = param1.value;
                  this._subFilterIndex[this._currentFilterBtn.name] = param1.value.filterType;
                  this.updateStockInventory();
               }
               break;
            case this.cbNpc:
               if(this._suppressNpcSelect || !param3 || param2 == 2)
               {
                  break;
               }
               _loc5_ = param1.value;
               if(!_loc5_ || _loc5_.npcId == this._currentNpcId)
               {
                  break;
               }
               this.sysApi.sendAction(new NpcGenericActionRequest(int(_loc5_.npcId),ACTION_BUY_SELL));
         }
      }
      
      public function onItemRightClick(param1:Object, param2:Object) : void
      {
      }
      
      public function onItemUseOnCell(param1:Object) : void
      {
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
      
      public function onItemRollOver(param1:Object, param2:Object) : void
      {
         if(!param2.data)
         {
         }
      }
      
      public function onItemRollOut(param1:Object, param2:Object) : void
      {
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         if(this.searchCtr.visible && this.searchInput.haveFocus)
         {
            if(this.searchInput.text.length > 2)
            {
               this._searchCriteria = this.searchInput.text.toLowerCase();
               this.updateStockInventory();
            }
            else
            {
               if(this._searchCriteria)
               {
                  this._searchCriteria = null;
               }
               this.gd_shop.dataProvider = new Array();
            }
         }
      }
      
      private function showTransfertUI(param1:String, param2:Boolean = true) : void
      {
         if(param2 && !this.uiApi.getUi(param1))
         {
            this.soundApi.playSound(SoundTypeEnum.MERCHANT_TRANSFERT_OPEN);
         }
         if(!param2 && Boolean(this.uiApi.getUi(param1)))
         {
            this.soundApi.playSound(SoundTypeEnum.MERCHANT_TRANSFERT_CLOSE);
         }
      }
      
      private function onShortCut(param1:String) : Boolean
      {
         if(param1 == "closeUi")
         {
            this.sysApi.dispatchHook(CloseStore);
         }
         return false;
      }
      
      private function onCloseInventory() : void
      {
         this.sysApi.dispatchHook(CloseStore);
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
