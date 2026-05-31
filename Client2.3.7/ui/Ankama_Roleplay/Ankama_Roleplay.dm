<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Roleplay</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.roleplay.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="prismDefense" exclusive="true" permanent="true" />
	<uiGroup name="spectator" exclusive="false" permanent="false" />

    <uis group="prismDefense">
        <ui name="prismDefense" file="xml/prismDefense.xml" class="ui::PrismDefense" />
    </uis>
    
    <uis group="spectator">
        <ui name="spectatorUi" file="xml/spectatorUi.xml" class="ui::SpectatorUi" />
    </uis>

    <uis>
        <ui name="npcDialog" file="xml/npcDialog.xml" class="ui::NpcDialog" />
        <ui name="spellForget" file="xml/spellForget.xml" class="ui::SpellForget" />
        <ui name="levelUp" file="xml/LevelUp.xml" class="ui::LevelUpUi" />
        
        <ui name="spellForgetItem" file="xml/items/spellForgetItem.xml" class="ui.items::SpellForgetItem" />
        <ui name="fightItem" file="xml/items/fightItem.xml" class="ui.items::FightItem" />
    </uis>
    
    <script>Roleplay.swf</script>
    
</module> 