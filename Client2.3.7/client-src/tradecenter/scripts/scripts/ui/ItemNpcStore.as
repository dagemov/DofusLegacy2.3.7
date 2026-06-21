package ui
{
   import d2actions.ExchangeBuy;
   import d2actions.ExchangeSell;
   import d2components.Input;
   import d2hooks.BuyOk;
   import d2hooks.ClickItemInventory;
   import d2hooks.ClickItemStore;
   import d2hooks.KeyUp;
   import d2hooks.ObjectDeleted;
   import d2hooks.SellOk;
   
   public class ItemNpcStore extends BasicItemCard
   {
      
      private var _mode:Boolean;
      
      public var tx_inputPrice:Object;
      
      public function ItemNpcStore()
      {
         super();
      }
      
      override public function main(param1:Object = null) : void
      {
         super.main(param1);
         sysApi.addHook(KeyUp,this.onKeyUp);
         sysApi.addHook(ClickItemStore,this.onClickItemStore);
         sysApi.addHook(ClickItemInventory,this.onClickItemInventory);
         sysApi.addHook(ObjectDeleted,this.onObjectDeleted);
         sysApi.addHook(SellOk,this.onSellOk);
         sysApi.addHook(BuyOk,this.onBuyOk);
         this.tx_inputPrice.visible = false;
      }
      
      override public function unload() : void
      {
         super.unload();
      }
      
      override public function onRelease(param1:Object) : void
      {
         var _loc2_:RegExp = null;
         switch(param1)
         {
            case btn_valid:
               _loc2_ = /^\s*(.*?)\s*$/g;
               input_quantity.text = input_quantity.text.replace(_loc2_,"$1");
               input_price.text = input_price.text.replace(_loc2_,"$1");
               if(this._mode)
               {
                  if(utilApi.stringToKamas(input_quantity.text,"") <= 0 || utilApi.stringToKamas(input_quantity.text,"") > _currentObject.quantity)
                  {
                     modCommon.openPopup(uiApi.getText("ui.common.error"),uiApi.getText("ui.error.invalidQuantity"),[uiApi.getText("ui.common.ok")]);
                     break;
                  }
                  sysApi.sendAction(new ExchangeSell(_currentObject.objectUID,utilApi.stringToKamas(input_quantity.text,"")));
               }
               else
               {
                  if(utilApi.stringToKamas(input_quantity.text,"") <= 0)
                  {
                     modCommon.openPopup(uiApi.getText("ui.common.error"),uiApi.getText("ui.error.invalidQuantity"),[uiApi.getText("ui.common.ok")]);
                     break;
                  }
                  sysApi.sendAction(new ExchangeBuy(_currentObject.objectGID,utilApi.stringToKamas(input_quantity.text,"")));
               }
         }
      }
      
      public function onObjectDeleted(param1:Object) : void
      {
         if(_currentObject.objectUID == param1.objectUID)
         {
            _currentObject = null;
            super.hideCard();
         }
      }
      
      public function onClickItemStore(param1:Object) : void
      {
         this._mode = false;
         sysApi.log(2,"clic magasin");
         onObjectSelected(param1);
         ctr_inputPrice.visible = false;
         btn_lbl_btn_valid.text = uiApi.getText("ui.common.buy");
         var _loc2_:Object = dataApi.getItem(param1.objectGID);
         _currentPrice = (param1.price > 0) ? param1.price : _loc2_.price;
         lbl_price.text = utilApi.kamasToString(_currentPrice);
         input_quantity.text = "1";
         input_quantity.focus();
         input_quantity.textfield.setSelection(0,8388607);
         lbl_totalPrice.text = utilApi.kamasToString(_currentPrice);
      }
      
      public function onClickItemInventory(param1:Object = null) : void
      {
         var _loc2_:Object = null;
         var _loc3_:uint = 0;
         this._mode = true;
         sysApi.log(2,"clic inventaire");
         onObjectSelected(param1);
         ctr_inputPrice.visible = false;
         btn_lbl_btn_valid.text = uiApi.getText("ui.common.sell");
         if(param1 != null)
         {
            _loc2_ = dataApi.getItem(param1.objectGID);
            _loc3_ = 0;
            _currentPrice = _loc2_.price;
            if(_currentPrice > 0)
            {
               if(_currentPrice / TradeCenter.SELLING_RATIO < 1)
               {
                  _loc3_ = 1;
               }
               else
               {
                  _loc3_ = Math.floor(_currentPrice / TradeCenter.SELLING_RATIO);
               }
            }
            _currentPrice = _loc3_;
            lbl_price.text = utilApi.kamasToString(_currentPrice);
            input_quantity.text = "1";
            input_quantity.focus();
            input_quantity.textfield.setSelection(0,8388607);
            lbl_totalPrice.text = utilApi.kamasToString(_currentPrice);
         }
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         var _loc3_:int = 0;
         if((input_quantity as Input).haveFocus)
         {
            _loc3_ = int(input_quantity.text);
            if(this._mode)
            {
               if(_loc3_ > _currentObject.quantity)
               {
                  input_quantity.text = _currentObject.quantity;
                  _loc3_ = int(_currentObject.quantity);
               }
            }
            lbl_totalPrice.text = utilApi.kamasToString(_currentPrice * _loc3_);
         }
      }
      
      private function onSellOk() : void
      {
         hideCard();
      }
      
      private function onBuyOk() : void
      {
         hideCard();
      }
   }
}

