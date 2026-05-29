<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Social</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.social.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>

	<uiGroup name="guildCreator" exclusive="true" permanent="true" />
	<uiGroup name="socialBase" exclusive="true" permanent="false" />

    <uis group="guildCreator">
        <ui name="guildCreator" 				file="xml/guildCreator.xml" class="ui::GuildCreator" />
    </uis>

    <uis group="socialBase">
        <ui name="socialBase" 					file="xml/socialBase.xml" class="ui::SocialBase" />
        
        <ui name="friends" 						file="xml/friends.xml" class="ui::Friends" />
        <ui name="spouse" 						file="xml/spouse.xml" class="ui::Spouse" />
        <ui name="guild" 						file="xml/guild.xml" class="ui::Guild" />

        <ui name="friendXmlItem"				file="xml/item/friendXmlItem.xml" class="ui.items::FriendXmlItem" />
        <ui name="guildMemberXmlItem"			file="xml/item/guildMemberXmlItem.xml" class="ui.items::GuildMemberXmlItem" />
        <ui name="guildSpellItem"				file="xml/item/guildSpellItem.xml" class="ui.items::GuildSpellItem" /> 
        <ui name="guildMountXmlItem"			file="xml/item/guildMountXmlItem.xml" class="ui.items::GuildMountXmlItem" />  
        <ui name="guildPaddockXmlItem"			file="xml/item/guildPaddockXmlItem.xml" class="ui.items::GuildPaddockXmlItem" />  
        <ui name="guildHouseXmlItem"			file="xml/item/guildHouseXmlItem.xml" class="ui.items::GuildHouseXmlItem" />  
        <ui name="ponyXmlItem"					file="xml/item/ponyXmlItem.xml" class="ui.items::PonyXmlItem" />

        <ui name="guildMembers" 				file="xml/guildMembers.xml" class="ui::GuildMembers" />
        <ui name="guildMemberRights" 			file="xml/guildMemberRights.xml" class="ui::GuildMemberRights" />
        <ui name="guildPersonalization" 		file="xml/guildPersonalization.xml" class="ui::GuildPersonalization" />
        <ui name="guildTaxCollector" 			file="xml/guildTaxCollector.xml" class="ui::GuildTaxCollector" />
        <ui name="guildPaddock" 				file="xml/guildPaddock.xml" class="ui::GuildPaddock" />
        <ui name="guildHouses" 					file="xml/guildHouses.xml" class="ui::GuildHouses" />        
    </uis>
    
    <script>Social.swf</script>
    
</module> 