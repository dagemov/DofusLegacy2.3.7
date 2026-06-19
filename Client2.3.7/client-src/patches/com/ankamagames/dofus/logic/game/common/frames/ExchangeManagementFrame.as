package com.ankamagames.dofus.logic.game.common.frames
{
   import com.ankamagames.berilia.managers.KernelEventsManager;
   import com.ankamagames.dofus.datacenter.npcs.Npc;
   import com.ankamagames.dofus.internalDatacenter.items.ItemWrapper;
   import com.ankamagames.dofus.kernel.Kernel;
   import com.ankamagames.dofus.kernel.net.ConnectionsHandler;
   import com.ankamagames.dofus.logic.common.actions.ChangeWorldInteractionAction;
   import com.ankamagames.dofus.logic.game.common.actions.LeaveDialogAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeAcceptAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeObjectMoveAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeObjectMoveKamaAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeObjectTransfertAllFromInvAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeObjectTransfertAllToInvAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeObjectTransfertListFromInvAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeObjectTransfertListToInvAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeReadyAction;
   import com.ankamagames.dofus.logic.game.common.actions.exchange.ExchangeRefuseAction;
   import com.ankamagames.dofus.logic.game.common.managers.InventoryManager;
   import com.ankamagames.dofus.logic.game.common.managers.PlayedCharacterManager;
   import com.ankamagames.dofus.logic.game.roleplay.actions.LeaveDialogRequestAction;
   import com.ankamagames.dofus.logic.game.roleplay.frames.RoleplayContextFrame;
   import com.ankamagames.dofus.logic.game.roleplay.frames.RoleplayEntitiesFrame;
   import com.ankamagames.dofus.logic.game.roleplay.frames.RoleplayMovementFrame;
   import com.ankamagames.dofus.misc.EntityLookAdapter;
   import com.ankamagames.dofus.misc.lists.ExchangeHookList;
   import com.ankamagames.dofus.misc.lists.HookList;
   import com.ankamagames.dofus.misc.lists.InventoryHookList;
   import com.ankamagames.dofus.network.enums.ExchangeTypeEnum;
   import com.ankamagames.dofus.network.messages.game.dialog.LeaveDialogMessage;
   import com.ankamagames.dofus.network.messages.game.dialog.LeaveDialogRequestMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeAcceptMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeIsReadyMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeLeaveMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeObjectMoveKamaMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeObjectMoveMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeObjectTransfertAllFromInvMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeObjectTransfertAllToInvMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeObjectTransfertListFromInvMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeObjectTransfertListToInvMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeReadyMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeRequestedTradeMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeStartOkNpcShopMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeStartOkNpcTradeMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeStartOkTaxCollectorMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeStartedMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeStartedWithPodsMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.exchanges.ExchangeStartedWithStorageMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.storage.StorageInventoryContentMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.storage.StorageKamasUpdateMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.storage.StorageObjectRemoveMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.storage.StorageObjectUpdateMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.storage.StorageObjectsRemoveMessage;
   import com.ankamagames.dofus.network.messages.game.inventory.storage.StorageObjectsUpdateMessage;
   import com.ankamagames.dofus.network.types.game.context.GameContextActorInformations;
   import com.ankamagames.dofus.network.types.game.context.roleplay.GameRolePlayNamedActorInformations;
   import com.ankamagames.dofus.network.types.game.context.roleplay.GameRolePlayNpcInformations;
   import com.ankamagames.dofus.network.types.game.data.items.ObjectItem;
   import com.ankamagames.dofus.network.types.game.data.items.ObjectItemMinimalInformation;
   import com.ankamagames.jerakine.logger.Log;
   import com.ankamagames.jerakine.logger.Logger;
   import com.ankamagames.jerakine.messages.Frame;
   import com.ankamagames.jerakine.messages.Message;
   import com.ankamagames.jerakine.network.IServerConnection;
   import com.ankamagames.jerakine.types.enums.Priority;
   import com.ankamagames.tiphon.types.look.TiphonEntityLook;
   import flash.utils.getQualifiedClassName;
   
   public class ExchangeManagementFrame implements Frame
   {
      
      protected static const _log:Logger = Log.getLogger(getQualifiedClassName(ExchangeManagementFrame));
      
      private var _sourceInformations:GameRolePlayNamedActorInformations;
      
      private var _targetInformations:GameRolePlayNamedActorInformations;
      
      private var _meReady:Boolean = false;
      
      private var _youReady:Boolean = false;
      
      private var _exchangeInventory:Array;
      
      public function ExchangeManagementFrame()
      {
         super();
      }
      
      public function get priority() : int
      {
         return Priority.NORMAL;
      }
      
      private function get roleplayContextFrame() : RoleplayContextFrame
      {
         return Kernel.getWorker().getFrame(RoleplayContextFrame) as RoleplayContextFrame;
      }
      
      private function get roleplayEntitiesFrame() : RoleplayEntitiesFrame
      {
         return Kernel.getWorker().getFrame(RoleplayEntitiesFrame) as RoleplayEntitiesFrame;
      }
      
      private function get roleplayMovementFrame() : RoleplayMovementFrame
      {
         return Kernel.getWorker().getFrame(RoleplayMovementFrame) as RoleplayMovementFrame;
      }
      
      public function initMountStock(param1:Vector.<ObjectItem>) : void
      {
         InventoryManager.getInstance().bankInventory.initializeFromObjectItems(param1);
         InventoryManager.getInstance().bankInventory.releaseHooks();
      }
      
      public function processExchangeRequestedTradeMessage(param1:ExchangeRequestedTradeMessage) : void
      {
         var _loc4_:SocialFrame = null;
         var _loc5_:LeaveDialogAction = null;
         if(param1.exchangeType != ExchangeTypeEnum.PLAYER_TRADE)
         {
            return;
         }
         this._sourceInformations = this.roleplayEntitiesFrame.getEntityInfos(param1.source) as GameRolePlayNamedActorInformations;
         this._targetInformations = this.roleplayEntitiesFrame.getEntityInfos(param1.target) as GameRolePlayNamedActorInformations;
         var _loc2_:String = this._sourceInformations.name;
         var _loc3_:String = this._targetInformations.name;
         if(param1.source == PlayedCharacterManager.getInstance().id)
         {
            this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeRequestCharacterFromMe,_loc2_,_loc3_);
         }
         else
         {
            _loc4_ = Kernel.getWorker().getFrame(SocialFrame) as SocialFrame;
            if(Boolean(_loc4_) && _loc4_.isIgnored(_loc2_))
            {
               _loc5_ = new LeaveDialogAction();
               Kernel.getWorker().process(_loc5_);
               return;
            }
            this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeRequestCharacterToMe,_loc3_,_loc2_);
         }
      }
      
      public function processExchangeStartOkNpcTradeMessage(param1:ExchangeStartOkNpcTradeMessage) : void
      {
         var _loc2_:String = PlayedCharacterManager.getInstance().infos.name;
         var _loc3_:int = this.roleplayEntitiesFrame.getEntityInfos(param1.npcId).contextualId;
         var _loc4_:Npc = Npc.getNpcById(_loc3_);
         var _loc5_:String = Npc.getNpcById((this.roleplayEntitiesFrame.getEntityInfos(param1.npcId) as GameRolePlayNpcInformations).npcId).name;
         var _loc6_:TiphonEntityLook = EntityLookAdapter.getRiderLook(PlayedCharacterManager.getInstance().infos.entityLook);
         var _loc7_:TiphonEntityLook = EntityLookAdapter.getRiderLook(this.roleplayContextFrame.entitiesFrame.getEntityInfos(param1.npcId).look);
         var _loc8_:ExchangeStartOkNpcTradeMessage = param1 as ExchangeStartOkNpcTradeMessage;
         PlayedCharacterManager.getInstance().isInExchange = true;
         this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStartOkNpcTrade,_loc8_.npcId,_loc2_,_loc5_,_loc6_,_loc7_);
         this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStartedType,ExchangeTypeEnum.NPC_TRADE);
      }
      
      public function process(param1:Message) : Boolean
      {
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:ExchangeStartedWithStorageMessage = null;
         var _loc5_:int = 0;
         var _loc6_:ExchangeStartedMessage = null;
         var _loc7_:StorageInventoryContentMessage = null;
         var _loc8_:ExchangeStartOkTaxCollectorMessage = null;
         var _loc9_:StorageObjectUpdateMessage = null;
         var _loc10_:ObjectItem = null;
         var _loc11_:ItemWrapper = null;
         var _loc12_:StorageObjectRemoveMessage = null;
         var _loc13_:StorageObjectsUpdateMessage = null;
         var _loc14_:StorageObjectsRemoveMessage = null;
         var _loc15_:StorageKamasUpdateMessage = null;
         var _loc16_:ExchangeAcceptMessage = null;
         var _loc17_:LeaveDialogRequestMessage = null;
         var _loc18_:ExchangeReadyAction = null;
         var _loc19_:ExchangeReadyMessage = null;
         var _loc20_:ExchangeIsReadyMessage = null;
         var _loc21_:String = null;
         var _loc22_:ExchangeObjectMoveAction = null;
         var _loc23_:ExchangeObjectMoveMessage = null;
         var _loc24_:ExchangeObjectMoveKamaAction = null;
         var _loc25_:ExchangeObjectMoveKamaMessage = null;
         var _loc26_:ExchangeObjectTransfertAllToInvAction = null;
         var _loc27_:ExchangeObjectTransfertAllToInvMessage = null;
         var _loc28_:ExchangeObjectTransfertListToInvAction = null;
         var _loc29_:ExchangeObjectTransfertAllFromInvAction = null;
         var _loc30_:ExchangeObjectTransfertAllFromInvMessage = null;
         var _loc31_:ExchangeObjectTransfertListFromInvAction = null;
         var _loc32_:ExchangeStartOkNpcShopMessage = null;
         var _loc33_:GameContextActorInformations = null;
         var _loc34_:TiphonEntityLook = null;
         var _loc35_:Array = null;
         var _loc36_:String = null;
         var _loc37_:String = null;
         var _loc38_:TiphonEntityLook = null;
         var _loc39_:TiphonEntityLook = null;
         var _loc40_:ExchangeStartedWithPodsMessage = null;
         var _loc41_:int = 0;
         var _loc42_:int = 0;
         var _loc43_:int = 0;
         var _loc44_:int = 0;
         var _loc45_:int = 0;
         var _loc46_:ObjectItem = null;
         var _loc47_:ObjectItem = null;
         var _loc48_:ItemWrapper = null;
         var _loc49_:uint = 0;
         var _loc50_:ExchangeObjectTransfertListToInvMessage = null;
         var _loc51_:ExchangeObjectTransfertListFromInvMessage = null;
         var _loc52_:ObjectItemMinimalInformation = null;
         var _loc53_:ItemWrapper = null;
         var _loc54_:Npc = null;
         switch(true)
         {
            case param1 is ExchangeStartedWithStorageMessage:
               _loc4_ = param1 as ExchangeStartedWithStorageMessage;
               PlayedCharacterManager.getInstance().isInExchange = true;
               _loc5_ = int(_loc4_.storageMaxSlot);
               this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeBankStartedWithStorage,ExchangeTypeEnum.STORAGE,_loc5_);
               return true;
            case param1 is ExchangeStartedMessage:
               _loc6_ = param1 as ExchangeStartedMessage;
               PlayedCharacterManager.getInstance().isInExchange = true;
               switch(_loc6_.exchangeType)
               {
                  case ExchangeTypeEnum.PLAYER_TRADE:
                     _loc36_ = this._sourceInformations.name;
                     _loc37_ = this._targetInformations.name;
                     _loc38_ = EntityLookAdapter.getRiderLook(this._sourceInformations.look);
                     _loc39_ = EntityLookAdapter.getRiderLook(this._targetInformations.look);
                     if(_loc6_.getMessageId() == ExchangeStartedWithPodsMessage.protocolId)
                     {
                        _loc40_ = param1 as ExchangeStartedWithPodsMessage;
                     }
                     _loc41_ = -1;
                     _loc42_ = -1;
                     _loc43_ = -1;
                     _loc44_ = -1;
                     if(_loc40_ != null)
                     {
                        if(_loc40_.firstCharacterId == this._sourceInformations.contextualId)
                        {
                           _loc41_ = int(_loc40_.firstCharacterCurrentWeight);
                           _loc42_ = int(_loc40_.secondCharacterCurrentWeight);
                           _loc43_ = int(_loc40_.firstCharacterMaxWeight);
                           _loc44_ = int(_loc40_.secondCharacterMaxWeight);
                        }
                        else
                        {
                           _loc42_ = int(_loc40_.firstCharacterCurrentWeight);
                           _loc41_ = int(_loc40_.secondCharacterCurrentWeight);
                           _loc44_ = int(_loc40_.firstCharacterMaxWeight);
                           _loc43_ = int(_loc40_.secondCharacterMaxWeight);
                        }
                     }
                     if(PlayedCharacterManager.getInstance().id == _loc40_.firstCharacterId)
                     {
                        _loc45_ = _loc40_.secondCharacterId;
                     }
                     else
                     {
                        _loc45_ = _loc40_.firstCharacterId;
                     }
                     _log.debug("look : " + _loc38_.toString() + "    " + _loc39_.toString());
                     this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStarted,_loc36_,_loc37_,_loc38_,_loc39_,_loc41_,_loc42_,_loc43_,_loc44_,_loc45_);
                     this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStartedType,_loc6_.exchangeType);
                     return true;
                  case ExchangeTypeEnum.STORAGE:
                     this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStartedType,_loc6_.exchangeType);
                     return true;
                  case ExchangeTypeEnum.TAXCOLLECTOR:
                     this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStartedType,_loc6_.exchangeType);
                     return true;
                  default:
                     return false;
               }
               break;
            case param1 is StorageInventoryContentMessage:
               _loc7_ = param1 as StorageInventoryContentMessage;
               InventoryManager.getInstance().bankInventory.kamas = _loc7_.kamas;
               InventoryManager.getInstance().bankInventory.initializeFromObjectItems(_loc7_.objects);
               InventoryManager.getInstance().bankInventory.releaseHooks();
               return false;
            case param1 is ExchangeStartOkTaxCollectorMessage:
               _loc8_ = param1 as ExchangeStartOkTaxCollectorMessage;
               InventoryManager.getInstance().bankInventory.kamas = _loc8_.goldInfo;
               InventoryManager.getInstance().bankInventory.initializeFromObjectItems(_loc8_.objectsInfos);
               InventoryManager.getInstance().bankInventory.releaseHooks();
               return false;
            case param1 is StorageObjectUpdateMessage:
               _loc9_ = param1 as StorageObjectUpdateMessage;
               _loc10_ = _loc9_.object;
               _loc11_ = ItemWrapper.create(_loc10_.position,_loc10_.objectUID,_loc10_.objectGID,_loc10_.quantity,_loc10_.effects);
               InventoryManager.getInstance().bankInventory.modifyItem(_loc11_);
               InventoryManager.getInstance().bankInventory.releaseHooks();
               return false;
            case param1 is StorageObjectRemoveMessage:
               _loc12_ = param1 as StorageObjectRemoveMessage;
               InventoryManager.getInstance().bankInventory.removeItem(_loc12_.objectUID);
               InventoryManager.getInstance().bankInventory.releaseHooks();
               return false;
            case param1 is StorageObjectsUpdateMessage:
               _loc13_ = param1 as StorageObjectsUpdateMessage;
               for each(_loc46_ in _loc13_.objectList)
               {
                  _loc47_ = _loc46_;
                  _loc48_ = ItemWrapper.create(_loc47_.position,_loc47_.objectUID,_loc47_.objectGID,_loc47_.quantity,_loc47_.effects);
                  InventoryManager.getInstance().bankInventory.modifyItem(_loc48_);
               }
               InventoryManager.getInstance().bankInventory.releaseHooks();
               return false;
            case param1 is StorageObjectsRemoveMessage:
               _loc14_ = param1 as StorageObjectsRemoveMessage;
               for each(_loc49_ in _loc14_.objectUIDList)
               {
                  InventoryManager.getInstance().bankInventory.removeItem(_loc49_);
               }
               InventoryManager.getInstance().bankInventory.releaseHooks();
               return false;
            case param1 is StorageKamasUpdateMessage:
               _loc15_ = param1 as StorageKamasUpdateMessage;
               InventoryManager.getInstance().bankInventory.kamas = _loc15_.kamasTotal;
               KernelEventsManager.getInstance().processCallback(InventoryHookList.StorageKamasUpdate,_loc15_.kamasTotal);
               return false;
            case param1 is ExchangeAcceptAction:
               _loc16_ = new ExchangeAcceptMessage();
               _loc16_.initExchangeAcceptMessage();
               this._serverConnection.send(_loc16_);
               return true;
            case param1 is ExchangeRefuseAction:
               _loc17_ = new LeaveDialogRequestMessage();
               _loc17_.initLeaveDialogRequestMessage();
               this._serverConnection.send(_loc17_);
               return true;
            case param1 is ExchangeReadyAction:
               _loc18_ = param1 as ExchangeReadyAction;
               _loc19_ = new ExchangeReadyMessage();
               _loc19_.initExchangeReadyMessage(_loc18_.isReady);
               this._serverConnection.send(_loc19_);
               return true;
            case param1 is ExchangeIsReadyMessage:
               _loc20_ = param1 as ExchangeIsReadyMessage;
               _loc21_ = (this.roleplayEntitiesFrame.getEntityInfos(_loc20_.id) as GameRolePlayNamedActorInformations).name;
               this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeIsReady,_loc21_,_loc20_.ready);
               return true;
            case param1 is ExchangeObjectMoveAction:
               _loc22_ = param1 as ExchangeObjectMoveAction;
               _loc23_ = new ExchangeObjectMoveMessage();
               _loc23_.initExchangeObjectMoveMessage(_loc22_.objectUID,_loc22_.quantity);
               this._serverConnection.send(_loc23_);
               return true;
            case param1 is ExchangeObjectMoveKamaAction:
               _loc24_ = param1 as ExchangeObjectMoveKamaAction;
               _loc25_ = new ExchangeObjectMoveKamaMessage();
               _loc25_.initExchangeObjectMoveKamaMessage(_loc24_.kamas);
               this._serverConnection.send(_loc25_);
               return true;
            case param1 is ExchangeObjectTransfertAllToInvAction:
               _loc26_ = param1 as ExchangeObjectTransfertAllToInvAction;
               _loc27_ = new ExchangeObjectTransfertAllToInvMessage();
               _loc27_.initExchangeObjectTransfertAllToInvMessage();
               this._serverConnection.send(_loc27_);
               return true;
            case param1 is ExchangeObjectTransfertListToInvAction:
               _loc28_ = param1 as ExchangeObjectTransfertListToInvAction;
               if(_loc28_.ids.length != 0)
               {
                  _loc50_ = new ExchangeObjectTransfertListToInvMessage();
                  _loc50_.initExchangeObjectTransfertListToInvMessage(_loc28_.ids);
                  this._serverConnection.send(_loc50_);
               }
               return true;
            case param1 is ExchangeObjectTransfertAllFromInvAction:
               _loc29_ = param1 as ExchangeObjectTransfertAllFromInvAction;
               _loc30_ = new ExchangeObjectTransfertAllFromInvMessage();
               _loc30_.initExchangeObjectTransfertAllFromInvMessage();
               this._serverConnection.send(_loc30_);
               return true;
            case param1 is ExchangeObjectTransfertListFromInvAction:
               _loc31_ = param1 as ExchangeObjectTransfertListFromInvAction;
               _log.debug("ExchangeObjectTransfertListFromInvAction : " + _loc31_.ids.length);
               if(_loc31_.ids.length != 0)
               {
                  _loc51_ = new ExchangeObjectTransfertListFromInvMessage();
                  _loc51_.initExchangeObjectTransfertListFromInvMessage(_loc31_.ids);
                  this._serverConnection.send(_loc51_);
               }
               return true;
            case param1 is ExchangeStartOkNpcShopMessage:
               _loc32_ = param1 as ExchangeStartOkNpcShopMessage;
               PlayedCharacterManager.getInstance().isInExchange = true;
               Kernel.getWorker().process(ChangeWorldInteractionAction.create(false,true));
               _loc33_ = this.roleplayContextFrame.entitiesFrame.getEntityInfos(_loc32_.npcSellerId);
               if(_loc33_ != null)
               {
                  _loc34_ = EntityLookAdapter.fromNetwork(_loc33_.look);
               }
               else
               {
                  _loc54_ = Npc.getNpcById(_loc32_.npcSellerId);
                  if(_loc54_ != null && _loc54_.look)
                  {
                     _loc34_ = TiphonEntityLook.fromString(_loc54_.look);
                  }
                  else
                  {
                     _loc34_ = new TiphonEntityLook();
                  }
               }
               _loc35_ = new Array();
               for each(_loc52_ in _loc32_.objectsInfos)
               {
                  _loc53_ = ItemWrapper.create(63,0,_loc52_.objectGID,0,_loc52_.effects,false);
                  _loc53_.price = _loc52_.objectPrice;
                  _loc53_.criteria = _loc52_.buyCriterion;
                  _loc35_.push(_loc53_);
               }
               _loc35_.sortOn("price",Array.NUMERIC);
               this._kernelEventsManager.processCallback(ExchangeHookList.ExchangeStartOkNpcShop,_loc32_.npcSellerId,_loc35_,_loc34_);
               return true;
            case param1 is LeaveDialogRequestAction:
               ConnectionsHandler.getConnection().send(new LeaveDialogRequestMessage());
               return true;
            case param1 is LeaveDialogMessage:
               Kernel.getWorker().removeFrame(this);
               return true;
            case param1 is ExchangeLeaveMessage:
               this._kernelEventsManager.processCallback(HookList.LeaveDialog);
         }
         return false;
      }
      
      private function proceedExchange() : void
      {
      }
      
      public function pushed() : Boolean
      {
         return true;
      }
      
      public function pulled() : Boolean
      {
         this._exchangeInventory = null;
         return true;
      }
      
      private function get _kernelEventsManager() : KernelEventsManager
      {
         return KernelEventsManager.getInstance();
      }
      
      private function get _serverConnection() : IServerConnection
      {
         return ConnectionsHandler.getConnection();
      }
   }
}
