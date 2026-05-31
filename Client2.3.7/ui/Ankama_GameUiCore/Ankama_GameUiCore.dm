<module>
    <!-- Information about the module -->
    <header>
        <!-- Name displayed in modules list -->
        <name>GameUiCore</name>
        
        <!-- Module's version -->
        <version>0.1</version>

        <!-- Last Dofus version that works with -->
        <dofusVersion>2.0</dofusVersion>

        <!-- Author of the module -->
        <author>Ankama</author>

        <!-- A short description -->
        <shortDescription>ui.module.gameuicore.shortDesc</shortDescription>

        <!-- Detailled description -->
        <description></description>
	</header>
    
    <uis>
        <ui name="banner" file="xml/banner.xml" class="ui::Banner" />
        <ui name="chat" file="xml/chat.xml" class="ui::Chat" />
        <ui name="smileys" file="xml/smileys.xml" class="ui::Smileys" />
        <ui name="mapInfo" file="xml/mapInfo.xml" class="ui::MapInfo" />
        <ui name="mainMenu" file="xml/mainMenu.xml" class="ui::MainMenu"/>
        <ui name="payZone" file="xml/payZone.xml" class="ui::PayZone"/>
        <ui name="buffUi"	file="xml/buffUi.xml" class="ui::BuffUi" />
        <ui name="zoomUi"	file="xml/zoom.xml" class="ui::Zoom" />
        <ui name="cinematic"	file="xml/cinematic.xml" class="ui::Cinematic" />
        <ui name="cinematicOverlay"	file="xml/cinematicOverlay.xml" class="ui::CinematicOverlay" />
        <ui name="downloadUi"	file="xml/downloadUi.xml" class="ui::DownloadUi" />
        
        <ui name="hardcoreDeath" file="xml/hardcoreDeath.xml" class="ui::HardcoreDeath"/>
        
        <ui name="report" 			file="xml/report.xml" class="ui::Report" />
    </uis>
    
    <shortcuts>shortcuts.xml</shortcuts>
    
    <script>GameUiCore.swf</script>
    
</module> 