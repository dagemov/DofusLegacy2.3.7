package d2api
{
   import d2components.WebBrowser;
   import d2data.Server;
   import d2network.Version;
   
   public class SystemApi
   {
      
      public function SystemApi()
      {
         super();
      }
      
      [Trusted]
      public function destroy() : void
      {
      }
      
      [Untrusted]
      public function isInGame() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function addHook(param1:Class, param2:Function) : void
      {
      }
      
      [Untrusted]
      public function removeHook(param1:Class) : void
      {
      }
      
      [Untrusted]
      public function createHook(param1:String) : void
      {
      }
      
      [Untrusted]
      public function dispatchHook(param1:Class, ... rest) : void
      {
      }
      
      [Untrusted]
      public function sendAction(param1:Object) : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function log(param1:uint, param2:*) : void
      {
      }
      
      [Trusted]
      public function setConfigEntry(param1:String, param2:*) : void
      {
      }
      
      [Trusted]
      public function getConfigEntry(param1:String) : *
      {
         return null;
      }
      
      [Trusted]
      public function getEnum(param1:String) : Object
      {
         return null;
      }
      
      [Trusted]
      public function isEventMode() : Boolean
      {
         return false;
      }
      
      [Trusted]
      public function isCharacterCreationAllowed() : Boolean
      {
         return false;
      }
      
      [Trusted]
      public function getConfigKey(param1:String) : *
      {
         return null;
      }
      
      [Trusted]
      public function goToUrl(param1:String) : void
      {
      }
      
      [Trusted]
      public function getPlayerManager() : Object
      {
         return null;
      }
      
      [Trusted]
      public function getPort() : uint
      {
         return 0;
      }
      
      [Trusted]
      public function setPort(param1:uint) : void
      {
      }
      
      [Untrusted]
      public function setData(param1:String, param2:*, param3:Boolean = false) : void
      {
      }
      
      [Untrusted]
      public function getSetData(param1:String, param2:*, param3:Boolean = false) : *
      {
         return null;
      }
      
      [Untrusted]
      public function setQualityIsEnable() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function getAirVersion() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function isAirVersionAvailable(param1:uint) : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function setAirVersion(param1:uint) : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function getOs() : String
      {
         return null;
      }
      
      [Untrusted]
      public function getData(param1:String, param2:Boolean = false) : *
      {
         return null;
      }
      
      [Untrusted]
      public function getOption(param1:String, param2:String) : *
      {
         return null;
      }
      
      [Untrusted]
      public function callbackHook(param1:Object, ... rest) : void
      {
      }
      
      [Untrusted]
      public function showWorld(param1:Boolean) : void
      {
      }
      
      [Untrusted]
      public function worldIsVisible() : Boolean
      {
         return false;
      }
      
      [Trusted]
      public function getConsoleAutoCompletion(param1:String, param2:Boolean) : String
      {
         return null;
      }
      
      [Trusted]
      public function getAutoCompletePossibilities(param1:String, param2:Boolean = false) : Object
      {
         return null;
      }
      
      [Trusted]
      public function getAutoCompletePossibilitiesOnParam(param1:String, param2:Boolean = false, param3:uint = 0, param4:Object = null) : Object
      {
         return null;
      }
      
      [Trusted]
      public function getCmdHelp(param1:String, param2:Boolean = false) : String
      {
         return null;
      }
      
      [Untrusted]
      public function startChrono(param1:String) : void
      {
      }
      
      [Untrusted]
      public function stopChrono() : void
      {
      }
      
      [Trusted]
      public function addEventListener(param1:Function, param2:String, param3:uint = 25) : void
      {
      }
      
      [Trusted]
      public function removeEventListener(param1:Function) : void
      {
      }
      
      [Trusted]
      public function disableWorldInteraction(param1:Boolean = true) : void
      {
      }
      
      [Trusted]
      public function enableWorldInteraction() : void
      {
      }
      
      [Trusted]
      public function hasWorldInteraction() : Boolean
      {
         return false;
      }
      
      [Trusted]
      public function hasRight() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function isFightContext() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function getEntityLookFromString(param1:String) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getCurrentVersion() : Version
      {
         return null;
      }
      
      [Untrusted]
      public function getBuildType() : uint
      {
         return 0;
      }
      
      [Untrusted]
      public function getCurrentLanguage() : String
      {
         return null;
      }
      
      [Trusted]
      public function clearCache(param1:Boolean = false) : void
      {
      }
      
      [Trusted]
      public function reset() : void
      {
      }
      
      [Untrusted]
      public function getCurrentServer() : Server
      {
         return null;
      }
      
      [Trusted]
      public function getGroundCacheSize() : Number
      {
         return 0;
      }
      
      [Trusted]
      public function clearGroundCache() : void
      {
      }
      
      [Trusted]
      public function zoom(param1:Number) : void
      {
      }
      
      [Trusted]
      public function getCurrentZoom() : Number
      {
         return 0;
      }
      
      [Trusted]
      public function goToThirdPartyLogin(param1:WebBrowser) : void
      {
      }
      
      [Trusted]
      public function goToOgrinePortal(param1:WebBrowser) : void
      {
      }
      
      [Trusted]
      public function goToSupportFAQ(param1:uint) : void
      {
      }
      
      [Trusted]
      public function goToChangelogPortal(param1:WebBrowser) : void
      {
      }
      
      [Trusted]
      public function goToCheckLink(param1:String, param2:uint, param3:String) : void
      {
      }
      
      [Trusted]
      public function refreshUrl(param1:WebBrowser, param2:uint = 0) : void
      {
      }
      
      [Trusted]
      public function execServerCmd(param1:String) : void
      {
      }
      
      [Trusted]
      public function mouseZoom(param1:Boolean = true) : void
      {
      }
      
      [Trusted]
      public function resetZoom() : void
      {
      }
      
      [Trusted]
      public function getMaxZoom() : uint
      {
         return 0;
      }
      
      [Trusted]
      public function optimize() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function hasPart(param1:String) : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function hasUpdaterConnection() : Boolean
      {
         return false;
      }
      
      [Untrusted]
      public function notifyUser(param1:Boolean) : void
      {
      }
      
      [Untrusted]
      public function setGameAlign(param1:String) : void
      {
      }
      
      [Untrusted]
      public function getGameAlign() : String
      {
         return null;
      }
      
      [Untrusted]
      public function getDirectoryContent(param1:String = ".") : Object
      {
         return null;
      }
   }
}

