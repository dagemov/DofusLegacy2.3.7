<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Mount</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.mount.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="mount" exclusive="false" permanent="false" />

	<uis group="mount">
		<ui name="mountInfo" file="xml/mountInfo.xml" class="ui::MountInfo" />
	</uis>
	<uis group="mount2">
    	<ui name="mountInfo" file="xml/mountInfo.xml" class="ui::MountInfo" />
    	<ui name="mountAncestors" file="xml/mountAncestors.xml" class="ui::MountAncestors" />
    	<ui name="mountPaddock" file="xml/mountPaddock.xml" class="ui::MountPaddock" />
    	<ui name="barnItem" file="xml/items/barnXmlItem.xml" class="ui.items::BarnItem" />
    	<ui name="inventoryItem" file="xml/items/inventoryXmlItem.xml" class="ui.items::InventoryItem" />
    	<ui name="paddockSellBuy" file="xml/paddockSellBuy.xml" class="ui::PaddockSellBuy" />
    	<ui name="mountFeed" file="xml/mountFeed.xml" class="ui::MountFeed" />
	</uis>
    
	<script>Mount.swf</script>
    
</module> 