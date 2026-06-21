package
{
   import com.ankamagames.dofusModuleLibrary.enum.interfaces.UIEnum;
   import d2actions.ExchangeStartAsVendorRequest;
   import d2actions.LeaveDialogRequest;
   import d2actions.LeaveShopStock;
   import d2actions.OpenInventory;
   import d2api.ExchangeApi;
   import d2api.PlayedCharacterApi;
   import d2api.SystemApi;
   import d2api.UiApi;
   import d2hooks.CloseHumanVendor;
   import d2hooks.CloseInventory;
   import d2hooks.CloseStore;
   import d2hooks.EstateToSellList;
   import d2hooks.ExchangeLeave;
   import d2hooks.ExchangeReplyTaxVendor;
   import d2hooks.ExchangeShopStockStarted;
   import d2hooks.ExchangeStartOkHumanVendor;
   import d2hooks.ExchangeStartOkNpcShop;
   import d2hooks.ExchangeStartedBidBuyer;
   import d2hooks.ExchangeStartedBidSeller;
   import d2hooks.NpcDialogQuestion;
   import flash.display.Sprite;
   import flash.utils.Dictionary;
   import ui.EstateAgency;
   import ui.EstateForm;
   import ui.ItemBidHouseBuy;
   import ui.ItemBidHouseSell;
   import ui.ItemHumanVendor;
   import ui.ItemMyselfVendor;
   import ui.ItemNpcStore;
   import ui.ShopDirectory;
   import ui.StockBidHouse;
   import ui.StockHumanVendor;
   import ui.StockMyselfVendor;
   import ui.StockNpcStore;
   import ui.items.BuyModeXmlItem;
   
   public class TradeCenter extends Sprite
   {
      
      public static var BID_HOUSE_BUY_MODE:Boolean;
      
      public static var SWITCH_MODE:Boolean = false;
      
      public static var SEARCH_MODE:Boolean = false;
      
      public static var SALES_PRICES:Dictionary = new Dictionary();
      
      public static var SALES_QUANTITIES:Dictionary = new Dictionary();
      
      public static const SELLING_RATIO:uint = 10;
      
      public static const SHOP_DIRECTORY_UI:String = "shopDirectory";
      
      private static const VIRTUAL_SHOP_DIRECTORY_MSG:int = 9900;
      
      public var uiApi:UiApi;
      
      public var sysApi:SystemApi;
      
      [Module(name="Ankama_Common")]
      public var modCommon:Object;
      
      public var playerApi:PlayedCharacterApi;
      
      public var exchangeApi:ExchangeApi;
      
      private var include_StockBidHouse:StockBidHouse = null;
      
      private var include_ItemBidHouseSell:ItemBidHouseSell = null;
      
      private var include_ItemBidHouseBuy:ItemBidHouseBuy = null;
      
      private var include_BuyModeXmlItem:BuyModeXmlItem = null;
      
      private var include_EstateAgency:EstateAgency = null;
      
      private var include_EstateForm:EstateForm = null;
      
      private var include_ShopDirectory:ShopDirectory = null;
      
      private var include_StockMyselfVendor:StockMyselfVendor = null;
      
      private var include_StockHumanVendor:StockHumanVendor = null;
      
      private var include_ItemMyselfVendor:ItemMyselfVendor = null;
      
      private var include_ItemHumanVendor:ItemHumanVendor = null;
      
      private var _include_StockNpcStore:StockNpcStore = null;
      
      private var _include_ItemNpcStoreSell:ItemNpcStore = null;
      
      private var _storedObjectsInfos:Object;
      
      private var _askShowUI:Boolean = false;
      
      private var _inventoryOpen:Boolean;
      
      private var _uiToOpen:Array;
      
      private var _sellerName:String;
      
      private var _askToSwitchToVendor:Boolean = false;
      
      public function TradeCenter()
      {
         super();
      }
      
      public function main() : void
      {
         this.sysApi.addHook(ExchangeStartedBidSeller,this.onExchangeStartedBidSeller);
         this.sysApi.addHook(ExchangeStartedBidBuyer,this.onExchangeStartedBidBuyer);
         this.sysApi.addHook(CloseInventory,this.onCloseInventory);
         this.sysApi.addHook(EstateToSellList,this.onEstateList);
         this.sysApi.addHook(ExchangeShopStockStarted,this.onExchangeShopStockStarted);
         this.sysApi.addHook(CloseHumanVendor,this.onCloseHumanVendor);
         this.sysApi.addHook(ExchangeStartOkHumanVendor,this.onExchangeStartOkHumanVendor);
         this.sysApi.addHook(ExchangeReplyTaxVendor,this.onExchangeReplyTaxVendor);
         this.sysApi.addHook(ExchangeLeave,this.onExchangeLeave);
         this.sysApi.addHook(ExchangeStartOkNpcShop,this.onExchangeStartOkNpcShop);
         this.sysApi.addHook(CloseStore,this.onCloseStore);
         this.sysApi.addHook(NpcDialogQuestion,this.onNpcDialogQuestion);
         this._uiToOpen = new Array();
      }
      
      private function onNpcDialogQuestion(param1:Object, param2:Object, param3:Object) : void
      {
         if(int(param1) != VIRTUAL_SHOP_DIRECTORY_MSG)
         {
            return;
         }
         var names:Array = param2 as Array;
         var ids:Array = param3 as Array;
         if(!names || !ids)
         {
            return;
         }
         var shops:Array = [];
         var i:int = 0;
         while(i < names.length && i < ids.length)
         {
            shops.push({
               "shopId":int(ids[i]),
               "name":String(names[i])
            });
            i++;
         }
         this.sysApi.sendAction(new LeaveDialogRequest());
         if(this.uiApi.getUi(SHOP_DIRECTORY_UI) == null)
         {
            this.uiApi.loadUi(SHOP_DIRECTORY_UI,SHOP_DIRECTORY_UI,{"shops":shops});
         }
      }
      
      private function onExchangeStartedBidSeller(param1:Object, param2:Object) : void
      {
         SWITCH_MODE = false;
         BID_HOUSE_BUY_MODE = false;
         var _loc3_:Object = this.uiApi.getUi(UIEnum.BIDHOUSE_SELL);
         if(_loc3_ == null)
         {
            this.uiApi.loadUi(UIEnum.BIDHOUSE_SELL,UIEnum.BIDHOUSE_SELL,{"sellerBuyerDescriptor":param1});
         }
         var _loc4_:Object = this.uiApi.getUi(UIEnum.BIDHOUSE_BUY);
         if(_loc4_ == null)
         {
            this.uiApi.loadUi(UIEnum.BIDHOUSE_BUY,UIEnum.BIDHOUSE_BUY,{"sellerBuyerDescriptor":param1});
         }
         var _loc5_:Object = this.uiApi.getUi(UIEnum.BIDHOUSE_STOCK);
         if(_loc5_ == null)
         {
            this.uiApi.loadUi(UIEnum.BIDHOUSE_STOCK,UIEnum.BIDHOUSE_STOCK,{
               "sellerBuyerDescriptor":param1,
               "objectsInfos":param2
            });
         }
         else
         {
            _loc5_.uiClass.changeBidHouseMode();
         }
         this.sysApi.sendAction(new OpenInventory("bidHouse"));
      }
      
      private function onExchangeStartedBidBuyer(param1:Object) : void
      {
         SWITCH_MODE = false;
         BID_HOUSE_BUY_MODE = true;
         var _loc2_:Object = this.uiApi.getUi(UIEnum.BIDHOUSE_SELL);
         if(_loc2_ == null)
         {
            this.uiApi.loadUi(UIEnum.BIDHOUSE_SELL,UIEnum.BIDHOUSE_SELL,{"sellerBuyerDescriptor":param1});
         }
         var _loc3_:Object = this.uiApi.getUi(UIEnum.BIDHOUSE_BUY);
         if(_loc3_ == null)
         {
            this.uiApi.loadUi(UIEnum.BIDHOUSE_BUY,UIEnum.BIDHOUSE_BUY,{
               "visible":true,
               "sellerBuyerDescriptor":param1
            });
         }
         var _loc4_:Object = this.uiApi.getUi(UIEnum.BIDHOUSE_STOCK);
         if(_loc4_ == null)
         {
            this.uiApi.loadUi(UIEnum.BIDHOUSE_STOCK,UIEnum.BIDHOUSE_STOCK,{"sellerBuyerDescriptor":param1});
         }
         else
         {
            _loc4_.uiClass.changeBidHouseMode();
         }
         this.sysApi.sendAction(new OpenInventory("bidHouse"));
      }
      
      private function onEstateList(param1:Object, param2:uint, param3:uint, param4:uint = 0) : void
      {
         if(this.uiApi.getUi("estateAgency") == null)
         {
            this.uiApi.loadUi("estateAgency","estateAgency",{
               "type":param4,
               "list":param1,
               "index":param2,
               "total":param3
            });
         }
      }
      
      public function onExchangeStartOkNpcShop(param1:int, param2:Object, param3:Object) : void
      {
         this.uiApi.unloadUi(SHOP_DIRECTORY_UI);
         this.uiApi.loadUi(UIEnum.NPC_STOCK,UIEnum.NPC_STOCK,{
            "NPCSellerId":param1,
            "Objects":param2,
            "Look":param3
         });
         this.sysApi.sendAction(new OpenInventory("shop"));
      }
      
      private function onCloseStore() : void
      {
         this.uiApi.unloadUi(UIEnum.NPC_STOCK);
         this.uiApi.unloadUi(SHOP_DIRECTORY_UI);
      }
      
      private function onExchangeLeave(param1:Boolean) : void
      {
         if(this._askToSwitchToVendor)
         {
            this.sysApi.sendAction(new ExchangeStartAsVendorRequest());
         }
         this._askToSwitchToVendor = false;
      }
      
      private function onExchangeReplyTaxVendor(param1:uint) : void
      {
         this.modCommon.openPopup(this.uiApi.getText("ui.humanVendor.poupTitleTaxMessage"),this.uiApi.getText("ui.humanVendor.taxPriceMessage",param1),[this.uiApi.getText("ui.common.yes"),this.uiApi.getText("ui.common.no")],[this.onAcceptTax,this.onRefuseTax],this.onAcceptTax,this.onRefuseTax);
      }
      
      private function onExchangeStartOkHumanVendor(param1:String, param2:Object) : void
      {
         this._askShowUI = true;
         this._storedObjectsInfos = param2;
         this._uiToOpen = new Array(UIEnum.HUMAN_VENDOR_STOCK,UIEnum.HUMAN_VENDOR);
         this.sysApi.sendAction(new OpenInventory("humanVendor"));
         this.openUi(param1);
      }
      
      private function onExchangeShopStockStarted(param1:Object) : void
      {
         this._askShowUI = true;
         this._storedObjectsInfos = param1;
         this._uiToOpen = new Array(UIEnum.MYSELF_VENDOR_STOCK,UIEnum.MYSELF_VENDOR);
         this.sysApi.sendAction(new OpenInventory("myselfVendor"));
         this.openUi();
      }
      
      private function onCloseInventory() : void
      {
         if(SWITCH_MODE)
         {
            SWITCH_MODE = false;
         }
         this._inventoryOpen = false;
      }
      
      private function onCloseHumanVendor() : void
      {
         this.closeUi();
      }
      
      private function onAcceptTax() : void
      {
         this._askToSwitchToVendor = true;
         this.closeUi();
      }
      
      private function onRefuseTax() : void
      {
      }
      
      private function openUi(param1:String = null) : void
      {
         var _loc2_:String = null;
         this._sellerName = param1;
         for each(_loc2_ in this._uiToOpen)
         {
            switch(_loc2_)
            {
               case UIEnum.HUMAN_VENDOR_STOCK:
                  this.uiApi.loadUi(_loc2_,_loc2_,{
                     "playerName":param1,
                     "objects":this._storedObjectsInfos
                  });
                  break;
               case UIEnum.MYSELF_VENDOR_STOCK:
                  this.uiApi.loadUi(_loc2_,_loc2_,this._storedObjectsInfos);
                  break;
               case UIEnum.MYSELF_VENDOR:
               case UIEnum.HUMAN_VENDOR:
                  this.uiApi.loadUi(_loc2_,_loc2_);
            }
         }
         this.sysApi.disableWorldInteraction();
      }
      
      private function closeUi() : void
      {
         this._askShowUI = false;
         this.sysApi.enableWorldInteraction();
         if(this.uiApi.getUi(UIEnum.MYSELF_VENDOR))
         {
            this.uiApi.unloadUi(UIEnum.MYSELF_VENDOR);
         }
         if(this.uiApi.getUi(UIEnum.HUMAN_VENDOR_STOCK))
         {
            this.uiApi.unloadUi(UIEnum.HUMAN_VENDOR_STOCK);
         }
         if(this.uiApi.getUi(UIEnum.MYSELF_VENDOR_STOCK))
         {
            this.uiApi.unloadUi(UIEnum.MYSELF_VENDOR_STOCK);
         }
         if(this.uiApi.getUi(UIEnum.HUMAN_VENDOR))
         {
            this.uiApi.unloadUi(UIEnum.HUMAN_VENDOR);
         }
         if(this._askToSwitchToVendor)
         {
            this.sysApi.sendAction(new ExchangeStartAsVendorRequest());
            this._askToSwitchToVendor = false;
         }
         else if(this.playerApi.isInExchange())
         {
            this.sysApi.sendAction(new LeaveShopStock());
         }
      }
   }
}
