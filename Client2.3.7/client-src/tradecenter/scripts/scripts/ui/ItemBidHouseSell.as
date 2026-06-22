package ui
{
   import d2actions.ExchangeBidHousePrice;
   import d2actions.ExchangeShopStockMouvmentAdd;
   import d2actions.ExchangeShopStockMouvmentRemove;
   import d2api.ContextMenuApi;
   import d2components.ComboBox;
   import d2components.Label;
   import d2hooks.ExchangeBidPrice;
   import d2hooks.KeyUp;
   
   public class ItemBidHouseSell extends BasicItemCard
   {
      
      private static var _lastSell:Object;
      
      public var menuApi:ContextMenuApi;
      
      [Module(name="Ankama_ContextMenu")]
      public var modContextMenu:Object;
      
      public var _sellerDescriptor:Object;
      
      private var _exchangeQuantity:uint;
      
      private var _itemName:String;
      
      private var _mode:Boolean;
      
      private var _price:uint;
      
      private var _tax:uint;
      
      public var lbl_error:Label;
      
      public var lbl_quantity:Label;
      
      public var lbl_taxTimeTitle:Label;
      
      public var lbl_taxTime:Label;
      
      public var lbl_averagePrice:Label;
      
      public var lbl_averagePriceTitle:Label;
      
      public var cb_quantity:ComboBox;
      
      public var ctr_sellingGroup:Object;
      
      public function ItemBidHouseSell()
      {
         super();
      }
      
      override public function main(param1:Object = null) : void
      {
         super.main(param1);
         sysApi.addHook(ExchangeBidPrice,this.onExchangeBidPrice);
         sysApi.addHook(KeyUp,this.onKeyUp);
         uiApi.addShortcutHook("validUi",this.onShortcut);
         uiApi.addComponentHook(this.lbl_averagePriceTitle,"onRollOver");
         uiApi.addComponentHook(this.lbl_averagePriceTitle,"onRollOut");
         uiApi.addComponentHook(this.lbl_averagePrice,"onRollOver");
         uiApi.addComponentHook(this.lbl_averagePrice,"onRollOut");
         ctr_inputQty.visible = false;
         this._sellerDescriptor = param1.sellerBuyerDescriptor;
      }
      
      public function onSelectItemFromInventory(param1:Object) : void
      {
         var _loc5_:* = undefined;
         var _loc6_:Array = null;
         var _loc7_:int = 0;
         var _loc8_:int = 0;
         var _loc9_:uint = 0;
         if(TradeCenter.BID_HOUSE_BUY_MODE)
         {
            return;
         }
         this._mode = true;
         onObjectSelected(param1);
         this._itemName = param1.name;
         lbl_price.visible = false;
         ctr_inputPrice.visible = true;
         this.lbl_quantity.visible = false;
         this.cb_quantity.visible = true;
         this.ctr_sellingGroup.visible = false;
         this.lbl_error.visible = false;
         btn_valid.disabled = true;
         this.lbl_taxTime.text = "";
         this.lbl_taxTimeTitle.text = uiApi.getText("ui.bidhouse.bigStoreTax") + " :";
         var _loc2_:Boolean = false;
         var _loc3_:int = int(this._sellerDescriptor.types.length);
         var _loc4_:int = 0;
         while(_loc4_ < _loc3_)
         {
            if(this._sellerDescriptor.types[_loc4_] == _currentObject.typeId)
            {
               _loc2_ = true;
               for each(_loc5_ in _currentObject.effects)
               {
                  if(_loc5_.effectId == 982 || _loc5_.effectId == 983)
                  {
                     _loc2_ = false;
                  }
               }
               break;
            }
            _loc4_++;
         }
         if(!_loc2_)
         {
            this.lbl_error.text = uiApi.getText("ui.bidhouse.badType");
            this.lbl_error.visible = true;
         }
         else if(_currentObject.level > this._sellerDescriptor.maxItemLevel)
         {
            this.lbl_error.text = uiApi.getText("ui.bidhouse.badLevel");
            this.lbl_error.visible = true;
         }
         else
         {
            _loc6_ = new Array();
            _loc7_ = int(this._sellerDescriptor.quantities.length);
            _loc8_ = 0;
            while(_loc8_ < _loc7_)
            {
               _loc9_ = uint(this._sellerDescriptor.quantities[_loc8_]);
               if(_currentObject.quantity >= _loc9_)
               {
                  _loc6_.push({
                     "label":String(_loc9_),
                     "quantity":_loc8_ + 1
                  });
               }
               _loc8_++;
            }
            this.cb_quantity.dataProvider = _loc6_;
            if(TradeCenter.SALES_QUANTITIES[param1.objectGID])
            {
               this.cb_quantity.selectedIndex = TradeCenter.SALES_QUANTITIES[param1.objectGID] - 1;
            }
            else
            {
               this.cb_quantity.selectedIndex = _loc6_.length - 1;
            }
            btn_lbl_btn_valid.text = uiApi.getText("ui.common.putOnSell");
            btn_valid.disabled = false;
            if(Boolean(TradeCenter.SALES_PRICES[param1.objectGID]) && Boolean(TradeCenter.SALES_PRICES[param1.objectGID][this.cb_quantity.value.quantity.toString()]))
            {
               input_price.text = utilApi.kamasToString(TradeCenter.SALES_PRICES[param1.objectGID][this.cb_quantity.value.quantity.toString()],"");
            }
            else
            {
               input_price.text = "";
            }
            input_price.focus();
            input_price.textfield.setSelection(0,8388607);
            this.ctr_sellingGroup.visible = true;
         }
         sysApi.sendAction(new ExchangeBidHousePrice(_currentObject.objectGID));
         uiApi.getUi("stockBidHouse").uiClass.gd_shop.selectedIndex = -1;
      }
      
      public function onSelectItemFromStockBidHouse(param1:Object) : void
      {
         if(param1 == null)
         {
            return;
         }
         this._mode = false;
         onObjectSelected(param1.itemWrapper);
         lbl_price.visible = true;
         ctr_inputPrice.visible = false;
         this.cb_quantity.visible = false;
         this.lbl_quantity.visible = true;
         this.ctr_sellingGroup.visible = true;
         this.lbl_error.visible = false;
         btn_valid.disabled = false;
         this._exchangeQuantity = _currentObject.quantity;
         var _loc2_:Object = dataApi.getItem(_currentObject.objectGID);
         this._itemName = _loc2_.name;
         lbl_price.text = utilApi.kamasToString(param1.price);
         this.lbl_quantity.text = this._exchangeQuantity.toString();
         sysApi.sendAction(new ExchangeBidHousePrice(_currentObject.objectGID));
         this.lbl_taxTimeTitle.text = uiApi.getText("ui.bidhouse.bigStoreTime") + " : ";
         this.lbl_taxTime.text = param1.unsoldDelay + " H";
         btn_lbl_btn_valid.text = uiApi.getText("ui.common.remove");
      }
      
      public function displayUi(param1:Boolean) : void
      {
         if(!param1)
         {
            hideCard();
         }
      }
      
      private function onConfirmSellObject() : void
      {
         sysApi.sendAction(new ExchangeShopStockMouvmentAdd(_currentObject.objectUID,this._exchangeQuantity,this._price));
         if(!TradeCenter.SALES_PRICES[_currentObject.objectGID])
         {
            TradeCenter.SALES_PRICES[_currentObject.objectGID] = new Array();
         }
         TradeCenter.SALES_PRICES[_currentObject.objectGID][this._exchangeQuantity.toString()] = this._price;
         if(this._exchangeQuantity < this.cb_quantity.dataProvider.length)
         {
            TradeCenter.SALES_QUANTITIES[_currentObject.objectGID] = this._exchangeQuantity;
         }
         hideCard();
      }
      
      private function onCancelSellObject() : void
      {
      }
      
      private function putOnSell() : void
      {
         modCommon.openPopup(uiApi.getText("ui.popup.warning"),uiApi.getText("ui.bidhouse.doUSellItemBigStore","x" + this._sellerDescriptor.quantities[this._exchangeQuantity - 1] + " " + _currentObject.name,utilApi.kamasToString(this._price / int(this._sellerDescriptor.quantities[this._exchangeQuantity - 1]),""),utilApi.kamasToString(this._price,""),utilApi.kamasToString(this._tax,"")),[uiApi.getText("ui.common.yes"),uiApi.getText("ui.common.no")],[this.onConfirmSellObject,this.onCancelSellObject],this.onConfirmSellObject,this.onCancelSellObject);
      }
      
      private function onExchangeBidPrice(param1:uint, param2:uint) : void
      {
         this.lbl_averagePrice.text = utilApi.kamasToString(param2);
      }
      
      override public function onRelease(param1:Object) : void
      {
         if(param1 == btn_valid)
         {
            if(this._mode)
            {
               this._price = utilApi.stringToKamas(input_price.text,"");
               if(this._price > 0)
               {
                  this.putOnSell();
               }
            }
            else
            {
               hideCard();
               sysApi.sendAction(new ExchangeShopStockMouvmentRemove(_currentObject.objectUID,-this._exchangeQuantity));
            }
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
               _loc2_ = uiApi.textTooltipInfo(uiApi.getText("ui.bidhouse.bigStoreMiddlePrice"));
         }
         uiApi.showTooltip(_loc2_,param1,false,"standard",_loc3_,_loc4_,3,null,null,null,"TextInfo");
      }
      
      public function onRollOut(param1:Object) : void
      {
         uiApi.hideTooltip();
      }
      
      public function onRightClick(param1:Object) : void
      {
      }
      
      public function onSelectItem(param1:Object, param2:uint, param3:Boolean) : void
      {
         if(param1 == this.cb_quantity)
         {
            this._exchangeQuantity = param1.value.quantity;
            if(Boolean(TradeCenter.SALES_PRICES[_currentObject.objectGID]) && Boolean(TradeCenter.SALES_PRICES[_currentObject.objectGID][param1.value.quantity.toString()]))
            {
               input_price.text = utilApi.kamasToString(TradeCenter.SALES_PRICES[_currentObject.objectGID][param1.value.quantity.toString()],"");
               input_price.focus();
               input_price.textfield.setSelection(0,8388607);
               this._tax = Math.ceil(utilApi.stringToKamas(input_price.text,"") * (this._sellerDescriptor.taxPercentage / 100));
               this.lbl_taxTime.text = utilApi.kamasToString(this._tax);
            }
            else
            {
               input_price.text = "";
            }
         }
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         if(input_price.haveFocus)
         {
            this._tax = Math.ceil(utilApi.stringToKamas(input_price.text,"") * (this._sellerDescriptor.taxPercentage / 100));
            this.lbl_taxTime.text = utilApi.kamasToString(this._tax);
         }
      }
      
      public function onShortcut(param1:String) : Boolean
      {
         if(!TradeCenter.BID_HOUSE_BUY_MODE)
         {
            switch(param1)
            {
               case "validUi":
                  if(!uiVisible)
                  {
                     return false;
                  }
                  if(Boolean(this.ctr_sellingGroup.visible) && this._mode)
                  {
                     this._price = utilApi.stringToKamas(input_price.text,"");
                     if(this._price > 0)
                     {
                        this.putOnSell();
                        btn_valid.focus();
                     }
                  }
                  else
                  {
                     hideCard();
                     sysApi.sendAction(new ExchangeShopStockMouvmentRemove(_currentObject.objectUID,-this._exchangeQuantity));
                  }
                  return true;
            }
         }
         return false;
      }
   }
}

