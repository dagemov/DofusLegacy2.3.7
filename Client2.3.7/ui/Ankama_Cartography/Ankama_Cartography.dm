<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Cartography</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.cartography.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="cartography" exclusive="true" permanent="false" />

    <uis group="cartography">
        <ui name="cartographyUi" file="xml/cartographyUi.xml" class="ui::CartographyUi"/>
    </uis>
     <cachedFiles>
        <path>images/maps</path>
    </cachedFiles>
    <script>Cartography.swf</script>
</module>