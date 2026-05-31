<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Grimoire</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.grimoire.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>
	
	<uiGroup name="grimoire" exclusive="true" permanent="false" />
	
	<uis group="grimoire">
		<ui name="book" 				file="ui/book.xml" 						class="ui::Book"/>
		<ui name="spellTab" 			file="ui/spellTab.xml" 					class="ui::SpellTab"/>
		<ui name="objectTab" 		    file="ui/objectTab.xml" 				class="ui::ObjectTab"/>
		<ui name="alignmentTab"		file="ui/alignmentTab.xml"				class="ui::AlignmentTab"/>
		<ui name="emoteTab" 			file="ui/emoteTab.xml"					class="ui::EmoteTab" />
		<ui name="bestiaryTab" 			file="ui/bestiaryTab.xml"				class="ui::BestiaryTab"	/>
		<ui name="questTab" 			file="ui/questTab.xml"					class="ui::QuestTab"/>
		<ui name="jobTab" 				file="ui/jobTab.xml"					class="ui::JobTab"/>
		<ui name="jobCraftOptions" 		file="ui/jobCraftOptions.xml"			class="ui::JobCraftOptions"/>
		<ui name="recipeItem" 			file="ui/items/recipeItem.xml" 			class="ui.items::RecipeItem" />
		<ui name="skillItem" 			file="ui/items/skillItem.xml" 			class="ui.items::SkillItem" />
		<ui name="questItem"		    file="ui/items/questItem.xml"	        class="ui.items::QuestItem"/>
		<ui name="questObjectivesItem"	file="ui/items/questObjectivesItem.xml"	class="ui.items::QuestObjectivesItem"/>
		<ui name="giftXmlItem"		file="ui/items/giftXmlItem.xml"		class="ui.items::GiftXmlItem" />
	</uis>
	
	<script>Grimoire.swf</script>
	
</module> 