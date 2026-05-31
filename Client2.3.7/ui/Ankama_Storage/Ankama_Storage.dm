<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Storage</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.storage.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>
    
	<uiGroup name="storage" 	exclusive="false"	permanent="false" />
	
	<uis>
		<ui name="bank" 			file="ui/storageBank.xml" 	class="ui::BankUi" />
		<ui name="equipment" 			file="ui/equipmentUi.xml" 	class="ui::EquipmentUi" />
		<ui name="preset" 			file="ui/presetUi.xml" 	class="ui::PresetUi" />
	</uis>
	
    <uis group="storage">
        <ui name="storage" 			file="ui/storage.xml"	 	class="ui::StorageUi"/>
        <ui name="livingObject" 	file="ui/livingObject.xml" 	class="ui::LivingObject"/>
    </uis>

    <script>Storage.swf</script>
    
</module> 