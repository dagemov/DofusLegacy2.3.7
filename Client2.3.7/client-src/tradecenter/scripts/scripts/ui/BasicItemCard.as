package ui
{
   import com.ankamagames.dofusModuleLibrary.enum.SoundEnum;
   import d2api.DataApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2api.UtilApi;
   import flash.events.Event;
   
   public class BasicItemCard
   {
      
      public var sysApi:SystemApi;
      
      public var uiApi:UiApi;
      
      public var dataApi:DataApi;
      
      public var utilApi:UtilApi;
      
      [Module(name="Ankama_Common")]
      public var modCommon:Object;
      
      protected var _currentObject:Object;
      
      protected var _currentPrice:uint = 0;
      
      public var mainCtr:Object;
      
      public var ctr_item:Object;
      
      public var ctr_inputQty:Object;
      
      public var ctr_inputPrice:Object;
      
      public var input_quantity:Object;
      
      public var input_price:Object;
      
      public var lbl_price:Object;
      
      public var lbl_totalPrice:Object;
      
      public var btn_lbl_btn_valid:Object;
      
      public var btn_valid:Object;
      
      public var btn_remove:Object;
      
      public var btn_modify:Object;
      
      public function BasicItemCard()
      {
         super();
      }
      
      public function main(param1:Object = null) : void
      {
         this.btn_valid.soundId = SoundEnum.STORE_SELL_BUTTON;
         this.btn_remove.soundId = SoundEnum.STORE_SELL_BUTTON;
         this.btn_modify.soundId = SoundEnum.STORE_SELL_BUTTON;
         this.uiApi.addShortcutHook("validUi",this.onShortCut);
         this.hideCard();
         this.input_quantity.maxChars = 11;
         this.input_quantity.restrictChars = "0-9";
         this.input_quantity.textfield.addEventListener(Event.CHANGE,this.onInputQuantityChange);
         this.input_price.maxChars = 11;
         this.input_price.restrictChars = "0-9  ";
         this.input_price.textfield.addEventListener(Event.CHANGE,this.onInputKamaChange);
         this.btn_modify.visible = false;
         this.btn_remove.visible = false;
      }
      
      public function get uiVisible() : Boolean
      {
         return this.uiApi.me().visible;
      }
      
      public function onRelease(param1:Object) : void
      {
      }
      
      private function onShortCut(param1:String) : Boolean
      {
         if(param1 == "validUi")
         {
         }
         return false;
      }
      
      public function unload() : void
      {
         this.uiApi.unloadUi("itemBox_" + this.uiApi.me().name);
      }
      
      public function onInputKamaChange(param1:Event) : void
      {
         var _loc2_:int = this.utilApi.stringToKamas(this.input_price.text,"");
         this.input_price.text = this.utilApi.kamasToString(_loc2_,"");
         this.input_price.caretIndex = this.input_price.text.length;
      }
      
      public function onInputQuantityChange(param1:Event) : void
      {
         var _loc2_:int = this.utilApi.stringToKamas(this.input_quantity.text,"");
         this.input_quantity.text = this.utilApi.kamasToString(_loc2_,"");
         this.input_quantity.caretIndex = this.input_quantity.text.length;
      }
      
      public function onObjectSelected(param1:Object = null) : void
      {
         var _loc2_:Object = null;
         if(param1 == null)
         {
            this.hideCard();
         }
         else
         {
            this.hideCard(false);
            this._currentObject = param1;
            _loc2_ = this.dataApi.getItem(param1.objectGID);
            this.modCommon.createItemBox("itemBox_" + this.uiApi.me().name,this.ctr_item,this._currentObject);
            this.input_price.text = "";
            this.lbl_price.text = "";
            this.input_quantity.text = "";
         }
      }
      
      protected function hideCard(param1:Boolean = true) : void
      {
         this.mainCtr.visible = !param1;
      }
   }
}

