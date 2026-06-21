package ui
{
   import d2actions.ExchangeBidHouseBuy;
   import d2actions.ExchangeBidHousePrice;
   import d2api.DataApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2api.UtilApi;
   import d2components.ButtonContainer;
   import d2components.GraphicContainer;
   import d2components.Grid;
   import d2components.Label;
   import d2hooks.BidObjectListUpdate;
   import d2hooks.ExchangeBidPrice;
   import d2hooks.ExchangeError;
   
   public class ItemBidHouseBuy
   {
      
      public var uiApi:UiApi;
      
      public var sysApi:SystemApi;
      
      public var dataApi:DataApi;
      
      public var utilApi:UtilApi;
      
      [Module(name="Ankama_Common")]
      public var modCommon:Object;
      
      private var _sellerBuyerDescriptor:Object;
      
      private var _itemName:String;
      
      private var _listObjects:Object;
      
      private var _currentSort:int;
      
      private var _item:Object;
      
      public var mainCtr:GraphicContainer;
      
      public var ctr_item:GraphicContainer;
      
      public var lbl_averagePrice:Label;
      
      public var lbl_averagePriceTitle:Label;
      
      public var lbl_selectItem:Label;
      
      public var lbl_tabQty1:Label;
      
      public var lbl_tabQty2:Label;
      
      public var lbl_tabQty3:Label;
      
      public var btn_tabQty1:ButtonContainer;
      
      public var btn_tabQty2:ButtonContainer;
      
      public var btn_tabQty3:ButtonContainer;
      
      public var btn_reset:ButtonContainer;
      
      public var btn_buy:ButtonContainer;
      
      public var gd_list:Grid;
      
      public function ItemBidHouseBuy()
      {
         super();
      }
      
      public function main(param1:Object) : void
      {
         this.sysApi.addHook(ExchangeBidPrice,this.onExchangeBidPrice);
         this.sysApi.addHook(BidObjectListUpdate,this.onBidObjectListUpdate);
         this.sysApi.addHook(ExchangeError,this.onExchangeError);
         this.uiApi.addShortcutHook("validUi",this.onShortcut);
         this.uiApi.addComponentHook(this.lbl_averagePriceTitle,"onRollOver");
         this.uiApi.addComponentHook(this.lbl_averagePriceTitle,"onRollOut");
         this.uiApi.addComponentHook(this.lbl_averagePrice,"onRollOver");
         this.uiApi.addComponentHook(this.lbl_averagePrice,"onRollOut");
         this.uiApi.addComponentHook(this.btn_reset,"onRollOver");
         this.uiApi.addComponentHook(this.btn_reset,"onRollOut");
         this.uiApi.addComponentHook(this.btn_reset,"onRelease");
         this.uiApi.addComponentHook(this.btn_tabQty1,"onRelease");
         this.uiApi.addComponentHook(this.btn_tabQty2,"onRelease");
         this.uiApi.addComponentHook(this.btn_tabQty3,"onRelease");
         this._sellerBuyerDescriptor = param1.sellerBuyerDescriptor;
         this.lbl_tabQty1.text = "x " + this._sellerBuyerDescriptor.quantities[0];
         this.lbl_tabQty2.text = "x " + this._sellerBuyerDescriptor.quantities[1];
         this.lbl_tabQty3.text = "x " + this._sellerBuyerDescriptor.quantities[2];
         this.lbl_selectItem.visible = false;
         this.gd_list.autoSelect = false;
         this.gd_list.dataProvider = new Array();
         this.btn_buy.disabled = true;
         this.mainCtr.visible = false;
      }
      
      public function displayUi(param1:Boolean) : void
      {
         if(param1)
         {
            this.gd_list.dataProvider = new Array();
            this.btn_buy.disabled = true;
         }
         else
         {
            this.mainCtr.visible = false;
         }
      }
      
      private function onBidObjectListUpdate(param1:Object, param2:int = 1, param3:Boolean = false) : void
      {
         var _loc6_:int = 0;
         var _loc7_:Array = null;
         var _loc8_:int = 0;
         var _loc9_:Object = null;
         var _loc10_:int = 0;
         var _loc11_:Object = null;
         var _loc12_:Object = null;
         var _loc4_:int = this.gd_list.selectedIndex;
         var _loc5_:int = -1;
         if(this.gd_list.selectedItem)
         {
            _loc5_ = int(this.gd_list.selectedItem.currentCase);
         }
         this._currentSort = param2;
         this._listObjects = param1;
         if(!param1 || param1.length == 0)
         {
            this.mainCtr.visible = false;
         }
         else
         {
            this.mainCtr.visible = true;
            _loc6_ = int(param1.length);
            _loc7_ = new Array(_loc6_);
            _loc8_ = 0;
            while(_loc8_ < _loc6_)
            {
               _loc9_ = param1[_loc8_];
               _loc7_[_loc8_] = {
                  "itemWrapper":_loc9_.itemWrapper,
                  "prices":_loc9_.prices,
                  "currentCase":-1,
                  "p1":_loc9_.prices[0],
                  "p2":_loc9_.prices[1],
                  "p3":_loc9_.prices[2]
               };
               _loc8_++;
            }
            if(param2 < 0)
            {
               param2 *= -1;
               _loc7_.sortOn("p" + param2,Array.NUMERIC | Array.DESCENDING);
            }
            else
            {
               _loc7_.sortOn("p" + param2,Array.NUMERIC);
               _loc10_ = 0;
               while(_loc10_ < _loc6_)
               {
                  if(!_loc7_[_loc10_]["p" + param2] || _loc7_[_loc10_]["p" + param2] <= 0)
                  {
                     _loc7_.push(_loc7_.shift());
                  }
                  _loc10_++;
               }
            }
            this.gd_list.dataProvider = _loc7_;
            if(!param3)
            {
               this.sysApi.sendAction(new ExchangeBidHousePrice(param1[0].itemWrapper.objectGID));
               _loc11_ = param1[0].itemWrapper;
               _loc12_ = this.dataApi.getItem(_loc11_.objectGID);
               this._itemName = _loc12_.name;
               this._item = _loc11_;
               this.modCommon.createItemBox("itemBox",this.ctr_item,_loc11_,true);
               this.btn_reset.visible = false;
            }
         }
      }
      
      private function onExchangeError(param1:int) : void
      {
         if(param1 == 10)
         {
            this.modCommon.openPopup(this.uiApi.getText("ui.bidhouse.bigStore"),this.uiApi.getText("ui.bidhouse.itemNotInBigStore"),[this.uiApi.getText("ui.common.ok")]);
         }
      }
      
      private function onExchangeBidPrice(param1:uint, param2:uint) : void
      {
         this.lbl_averagePrice.text = this.utilApi.kamasToString(param2);
      }
      
      private function onConfirmBuyObject() : void
      {
         var _loc1_:int = int(this._sellerBuyerDescriptor.quantities[this.gd_list.selectedItem.currentCase]);
         var _loc2_:uint = uint(this.gd_list.selectedItem.prices[this.gd_list.selectedItem.currentCase]);
         this.sysApi.sendAction(new ExchangeBidHouseBuy(this.gd_list.selectedItem.itemWrapper.objectUID,this.gd_list.selectedItem.currentCase + 1,_loc2_));
      }
      
      private function onCancelBuyObject() : void
      {
      }
      
      public function onRelease(param1:Object) : void
      {
         var _loc2_:int = 0;
         if(param1 == this.btn_buy)
         {
            if(this.gd_list.selectedItem != null && this.gd_list.selectedItem.currentCase != -1)
            {
               _loc2_ = int(this.gd_list.selectedItem.currentCase);
               this.modCommon.openPopup(this.uiApi.getText("ui.popup.warning"),this.uiApi.getText("ui.bidhouse.doUBuyItemBigStore",this._itemName,this._sellerBuyerDescriptor.quantities[_loc2_],this.utilApi.kamasToString(this.gd_list.selectedItem.prices[_loc2_],"")),[this.uiApi.getText("ui.common.yes"),this.uiApi.getText("ui.common.no")],[this.onConfirmBuyObject,this.onCancelBuyObject],this.onConfirmBuyObject,this.onCancelBuyObject);
            }
         }
         else if(param1 == this.btn_reset)
         {
            this.modCommon.createItemBox("itemBox",this.ctr_item,this._item,true);
            this.btn_reset.visible = false;
         }
         else if(param1 == this.btn_tabQty1)
         {
            if(this._currentSort == 1)
            {
               this.onBidObjectListUpdate(this._listObjects,-1,true);
            }
            else
            {
               this.onBidObjectListUpdate(this._listObjects,1,true);
            }
         }
         else if(param1 == this.btn_tabQty2)
         {
            if(this._currentSort == 2)
            {
               this.onBidObjectListUpdate(this._listObjects,-2,true);
            }
            else
            {
               this.onBidObjectListUpdate(this._listObjects,2,true);
            }
         }
         else if(param1 == this.btn_tabQty3)
         {
            if(this._currentSort == 3)
            {
               this.onBidObjectListUpdate(this._listObjects,-3,true);
            }
            else
            {
               this.onBidObjectListUpdate(this._listObjects,3,true);
            }
         }
      }
      
      public function onSelectItem(param1:Object, param2:uint, param3:Boolean) : void
      {
         switch(param1)
         {
            case this.gd_list:
               this.onObjectSelected(this.gd_list.selectedItem.itemWrapper);
         }
      }
      
      public function onRollOver(param1:Object) : void
      {
         var _loc2_:Object = null;
         var _loc3_:uint = 7;
         var _loc4_:uint = 1;
         switch(param1)
         {
            case this.lbl_averagePriceTitle:
            case this.lbl_averagePrice:
               _loc2_ = this.uiApi.textTooltipInfo(this.uiApi.getText("ui.bidhouse.bigStoreMiddlePrice"));
               break;
            case this.btn_reset:
               _loc2_ = this.uiApi.textTooltipInfo(this.uiApi.getText("ui.item.genericObject"));
         }
         this.uiApi.showTooltip(_loc2_,param1,false,"standard",_loc3_,_loc4_,3,null,null,null,"TextInfo");
      }
      
      public function onRollOut(param1:Object) : void
      {
         this.uiApi.hideTooltip();
      }
      
      public function onObjectSelected(param1:Object) : void
      {
         if(param1)
         {
            this._item = param1;
            this.modCommon.createItemBox("itemBox",this.ctr_item,param1);
            this.btn_reset.visible = true;
         }
      }
      
      public function onShortcut(param1:String) : Boolean
      {
         return false;
      }
      
      public function unload() : void
      {
         this.uiApi.unloadUi("itemBox");
      }
   }
}

