package d2api
{
   import d2data.Job;
   import d2data.Recipe;
   import d2data.Skill;
   
   public class JobsApi
   {
      
      public function JobsApi()
      {
         super();
      }
      
      [Trusted]
      public function destroy() : void
      {
      }
      
      [Untrusted]
      public function getKnownJobs() : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobSkills(param1:Job) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobSkillType(param1:Job, param2:Skill) : String
      {
         return null;
      }
      
      [Untrusted]
      public function getJobCollectSkillInfos(param1:Job, param2:Skill) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getMaxSlotsByJobId(param1:int) : int
      {
         return 0;
      }
      
      [Untrusted]
      public function getJobCraftSkillInfos(param1:Job, param2:Skill) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobExperience(param1:Job) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getSkillFromId(param1:int) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobRecipes(param1:Job, param2:Object = null, param3:Skill = null, param4:String = null) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getRecipe(param1:uint) : Recipe
      {
         return null;
      }
      
      [Untrusted]
      public function getRecipesList(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobName(param1:uint) : String
      {
         return null;
      }
      
      [Untrusted]
      public function getJob(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobCrafterDirectorySettingsById(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getJobCrafterDirectorySettingsByIndex(param1:uint) : Object
      {
         return null;
      }
      
      [Untrusted]
      public function getUsableSkillsInMap(param1:int) : Object
      {
         return null;
      }
   }
}

