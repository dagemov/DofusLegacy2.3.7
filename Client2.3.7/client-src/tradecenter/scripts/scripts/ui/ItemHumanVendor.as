package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.hooks.ShortcutHookList;
   import d2actions.ExchangeBuy;
   import d2api.ContextMenuApi;
   import d2hooks.BuyOk;
   import d2hooks.ClickItemShopHV;
   import d2hooks.KeyUp;
   
   public class ItemHumanVendor extends BasicItemCard
   {
      
      private static var _self:ItemHumanVendor;
      
      public var menuApi:ContextMenuApi;
      
      [Module(name="Ankama_ContextMenu")]
      public var modContextMenu:Object;
      
      public function ItemHumanVendor()
      {
         super();
      }
      
      public static function getInstance() : ItemHumanVendor
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
         sysApi.addHook(ClickItemShopHV,this.onClickItemShopHV);
         uiApi.addShortcutHook(ShortcutHookList.VALID_UI,this.onShortcut);
         sysApi.addHook(BuyOk,this.onBuyOk);
         _self = this;
         ctr_inputPrice.visible = false;
         btn_lbl_btn_valid.text = uiApi.getText("ui.common.buy");
      }
      
      override public function onRelease(param1:Object) : void
      {
         switch(param1)
         {
            case btn_valid:
               if(utilApi.stringToKamas(input_quantity.text,"") > _currentObject.quantity || utilApi.stringToKamas(input_quantity.text,"") == 0)
               {
                  modCommon.openPopup(uiApi.getText("ui.common.error"),uiApi.getText("ui.error.invalidQuantity"),[uiApi.getText("ui.common.ok")]);
                  break;
               }
               sysApi.sendAction(new ExchangeBuy(_currentObject.objectUID,utilApi.stringToKamas(input_quantity.text,"")));
         }
      }
      
      public function onClickItemShopHV(param1:Object, param2:uint = 0) : void
      {
         _currentPrice = param2;
         onObjectSelected(param1);
         lbl_price.text = utilApi.kamasToString(_currentPrice,"");
         input_quantity.text = "1";
         input_quantity.textfield.setSelection(0,8388607);
         input_quantity.focus();
         lbl_totalPrice.text = utilApi.kamasToString(_currentPrice,"");
      }
      
      public function onShortcut(param1:String) : Boolean
      {
         switch(param1)
         {
            case ShortcutHookList.VALID_UI:
               if(input_quantity.haveFocus)
               {
                  this.onRelease(btn_valid);
                  return true;
               }
         }
         return false;
      }
      
      public function onKeyUp(param1:Object, param2:uint) : void
      {
         if(input_quantity.haveFocus)
         {
            if(utilApi.stringToKamas(input_quantity.text,"") > _currentObject.quantity)
            {
               lbl_totalPrice.text = utilApi.kamasToString(_currentPrice * _currentObject.quantity,"");
               input_quantity.text = utilApi.kamasToString(_currentObject.quantity,"");
            }
            else
            {
               lbl_totalPrice.text = utilApi.kamasToString(_currentPrice * utilApi.stringToKamas(input_quantity.text,""),"");
            }
         }
      }
      
      private function onBuyOk() : void
      {
         hideCard();
      }
   }
}

