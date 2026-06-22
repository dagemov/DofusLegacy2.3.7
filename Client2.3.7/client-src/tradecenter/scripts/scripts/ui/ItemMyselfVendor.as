package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.SoundEnum;
   import com.ankamagames.dofusModuleLibrary.enum.SoundTypeEnum;
   import com.ankamagames.dofusModuleLibrary.enum.hooks.ShortcutHookList;
   import d2actions.ExchangeShopStockModifyObject;
   import d2actions.ExchangeShopStockMouvmentAdd;
   import d2actions.ExchangeShopStockMouvmentRemove;
   import d2api.InventoryApi;
   import d2api.SoundApi;
   import d2hooks.ClickItemInventory;
   import d2hooks.ClickItemShopHV;
   import d2hooks.KeyUp;
   import d2hooks.ObjectDeleted;
   import flash.events.Event;
   import flash.text.TextField;
   
   public class ItemMyselfVendor extends BasicItemCard
   {
      
      private static var _self:ItemMyselfVendor;
      
      public static const SELL_MOD:String = "sell_mod";
      
      public static const MODIFY_REMOVE_MOD:String = "modify_remove_mod";
      
      public var soundApi:SoundApi;
      
      public var inventoryApi:InventoryApi;
      
      private var _currentMod:String;
      
      public function ItemMyselfVendor()
      {
         super();
      }
      
      public static function getInstance() : ItemMyselfVendor
      {
         if(_self == null)
         {
            return null;
         }
         return _self;
      }
      
      override public function main(param1:Object = null) : void
      {
         super.main(param1);
         sysApi.addHook(KeyUp,this.onKeyUp);
         btn_valid.soundId = SoundEnum.MERCHANT_SELL_BUTTON;
         btn_remove.soundId = SoundEnum.MERCHANT_REMOVE_SELL_BUTTON;
         sysApi.addHook(ClickItemInventory,this.onClickItemInventory);
         sysApi.addHook(ClickItemShopHV,this.onClickItemShopHV);
         sysApi.addHook(ObjectDeleted,this.onObjectDeleted);
         uiApi.addShortcutHook(ShortcutHookList.VALID_UI,this.onShortcut);
         (input_quantity.textfield as TextField).addEventListener(Event.CHANGE,this.onQuantityInputChange);
         _self = this;
         lbl_price.visible = false;
         btn_lbl_btn_valid.text = uiApi.getText("ui.common.putOnSell");
      }
      
      private function switchMode(param1:Boolean) : void
      {
         btn_valid.visible = param1;
         btn_modify.visible = !param1;
         btn_remove.visible = !param1;
      }
      
      private function displayObject(param1:Object) : void
      {
         onObjectSelected(param1);
         if(param1)
         {
            if(_currentPrice > 0)
            {
               input_price.text = utilApi.kamasToString(_currentPrice,"");
            }
            else
            {
               input_price.text = "";
            }
            input_quantity.text = utilApi.kamasToString(_currentObject.quantity,"");
            input_price.textfield.setSelection(0,8388607);
            input_price.focus();
            lbl_totalPrice.text = utilApi.kamasToString(_currentPrice * param1.quantity);
         }
      }
      
      override public function onRelease(param1:Object) : void
      {
         var _loc2_:RegExp = null;
         var _loc3_:Boolean = false;
         var _loc4_:Boolean = false;
         switch(param1)
         {
            case btn_valid:
               _loc2_ = /^\s*(.*?)\s*$/g;
               input_quantity.text = input_quantity.text.replace(_loc2_,"$1");
               input_price.text = input_price.text.replace(_loc2_,"$1");
               if(input_quantity.text == "" || input_price.text == "")
               {
                  modCommon.openPopup(uiApi.getText("ui.common.error"),uiApi.getText("ui.error.allFieldsRequired"),[uiApi.getText("ui.common.ok")]);
                  break;
               }
               if(utilApi.stringToKamas(input_quantity.text,"") > _currentObject.quantity || utilApi.stringToKamas(input_quantity.text,"") <= 0)
               {
                  modCommon.openPopup(uiApi.getText("ui.common.error"),uiApi.getText("ui.error.invalidQuantity"),[uiApi.getText("ui.common.ok")]);
                  break;
               }
               if(utilApi.stringToKamas(input_price.text,"") <= 0)
               {
                  modCommon.openPopup(uiApi.getText("ui.common.error"),uiApi.getText("ui.error.invalidPrice"),[uiApi.getText("ui.common.ok")]);
                  break;
               }
               sysApi.sendAction(new ExchangeShopStockMouvmentAdd(_currentObject.objectUID,utilApi.stringToKamas(input_quantity.text,""),utilApi.stringToKamas(input_price.text,"")));
               break;
            case btn_modify:
               _loc3_ = int(input_quantity.text) != _currentObject.quantity;
               _loc4_ = utilApi.stringToKamas(input_price.text,"") != _currentObject.price;
               if(_loc3_ && _loc4_)
               {
                  sysApi.sendAction(new ExchangeShopStockModifyObject(_currentObject.objectUID,int(input_quantity.text),utilApi.stringToKamas(input_price.text,"")));
               }
               else
               {
                  if(_loc3_)
                  {
                     sysApi.sendAction(new ExchangeShopStockModifyObject(_currentObject.objectUID,int(input_quantity.text),0));
                  }
                  if(_loc4_)
                  {
                     sysApi.sendAction(new ExchangeShopStockModifyObject(_currentObject.objectUID,0,utilApi.stringToKamas(input_price.text,"")));
                  }
               }
               break;
            case btn_remove:
               sysApi.sendAction(new ExchangeShopStockMouvmentRemove(_currentObject.objectUID,utilApi.stringToKamas(input_quantity.text,"")));
         }
      }
      
      override public function unload() : void
      {
         (input_quantity.textfield as TextField).removeEventListener(Event.CHANGE,this.onQuantityInputChange);
         super.unload();
      }
      
      private function onQuantityInputChange(param1:Event) : void
      {
         var _loc4_:uint = 0;
         var _loc2_:uint = this.inventoryApi.getItemQty(_currentObject.objectGID);
         var _loc3_:uint = uint(_currentObject.quantity);
         switch(this._currentMod)
         {
            case SELL_MOD:
               if(utilApi.stringToKamas(input_quantity.text,"") > _loc2_)
               {
                  input_quantity.text = utilApi.kamasToString(_loc2_,"");
               }
               break;
            case MODIFY_REMOVE_MOD:
               if(utilApi.stringToKamas(input_quantity.text,"") > _loc2_ + _loc3_)
               {
                  input_quantity.text = utilApi.kamasToString(_loc2_ + _loc3_,"");
               }
         }
      }
      
      public function onObjectDeleted(param1:Object) : void
      {
         if(_currentObject.objectUID == param1.objectUID)
         {
            hideCard();
         }
      }
      
      public function onClickItemShopHV(param1:Object, param2:uint = 0) : void
      {
         if(!uiVisible)
         {
            this.soundApi.playSound(SoundTypeEnum.MERCHANT_TRANSFERT_OPEN);
         }
         this._currentMod = MODIFY_REMOVE_MOD;
         _currentPrice = param2;
         this.switchMode(false);
         this.displayObject(param1);
      }
      
      public function onClickItemInventory(param1:Object) : void
      {
         if(!uiVisible)
         {
            this.soundApi.playSound(SoundTypeEnum.MERCHANT_TRANSFERT_OPEN);
         }
         this._currentMod = SELL_MOD;
         _currentPrice = 0;
         this.switchMode(true);
         this.displayObject(param1);
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         if(Boolean(input_quantity.haveFocus) || Boolean(input_price.haveFocus))
         {
            lbl_totalPrice.text = utilApi.kamasToString(utilApi.stringToKamas(input_price.text,"") * utilApi.stringToKamas(input_quantity.text,""));
         }
      }
      
      public function onShortcut(param1:String) : Boolean
      {
         switch(param1)
         {
            case ShortcutHookList.CLOSE_UI:
               break;
            case ShortcutHookList.VALID_UI:
               if(Boolean(input_price.haveFocus) || Boolean(input_quantity.haveFocus))
               {
                  if(this._currentMod == SELL_MOD)
                  {
                     this.onRelease(btn_valid);
                  }
                  else
                  {
                     this.onRelease(btn_modify);
                  }
                  return true;
               }
         }
         return false;
      }
   }
}

