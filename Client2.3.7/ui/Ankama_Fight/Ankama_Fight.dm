<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Fight</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.fight.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="fightResult" exclusive="true" permanent="true" />

    <uis group="fightResult">
        <ui name="fightResult" file="ui/fightResult.xml" class="ui::FightResult" />
    </uis>

    <uis>
        <ui name="timeline" file="ui/timeline.xml" class="ui::Timeline" />
        <ui name="buffs" file="ui/buffs.xml" class="ui::Buffs" />
        <ui name="turnStart" file="ui/turnStart.xml" class="ui::TurnStart" />
        <ui name="fighterInfo" file="ui/fighterInfo.xml" class="ui::FighterInfo" />        
        <ui name="challengeDisplay"		file="ui/challengeDisplay.xml" 		class="ui::ChallengeDisplay" />
        
        <ui name="fighterPvmXmlItem" file="ui/items/fighterPvmXmlItem.xml" class="ui.items::FighterPvmXmlItem" />
    </uis>
    
    <script>Fight.swf</script>
    
</module> 