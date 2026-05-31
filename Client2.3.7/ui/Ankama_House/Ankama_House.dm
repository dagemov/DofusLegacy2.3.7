<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>House</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.house.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="houseGuildManager" exclusive="true" permanent="false" />

    <uis group="houseGuildManager">
       <ui name="houseGuildManager"		file="ui/houseGuildManager.xml"		class="ui::HouseGuildManager" />
    </uis>

	<uiGroup name="houseSale" exclusive="true" permanent="false" />

    <uis group="houseSale">
       <ui name="houseSale" 			file="ui/houseSale.xml"				class="ui::HouseSale" />
    </uis>

    <uis>
       <ui name="houseManager" 			file="ui/houseManager.xml"			class="ui::HouseManager" />
    </uis>
    
    <script>House.swf</script>
    
</module> 