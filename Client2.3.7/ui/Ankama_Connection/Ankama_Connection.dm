<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>Connection</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.connection.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>
    
    <!-- module name-->
    <uis>
        <ui name="login" file="xml/login.xml" class="ui::Login" />
        <ui name="loginThirdParty" file="xml/loginThirdParty.xml" class="ui::LoginThirdParty" />
        <ui name="characterSelection" file="xml/characterSelection.xml" class="ui::CharacterSelection" />
        <ui name="characterCreation" file="xml/characterCreation.xml" class="ui::CharacterCreation" />
        <ui name="serverSelection" file="xml/serverSelection.xml" class="ui::ServerSelection" />
        <ui name="serverSimpleSelection" file="xml/serverSimpleSelection.xml" class="ui::ServerSimpleSelection" />
        <ui name="serverListSelection" file="xml/serverListSelection.xml" class="ui::ServerListSelection" />
        <ui name="serverForm" file="xml/serverForm.xml" class="ui::ServerForm" />
        <ui name="serverPopup" file="xml/serverPopup.xml" class="ui::ServerPopup" />
        <ui name="serverImgXmlItem" file="xml/item/serverImgXmlItem.xml" class="ui.items::ServerImgXmlItem" />
        <ui name="characterHeader" file="xml/characterHeader.xml" class="ui::CharacterHeader" />
        <ui name="pseudoChoice" file="xml/pseudoChoice.xml" class="ui::PseudoChoice" />
        <ui name="serverXmlItem" file="xml/serverXmlItem.xml" class="ui.items::ServerXmlItem" />
        <ui name="preGameMainMenu" file="xml/preGameMainMenu.xml" class="ui::PreGameMainMenu"/>
        <ui name="giftMenu" file="xml/giftMenu.xml" class="ui::GiftMenu"/>
        <ui name="giftCharacterSelectionItem" file="xml/giftCharacterSelectionItem.xml" class="ui.items::GiftCharacterSelectionItem" />
        <ui name="cinematiqueIntro" file="xml/cinematiqueIntro.xml" class="ui::CinematiqueIntro" />
        <ui name="tutorialSelection" file="xml/tutorialSelection.xml" class="ui::TutorialSelection" />
        <ui name="characterSelectionXmlItem" file="xml/item/characterSelectionXmlItem.xml" class="ui.items::CharacterSelectionXmlItem" />
        <ui name="unavailableCharacterPopup" file="xml/unavailableCharacterPopup.xml" class="ui.UnavailableCharacterPopup" />
        
        <ui name="secretPopup" file="xml/secretPopup.xml" class="ui::SecretPopup" />
        <ui name="userAgreement" file="xml/userAgreement.xml" class="ui::UserAgreement" />
    </uis>
    
    <script>Connection.swf</script>
    
</module>