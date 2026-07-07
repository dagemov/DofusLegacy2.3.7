const { app } = require('electron');
const { autoUpdater } = require('electron-updater');
const { endpoints } = require('../config/endpoints');

let initialized = false;
const LAUNCHER_UPDATE_TIMEOUT_MS = Number(process.env.LAUNCHER_UPDATE_TIMEOUT_MS || 12000);

function sendToRenderer(mainWindow, channel, payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, payload);
  }
}

function configureAutoUpdater(mainWindow, logger = console) {
  if (initialized) return;
  initialized = true;

  autoUpdater.logger = logger;
  autoUpdater.autoDownload = true;
  autoUpdater.autoInstallOnAppQuit = true;
  autoUpdater.allowDowngrade = false;

  autoUpdater.on('checking-for-update', () => {
    logger.log('[Main:LauncherUpdate] Buscando actualizacion del launcher...');
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'checking',
      message: 'Buscando actualizacion del launcher...'
    });
  });

  autoUpdater.on('update-available', (info) => {
    logger.log('[Main:LauncherUpdate] Actualizacion disponible:', info?.version);
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'available',
      message: `Actualizacion del launcher ${info?.version || ''} disponible.`,
      version: info?.version
    });
  });

  autoUpdater.on('update-not-available', (info) => {
    logger.log('[Main:LauncherUpdate] Launcher al dia.', info?.version);
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'idle',
      message: 'Launcher al dia.'
    });
  });

  autoUpdater.on('download-progress', (progress) => {
    const percent = Math.round(progress.percent || 0);
    sendToRenderer(mainWindow, 'launcher-update-progress', {
      percent,
      transferred: progress.transferred,
      total: progress.total,
      bytesPerSecond: progress.bytesPerSecond
    });
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'downloading',
      message: `Descargando launcher ${percent}%`
    });
  });

  autoUpdater.on('update-downloaded', (info) => {
    logger.log('[Main:LauncherUpdate] Descarga lista, reinicio requerido:', info?.version);
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'downloaded',
      message: 'Actualizacion del launcher lista. Reiniciando...',
      version: info?.version
    });
  });

  autoUpdater.on('error', (error) => {
    logger.warn('[Main:LauncherUpdate] Error:', error?.message || error);
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'error',
      message: error?.message || 'Error al actualizar el launcher'
    });
  });
}

/**
 * Comprueba actualizaciones del instalador (electron-updater).
 * En desarrollo (npm start) no hace nada.
 * @returns {Promise<'ready'|'restart'>}
 */
async function runLauncherAutoUpdate(mainWindow, logger = console) {
  if (!app.isPackaged) {
    logger.log('[Main:LauncherUpdate] Modo desarrollo: sin auto-update del launcher.');
    sendToRenderer(mainWindow, 'launcher-update-status', {
      phase: 'dev',
      message: 'Modo desarrollo (sin auto-update del launcher).'
    });
    return 'ready';
  }

  configureAutoUpdater(mainWindow, logger);
  autoUpdater.setFeedURL({
    provider: 'generic',
    url: endpoints.launcherReleasesUrl
  });
  logger.log(`[Main:LauncherUpdate] Feed: ${endpoints.launcherReleasesUrl}`);

  return new Promise((resolve) => {
    let settled = false;
    const finish = (result) => {
      if (settled) return;
      settled = true;
      clearTimeout(timeoutId);
      resolve(result);
    };

    const timeoutId = setTimeout(() => {
      logger.warn(
        `[Main:LauncherUpdate] Timeout (${LAUNCHER_UPDATE_TIMEOUT_MS}ms). ` +
        `¿Esta activo ${endpoints.launcherReleasesUrl}latest.yml en el VPS?`
      );
      sendToRenderer(mainWindow, 'launcher-update-status', {
        phase: 'error',
        message: 'Servidor de actualizaciones del launcher no responde; continuando.'
      });
      finish('ready');
    }, LAUNCHER_UPDATE_TIMEOUT_MS);

    const onError = (error) => {
      logger.warn('[Main:LauncherUpdate] Continuando tras error:', error?.message || error);
      finish('ready');
    };

    autoUpdater.once('error', onError);
    autoUpdater.once('update-not-available', () => finish('ready'));

    autoUpdater.once('update-downloaded', () => {
      clearTimeout(timeoutId);
      logger.log('[Main:LauncherUpdate] Instalando actualizacion y reiniciando...');
      setTimeout(() => {
        autoUpdater.quitAndInstall(false, true);
      }, 1500);
      finish('restart');
    });

    autoUpdater.checkForUpdates().catch(onError);
  });
}

module.exports = {
  runLauncherAutoUpdate
};
