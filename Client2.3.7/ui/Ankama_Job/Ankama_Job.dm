<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Job</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.job.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="craft" exclusive="false" permanent="false" />
	<uiGroup name="smithMagic" exclusive="false" permanent="false" />
	<uiGroup name="crafterForm" exclusive="false" permanent="false" />
	<uiGroup name="crafterList" exclusive="false" permanent="false" />

    <uis group="craft">
        <ui name="craft" 				file="xml/craft.xml"					class="ui::Craft" />
        <ui name="craftCoop" 			file="xml/craftCoop.xml"				class="ui::CraftCoop" />
    </uis>

    <uis group="smithMagic">
        <ui name="smithMagic" 			file="xml/smithMagic.xml"				class="ui::SmithMagic" />
        <ui name="smithMagicCoop" 		file="xml/smithMagicCoop.xml"			class="ui::SmithMagicCoop" />
    </uis>

    <uis group="crafterForm">
        <ui name="crafterForm" 			file="xml/crafterForm.xml"				class="ui::CrafterForm" />
    </uis>

    <uis group="crafterList">
        <ui name="crafterList" 			file="xml/crafterList.xml"				class="ui::CrafterList" />
    </uis>

    <uis>
        <ui name="jobLevelUp" 			file="xml/jobLevelUp.xml"				class="ui::JobLevelUp" />
        <ui name="crafterXmlItem" 		file="xml/items/crafterXmlItem.xml"		class="ui.items::CrafterXmlItem" />
    </uis>
    
    <script>Job.swf</script>
    
</module> 