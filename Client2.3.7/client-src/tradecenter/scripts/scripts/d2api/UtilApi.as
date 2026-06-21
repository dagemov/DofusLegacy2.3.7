package d2api
{
   public class UtilApi
   {
      
      public function UtilApi()
      {
         super();
      }
      
      [Trusted]
      public function destroy() : void
      {
      }
      
      [Untrusted]
      public function callWithParameters(param1:Function, param2:Object) : void
      {
      }
      
      [Untrusted]
      public function callConstructorWithParameters(param1:Class, param2:Object) : *
      {
         return null;
      }
      
      [Untrusted]
      public function callRWithParameters(param1:Function, param2:Object) : *
      {
         return null;
      }
      
      [Untrusted]
      public function kamasToString(param1:Number, param2:String = "K") : String
      {
         return null;
      }
      
      [Untrusted]
      public function formateIntToString(param1:Number) : String
      {
         return null;
      }
      
      [Untrusted]
      public function stringToKamas(param1:String, param2:String = "K") : int
      {
         return 0;
      }
      
      [Untrusted]
      public function getTextWithParams(param1:int, param2:Object, param3:String = "%") : String
      {
         return null;
      }
   }
}

