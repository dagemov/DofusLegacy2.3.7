// OnesvUpdater V1.0 | 2025
// By https://maestro-yaco.blogspot.com/ 
// Suport: https://discord.gg/yZnADDUKHx
// preload.js
const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('api', {
  // Verificar actualizaciones
  checkUpdates: () => ipcRenderer.invoke('check-updates'),

  // Descarga de archivos
  downloadFile: (data) => ipcRenderer.invoke('download-file', data),
  onDownloadProgress: (callback) =>
    ipcRenderer.on('download-progress', (event, data) => callback(data)),

  // Extracción de ZIP con progreso
  extractZip: (zipPath) => ipcRenderer.invoke('extract-zip', zipPath),
  onExtractProgress: (callback) =>
    ipcRenderer.on('extract-progress', (event, data) => callback(data)),

  // Actualizar versión local
  updateLocalVersion: (newVersion) => ipcRenderer.invoke('update-local-version', newVersion),

  // Lanzar juego
  launchGame: () => ipcRenderer.invoke('launch-game'),

  // Controles de ventana
  minimize: () => ipcRenderer.send('minimize-app'),
  close: () => ipcRenderer.send('close-app'),

  // Abrir URLs externas
  openUrl: (url) => ipcRenderer.send('open-url', url),

  // Cuenta (API OneLauncher)
  authLogin: (payload) => ipcRenderer.invoke('auth-login', payload),
  authRegister: (payload) => ipcRenderer.invoke('auth-register', payload),
  authLogout: () => ipcRenderer.invoke('auth-logout'),
  getSession: () => ipcRenderer.invoke('get-session'),
  checkApiHealth: () => ipcRenderer.invoke('check-api-health')
});
