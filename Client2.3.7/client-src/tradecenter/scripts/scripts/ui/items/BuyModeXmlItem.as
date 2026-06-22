package ui.items
{
   import d2api.ContextMenuApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2api.UtilApi;
   import d2components.ButtonContainer;
   import d2components.Label;
   import d2components.Slot;
   
   public class BuyModeXmlItem
   {
      
      [Module(name="Ankama_ContextMenu")]
      public var modContextMenu:Object;
      
      public var sysApi:SystemApi;
      
      public var uiApi:UiApi;
      
      public var utilApi:UtilApi;
      
      public var menuApi:ContextMenuApi;
      
      private var _grid:Object;
      
      private var _data:*;
      
      private var _item:Object;
      
      private var _selectedQuantity:int = 1;
      
      public var slot_icon:Slot;
      
      public var btn_q1:ButtonContainer;
      
      public var btn_q2:ButtonContainer;
      
      public var btn_q3:ButtonContainer;
      
      public var lbl_q1:Label;
      
      public var lbl_q2:Label;
      
      public var lbl_q3:Label;
      
      public function BuyModeXmlItem()
      {
         super();
      }
      
      public function main(param1:Object = null) : void
      {
         this._grid = param1.grid;
         this._data = param1.data;
         this.slot_icon.allowDrag = false;
         this.uiApi.addComponentHook(this.btn_q1,"onRelease");
         this.uiApi.addComponentHook(this.btn_q2,"onRelease");
         this.uiApi.addComponentHook(this.btn_q3,"onRelease");
         this.update(this._data,false);
      }
      
      public function unload() : void
      {
      }
      
      public function get data() : *
      {
         return this._data;
      }
      
      public function update(param1:*, param2:Boolean) : void
      {
         this._data = param1;
         if(param1 != null)
         {
            this._item = param1.itemWrapper;
            this.slot_icon.data = this._item;
            if(param1.prices.length == 0 || param1.prices[0] <= 0)
            {
               this.lbl_q1.text = "-";
               this.btn_q1.disabled = true;
            }
            else
            {
               this.lbl_q1.text = this.utilApi.kamasToString(param1.prices[0]);
               this.btn_q1.disabled = false;
            }
            if(param1.prices.length < 2 || param1.prices[1] <= 0)
            {
               this.lbl_q2.text = "-";
               this.btn_q2.disabled = true;
            }
            else
            {
               this.lbl_q2.text = this.utilApi.kamasToString(param1.prices[1]);
               this.btn_q2.disabled = false;
            }
            if(param1.prices.length < 3 || param1.prices[2] <= 0)
            {
               this.lbl_q3.text = "-";
               this.btn_q3.disabled = true;
            }
            else
            {
               this.lbl_q3.text = this.utilApi.kamasToString(param1.prices[2]);
               this.btn_q3.disabled = false;
            }
            if(param2)
            {
               if(this.btn_q1.selected)
               {
                  this.onSelectedItem(this.btn_q1);
               }
               else if(this.btn_q2.selected)
               {
                  this.onSelectedItem(this.btn_q2);
               }
               else if(this.btn_q3.selected)
               {
                  this.onSelectedItem(this.btn_q3);
               }
               else
               {
                  this.onSelectedItem(this["btn_q" + this._selectedQuantity]);
               }
            }
            else
            {
               this.btn_q1.selected = false;
               this.btn_q2.selected = false;
               this.btn_q3.selected = false;
               this.lbl_q1.cssClass = "center";
               this.lbl_q2.cssClass = "center";
               this.lbl_q3.cssClass = "center";
            }
         }
         else
         {
            this.btn_q1.selected = false;
            this.btn_q2.selected = false;
            this.btn_q3.selected = false;
            this.btn_q1.disabled = true;
            this.btn_q2.disabled = true;
            this.btn_q3.disabled = true;
            this.slot_icon.data = null;
            this.lbl_q1.text = "";
            this.lbl_q2.text = "";
            this.lbl_q3.text = "";
            this.lbl_q1.cssClass = "center";
            this.lbl_q2.cssClass = "center";
            this.lbl_q3.cssClass = "center";
         }
      }
      
      private function onSelectedItem(param1:Object) : void
      {
         if(param1 == this.btn_q1)
         {
            this._selectedQuantity = 1;
            this.btn_q1.selected = true;
            this.btn_q2.selected = false;
            this.btn_q3.selected = false;
            this.btn_q1.state = this.sysApi.getEnum("com.ankamagames.berilia.enums.StatesEnum").STATE_SELECTED;
            this.lbl_q1.cssClass = "darkcenter";
            this.lbl_q2.cssClass = "center";
            this.lbl_q3.cssClass = "center";
            this.data.currentCase = 0;
            this.uiApi.getUi("itemBidHouseBuy").uiClass.btn_buy.disabled = false;
         }
         else if(param1 == this.btn_q2)
         {
            this._selectedQuantity = 2;
            this.btn_q1.selected = false;
            this.btn_q2.selected = true;
            this.btn_q3.selected = false;
            this.btn_q2.state = this.sysApi.getEnum("com.ankamagames.berilia.enums.StatesEnum").STATE_SELECTED;
            this.lbl_q1.cssClass = "center";
            this.lbl_q2.cssClass = "darkcenter";
            this.lbl_q3.cssClass = "center";
            this.data.currentCase = 1;
            this.uiApi.getUi("itemBidHouseBuy").uiClass.btn_buy.disabled = false;
         }
         else if(param1 == this.btn_q3)
         {
            this._selectedQuantity = 3;
            this.btn_q1.selected = false;
            this.btn_q2.selected = false;
            this.btn_q3.selected = true;
            this.btn_q3.state = this.sysApi.getEnum("com.ankamagames.berilia.enums.StatesEnum").STATE_SELECTED;
            this.lbl_q1.cssClass = "center";
            this.lbl_q2.cssClass = "center";
            this.lbl_q3.cssClass = "darkcenter";
            this.data.currentCase = 2;
            this.uiApi.getUi("itemBidHouseBuy").uiClass.btn_buy.disabled = false;
         }
         else
         {
            this.uiApi.getUi("itemBidHouseBuy").uiClass.btn_buy.disabled = true;
         }
      }
      
      public function onRelease(param1:Object) : void
      {
         this.onSelectedItem(param1);
      }
      
      public function onRollOver(param1:Object) : void
      {
         var _loc2_:Object = null;
         if(param1 == this.slot_icon)
         {
            if(this.sysApi.getOption("displayTooltips","dofus"))
            {
               _loc2_ = this.sysApi.getData("itemTooltipSettings",true);
               if(!_loc2_)
               {
                  _loc2_ = {
                     "header":true,
                     "effects":true,
                     "conditions":true,
                     "description":true
                  };
                  this.sysApi.setData("itemTooltipSettings",_loc2_,true);
               }
               this.uiApi.showTooltip(this._item,this.slot_icon,false,"standard",8,0,0,null,null,_loc2_);
            }
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
         if(param1 == this.slot_icon)
         {
            _loc2_ = param1.data;
            _loc3_ = this.menuApi.create(_loc2_);
            if(_loc3_.content.length > 0)
            {
               this.modContextMenu.createContextMenu(_loc3_);
            }
         }
      }
   }
}

