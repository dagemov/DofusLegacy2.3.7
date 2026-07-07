!macro customInit
  DetailPrint "Cerrando Onelauncher si esta en ejecucion..."
  nsExec::Exec 'taskkill /F /IM Onelauncher.exe /T'
  nsExec::Exec 'taskkill /F /IM electron.exe /T'
  Sleep 1000
!macroend
