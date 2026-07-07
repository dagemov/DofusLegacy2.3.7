// OnesvUpdater V1.0 | 2025
// By https://maestro-yaco.blogspot.com/
// Suport: https://discord.gg/yZnADDUKHx

const { app, BrowserWindow, ipcMain, shell, dialog } = require('electron');
const path = require('path');
const fs = require('fs-extra');
const axios = require('axios');
const { parseString } = require('xml2js');
const { spawn } = require('child_process');
// AdmZip ya no es necesario aquí directamente para la extracción, se usa en el worker
// const AdmZip = require('adm-zip');
const { Worker } = require('worker_threads'); // <--- AÑADIDO PARA WORKER THREADS
const { checkUpdates } = require('./services/update-service');
const { runLauncherAutoUpdate } = require('./services/launcher-auto-update');
const { login, register, checkHealth } = require('./services/auth-client');
const { findGameExecutable, isClientReady } = require('./services/client-paths');

let mainWindow;

const userDataPath = process.env.ONELAUNCHER_USER_DATA_PATH || app.getPath('userData');
const clientePath = path.join(userDataPath, 'cliente');
const sessionFilePath = path.join(userDataPath, 'launcher-session.json');
const launcherSettingsFilePath = path.join(userDataPath, 'launcher-settings.json');

const SUPPORTED_LANGUAGES = [
  { code: 'fr', label: 'Francais', langCurrent: 'fr', bindsCurrent: 'frFR' },
  { code: 'en', label: 'English', langCurrent: 'en', bindsCurrent: 'enGB' },
  { code: 'de', label: 'Deutsch', langCurrent: 'de', bindsCurrent: 'deDE' },
  { code: 'es', label: 'Espanol', langCurrent: 'es', bindsCurrent: 'esES' },
  { code: 'it', label: 'Italiano', langCurrent: 'it', bindsCurrent: 'itIT' },
  { code: 'pt', label: 'Portugues', langCurrent: 'pt', bindsCurrent: 'ptPT' },
  { code: 'jp', label: 'Japanese', langCurrent: 'ja', bindsCurrent: 'jaJP' },
  { code: 'nl', label: 'Nederlands', langCurrent: 'nl', bindsCurrent: 'nlNL' },
  { code: 'ru', label: 'Russian', langCurrent: 'ru', bindsCurrent: 'ruRU' }
];

const DEFAULT_LANGUAGE_CODE = 'es';

console.log(`[Main:Init] Ruta de datos del cliente (userData): ${clientePath}`);

try {
    fs.ensureDirSync(clientePath);
    console.log(`[Main:Init] Directorio ${clientePath} asegurado.`);
} catch (err) {
    console.error(`[Main:Init] ERROR CRÍTICO: No se pudo crear o acceder al directorio de datos ${clientePath}.`, err);
    dialog.showErrorBox('Error Crítico de Permisos',
        `No se pudo crear o acceder a la carpeta de datos necesaria:\n${clientePath}\n\n` +
        `Por favor, verifica los permisos de tu carpeta de usuario o ejecuta el lanzador una vez como administrador si persiste el problema.\n\nError: ${err.message}`
    );
    app.quit();
    process.exit(1);
}

function normalizeLanguageCode(value) {
  const code = String(value || '').trim().toLowerCase();

  if (!code) {
    return DEFAULT_LANGUAGE_CODE;
  }

  switch (code) {
    case 'ja':
      return 'jp';
    case 'us':
    case 'uk':
    case 'gb':
      return 'en';
    case 'br':
      return 'pt';
    default:
      return SUPPORTED_LANGUAGES.some(language => language.code === code)
        ? code
        : DEFAULT_LANGUAGE_CODE;
  }
}

function getLanguageDefinition(code) {
  const normalizedCode = normalizeLanguageCode(code);
  return SUPPORTED_LANGUAGES.find(language => language.code === normalizedCode)
    || SUPPORTED_LANGUAGES.find(language => language.code === DEFAULT_LANGUAGE_CODE);
}

function getClientConfigCandidatePaths() {
  return [
    path.join(clientePath, 'config.xml'),
    path.join(clientePath, 'cliente', 'config.xml'),
    path.join(clientePath, 'reg', 'config.xml'),
    path.join(clientePath, 'reg', 'share', 'config.xml')
  ];
}

async function readLauncherSettings() {
  try {
    if (await fs.pathExists(launcherSettingsFilePath)) {
      return await fs.readJson(launcherSettingsFilePath);
    }
  } catch (error) {
    console.warn('[Main:Settings] No se pudo leer launcher-settings.json:', error.message);
  }

  return {};
}

async function writeLauncherSettings(settings) {
  await fs.writeJson(launcherSettingsFilePath, settings, { spaces: 2 });
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function replaceOrAppendConfigEntry(xml, key, value) {
  const replacement = `<entry key="${key}">${value}</entry>`;
  const entryRegex = new RegExp(`<entry\\s+key="${escapeRegExp(key)}">.*?<\\/entry>`, 'gi');

  if (entryRegex.test(xml)) {
    return xml.replace(entryRegex, replacement);
  }

  if (/<\/LangFile>/i.test(xml)) {
    return xml.replace(/<\/LangFile>/i, `    ${replacement}\n</LangFile>`);
  }

  return `${xml}\n${replacement}\n`;
}

async function hasClientConfigFiles() {
  const paths = getClientConfigCandidatePaths();
  const exists = await Promise.all(paths.map((configPath) => fs.pathExists(configPath)));
  return exists.some(Boolean);
}

async function detectCurrentClientLanguage() {
  const languageFilePath = path.join(clientePath, 'data', 'Launcher', 'lang.txt');

  try {
    if (await fs.pathExists(languageFilePath)) {
      const fileCode = (await fs.readFile(languageFilePath, 'utf8')).trim();
      return normalizeLanguageCode(fileCode);
    }

    const configPaths = getClientConfigCandidatePaths();
    const existingPaths = [];

    await Promise.all(configPaths.map(async (configPath) => {
      if (await fs.pathExists(configPath)) {
        existingPaths.push(configPath);
      }
    }));

    for (const configPath of existingPaths) {
      const xml = await fs.readFile(configPath, 'utf8');
      const match = xml.match(/<entry key="lang\.current">(.*?)<\/entry>/i);

      if (match?.[1]) {
        return normalizeLanguageCode(match[1]);
      }
    }
  } catch (error) {
    console.warn('[Main:Language] No se pudo detectar el idioma actual:', error.message);
  }

  return DEFAULT_LANGUAGE_CODE;
}

async function applyLanguageToClientFiles(languageCode) {
  const language = getLanguageDefinition(languageCode);
  const launcherDataDirectory = path.join(clientePath, 'data', 'Launcher');
  const languageFilePath = path.join(launcherDataDirectory, 'lang.txt');
  const updatedConfigPaths = [];

  await fs.ensureDir(launcherDataDirectory);
  await fs.writeFile(languageFilePath, language.code, 'utf8');

  for (const configPath of getClientConfigCandidatePaths()) {
    if (!await fs.pathExists(configPath)) {
      continue;
    }

    const xml = await fs.readFile(configPath, 'utf8');
    const updatedXml = replaceOrAppendConfigEntry(
      replaceOrAppendConfigEntry(xml, 'lang.current', language.langCurrent),
      'binds.current',
      language.bindsCurrent
    );

    await fs.writeFile(configPath, updatedXml, 'utf8');
    updatedConfigPaths.push(configPath);
  }

  console.log(`[Main:Language] Idioma aplicado: ${language.code}`);
  return {
    language,
    languageFilePath,
    updatedConfigPaths
  };
}

async function ensureSavedLanguagePreferenceApplied() {
  const settings = await readLauncherSettings();

  if (!settings?.languageCode) {
    return null;
  }

  return applyLanguageToClientFiles(settings.languageCode);
}

async function getLanguageSettingsPayload() {
  const [settings, detected, clientConfigFound] = await Promise.all([
    readLauncherSettings(),
    detectCurrentClientLanguage(),
    hasClientConfigFiles()
  ]);
  const selected = normalizeLanguageCode(settings.languageCode || detected);

  return {
    selected,
    detected,
    hasStoredPreference: Boolean(settings.languageCode),
    clientConfigFound,
    supportedLanguages: SUPPORTED_LANGUAGES.map(({ code, label }) => ({ code, label }))
  };
}

function createWindow() {
  console.log('[Main:Window] Creando ventana principal...');
  mainWindow = new BrowserWindow({
    width: 1180,
    height: 720,
    frame: false,
    resizable: false,
    maximizable: false,
    backgroundColor: '#1a1a1a',
    icon: path.join(__dirname, 'icons', 'app-icon.ico'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  });

  // :::::Abre las herramientas de desarrollo::::
  // mainWindow.webContents.openDevTools();

  mainWindow.loadFile(path.join(__dirname, 'index.html'))
    .then(() => console.log('[Main:Window] index.html cargado exitosamente.'))
    .catch(err => console.error('[Main:Window] Error al cargar index.html:', err));

  mainWindow.on('closed', () => {
      console.log('[Main:Window] Ventana principal cerrada.');
      mainWindow = null;
  });
}

class Version {
  constructor(version) {
    this.parts = version.split('.').map(Number);
  }
  compare(otherVersion) {
    const other = new Version(otherVersion);
    const maxLength = Math.max(this.parts.length, other.parts.length);
    for (let i = 0; i < maxLength; i++) {
      const partThis = this.parts[i] || 0;
      const partOther = other.parts[i] || 0;
      if (partThis > partOther) return 1;
      if (partThis < partOther) return -1;
    }
    return 0;
  }
}

ipcMain.handle('check-updates', async () => {
  try {
    const result = await checkUpdates(clientePath, console);
    console.log(`[Main:UpdateCheck] Fuente usada: ${result.source}`);
    console.log(`[Main:UpdateCheck] Actualizaciones necesarias: ${result.neededUpdates.length}`);
    return result;
  } catch (error) {
    console.error(`[Main:UpdateCheck] Error en check-updates: ${error.message}`, error.stack);
    return {
      error: `Error al verificar actualizaciones: ${error.message}`,
      neededUpdates: [],
      localVersion: 'Error',
      latestVersion: 'Error',
      source: 'api',
      apiOnline: false
    };
  }
});

ipcMain.handle('download-file', async (_, { url, fileName }) => {
  const filePath = path.join(clientePath, fileName);
  console.log(`[Main:Download] Iniciando descarga de ${url} a ${filePath}`);

  try {
    console.log('[Main:Download] Realizando llamada Axios...');
    const response = await axios({
      url,
      method: 'GET',
      responseType: 'stream',
      timeout: 60000
    });
    console.log(`[Main:Download] Llamada Axios exitosa (Status: ${response.status}). Iniciando stream...`);

    const totalLength = Number(response.headers['content-length']);
    console.log(`[Main:Download] Tamaño total (bytes): ${totalLength ? totalLength : 'No especificado'}`);
    const writer = fs.createWriteStream(filePath);

    let receivedLength = 0;
    const startTime = Date.now();

    response.data.on('data', (chunk) => {
      try {
        receivedLength += chunk.length;
        const elapsedTime = (Date.now() - startTime) / 1000;
        const speed = elapsedTime > 0 ? Math.round((receivedLength / elapsedTime) / 1024) : 0;
        const progress = totalLength ? Math.round((receivedLength / totalLength) * 100) : 0;

        const receivedMB = (receivedLength / (1024 * 1024)).toFixed(2);
        const totalMB = totalLength ? (totalLength / (1024 * 1024)).toFixed(2) : '???';

        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.webContents.send('download-progress', {
            progress,
            receivedMB,
            totalMB,
            speed
          });
        }
      } catch(e) {
          console.error("[Main:Download] Error dentro del listener 'data':", e);
      }
    });

    return new Promise((resolve, reject) => {
      writer.on('finish', () => {
        console.log(`[Main:Download] Evento 'finish' del writer recibido. Resolviendo con: ${filePath}`);
        console.log(`[Main:Download] Descarga completada: ${filePath}`);
        resolve(filePath);
      });

      writer.on('error', (err) => {
        console.error(`[Main:Download] Error en writer: ${err.message}`);
        fs.remove(filePath).catch(() => {});
        reject(new Error(`Error al escribir archivo: ${err.message}`));
      });

      response.data.on('error', (err) => {
        console.error(`[Main:Download] Error en stream: ${err.message}`);
        writer.close();
        fs.remove(filePath).catch(() => {});
        reject(new Error(`Error de red durante la descarga: ${err.message}`));
      });

      response.data.pipe(writer);
    });

  } catch (error) {
    console.error(`[Main:Download] Error inicializando descarga: ${error.message}`);
    await fs.remove(filePath).catch(() => {});
    throw new Error(`Error inicializando descarga: ${error.message}`);
  }
});

// --- IPC: extract-zip con progreso USANDO WORKER THREAD ---
// ESTA ES LA SECCIÓN MODIFICADA
ipcMain.handle('extract-zip', async (_, zipFilePath) => {
  console.log(`[Main:ExtractWorker] Solicitud para extraer ${zipFilePath} a ${clientePath}`);

  return new Promise((resolve, reject) => {
    const workerPath = app.isPackaged
      ? path.join(process.resourcesPath, 'app.asar.unpacked', 'extractor-worker.js')
      : path.join(__dirname, 'extractor-worker.js');

    // Verificar si el archivo worker existe antes de intentar usarlo
    if (!fs.existsSync(workerPath)) {
        const errorMsg = `[Main:ExtractWorker] Error: El script del worker no se encontró en ${workerPath}`;
        console.error(errorMsg);
        return reject(new Error(errorMsg));
    }
    
    console.log(`[Main:ExtractWorker] Usando worker script desde: ${workerPath}`);

    const worker = new Worker(workerPath, {
      workerData: {
        zipFilePath: zipFilePath,
        extractPath: clientePath
      }
    });

    console.log(`[Main:ExtractWorker] Worker creado para ${zipFilePath}`);

    worker.on('message', (message) => {
      if (message.type === 'progress') {
        // No es necesario loguear cada progreso aquí si es muy frecuente,
        // pero puede ser útil para depuración inicial.
        // console.log(`[Main:ExtractWorker] Progreso: ${message.entryName} (${message.current}/${message.total}) - ${message.progress}%`);
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.webContents.send('extract-progress', {
            progress: message.progress,
            entryName: message.entryName,
            current: message.current,
            total: message.total
          });
        }
      } else if (message.type === 'done') {
        console.log(`[Main:ExtractWorker] Extracción completada por el worker: ${message.message}`);
        ensureSavedLanguagePreferenceApplied()
          .then(() => resolve({ success: true }))
          .catch(reject);
      } else if (message.type === 'error') {
        console.error(`[Main:ExtractWorker] Error en worker: ${message.error}`, message.stack ? `\nStack: ${message.stack}` : '');
        reject(new Error(`Error en extracción (worker): ${message.error}`));
      } else if (message.type === 'start') {
        console.log(`[Main:ExtractWorker] Worker inició extracción. Total de entradas: ${message.total}`);
      }
    });

    worker.on('error', (error) => {
      console.error(`[Main:ExtractWorker] Error crítico en worker: ${error.message}`, error.stack);
      // Asegurarse de que la promesa se rechace si aún no se ha hecho
      reject(new Error(`Error crítico del worker de extracción: ${error.message}`));
    });

    worker.on('exit', (code) => {
      if (code !== 0) {
        console.error(`[Main:ExtractWorker] Worker se detuvo con código de salida ${code}`);
        // Si el worker sale con un código distinto de 0 y no hemos resuelto/rechazado ya,
        // es probable un error no capturado dentro del worker o una salida prematura.
        // Considera rechazar aquí si la promesa aún está pendiente.
        // reject(new Error(`Worker se detuvo inesperadamente con código ${code}`));
      } else {
        console.log(`[Main:ExtractWorker] Worker finalizado exitosamente (código ${code}).`);
      }
    });
  });
});
// --- FIN DE LA SECCIÓN MODIFICADA ---

ipcMain.handle('update-local-version', async (_, newVersion) => {
  const versionFileName = 'version';
  const versionPath = path.join(clientePath, versionFileName);
  console.log(`[Main:VersionUpdate] Actualizando archivo de versión en ${versionPath} a ${newVersion}`);
  try {
    await fs.writeFile(versionPath, newVersion.trim());
    console.log(`[Main:VersionUpdate] Versión local actualizada exitosamente.`);
    return { success: true };
  } catch (error) {
    console.error(`[Main:VersionUpdate] Error al actualizar versión local: ${error.message}`, error.stack);
    return { success: false, error: `Error al guardar versión: ${error.message}` };
  }
});

ipcMain.handle('check-client-ready', async () => {
  try {
    return await isClientReady(clientePath);
  } catch (error) {
    return {
      ready: false,
      gamePath: null,
      error: error.message
    };
  }
});

ipcMain.handle('launch-game', async () => {
  const gamePath = await findGameExecutable(clientePath);
  console.log(`[Main:Launch] Intentando lanzar el juego desde: ${gamePath || '(no encontrado)'}`);
  try {
    await ensureSavedLanguagePreferenceApplied();

    if (!gamePath) {
      throw new Error('No se encontro Dofus.exe. Descarga e instala el cliente desde el launcher.');
    }

    const gameDir = path.dirname(gamePath);
    console.log(`[Main:Launch] Ejecutable encontrado. Lanzando con spawn...`);
    const gameProcess = spawn(gamePath, [], {
      cwd: gameDir,
      detached: true,
      stdio: 'ignore'
    });
    gameProcess.unref();
    gameProcess.on('error', (err) => {
      console.error(`[Main:Launch] Error en spawn: ${err.message}`, err.stack);
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('launch-error', `Error al iniciar el proceso: ${err.message}`);
      }
    });
    gameProcess.on('exit', (code, signal) => {
      console.log(`[Main:Launch] Proceso terminado con código: ${code}, señal: ${signal}`);
    });
    return { success: true };
  } catch (error) {
    console.error(`[Main:Launch] Error en launch-game: ${error.message}`, error.stack);
    return { success: false, error: error.message };
  }
});

ipcMain.on('minimize-app', () => {
    console.log('[Main:Window] Solicitud para minimizar ventana.');
    mainWindow?.minimize();
});
ipcMain.on('close-app', () => {
    console.log('[Main:Window] Solicitud para cerrar la aplicación.');
    app.quit();
});

async function readSession() {
  try {
    if (await fs.pathExists(sessionFilePath)) {
      return await fs.readJson(sessionFilePath);
    }
  } catch (err) {
    console.warn('[Main:Auth] No se pudo leer la sesion local:', err.message);
  }

  return null;
}

async function writeSession(session) {
  await fs.writeJson(sessionFilePath, session, { spaces: 2 });
}

async function clearSession() {
  if (await fs.pathExists(sessionFilePath)) {
    await fs.remove(sessionFilePath);
  }
}

ipcMain.handle('check-api-health', async () => {
  try {
    const payload = await checkHealth();
    return { online: true, payload };
  } catch (error) {
    return { online: false, error: error.message };
  }
});

ipcMain.handle('get-session', async () => readSession());

ipcMain.handle('get-client-folder', async () => ({
  path: clientePath
}));

ipcMain.handle('open-client-folder', async () => {
  await fs.ensureDir(clientePath);
  const result = await shell.openPath(clientePath);
  if (result) {
    throw new Error(result);
  }
  return { path: clientePath };
});

ipcMain.handle('get-language-settings', async () => {
  try {
    return await getLanguageSettingsPayload();
  } catch (error) {
    return {
      selected: DEFAULT_LANGUAGE_CODE,
      detected: DEFAULT_LANGUAGE_CODE,
      hasStoredPreference: false,
      clientConfigFound: false,
      supportedLanguages: SUPPORTED_LANGUAGES.map(({ code, label }) => ({ code, label })),
      error: error.message
    };
  }
});

ipcMain.handle('set-language-preference', async (_, languageCode) => {
  try {
    const language = getLanguageDefinition(languageCode);
    const currentSettings = await readLauncherSettings();

    await writeLauncherSettings({
      ...currentSettings,
      languageCode: language.code,
      updatedAtUtc: new Date().toISOString()
    });

    const applied = await applyLanguageToClientFiles(language.code);
    const updatedConfigCount = applied.updatedConfigPaths.length;

    return {
      success: true,
      selected: language.code,
      label: language.label,
      updatedConfigCount,
      message: updatedConfigCount > 0
        ? `Idioma guardado: ${language.label}.`
        : `Idioma guardado: ${language.label}. Se aplicara por completo cuando el cliente este disponible.`
    };
  } catch (error) {
    return {
      success: false,
      message: `No se pudo guardar el idioma: ${error.message}`
    };
  }
});

ipcMain.handle('auth-login', async (_, payload) => {
  try {
    const result = await login(payload);
    if (result?.success) {
      await writeSession({
        username: result.username,
        nickname: result.nickname,
        accountId: result.accountId,
        loggedInAtUtc: new Date().toISOString()
      });
    }

    return result;
  } catch (error) {
    const message = error.response?.data?.message || error.message;
    return {
      success: false,
      title: 'Login fallido',
      message
    };
  }
});

ipcMain.handle('auth-register', async (_, payload) => {
  try {
    return await register(payload);
  } catch (error) {
    const message = error.response?.data?.message || error.message;
    return {
      success: false,
      title: 'Registro fallido',
      message
    };
  }
});

ipcMain.handle('auth-logout', async () => {
  await clearSession();
  return { success: true };
});

ipcMain.on('open-url', (_, url) => {
  console.log(`[Main:Shell] Solicitud para abrir URL externa: ${url}`);
  if (/^https?:\/\//.test(url)) {
    shell.openExternal(url);
  } else {
    console.warn(`[Main:Shell] Intento de abrir URL inválida o no segura: ${url}`);
  }
});

app.whenReady().then(async () => {
    console.log('[Main:App] Evento app.whenReady recibido.');
    createWindow();

    if (mainWindow) {
      mainWindow.webContents.once('did-finish-load', () => {
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.webContents.send('launcher-ready');
        }

        runLauncherAutoUpdate(mainWindow, console).catch((error) => {
          console.warn('[Main:LauncherUpdate] Excepcion no bloqueante:', error?.message || error);
        });
      });
    }

    app.on('activate', () => {
        console.log('[Main:App] Evento app.activate recibido.');
        if (BrowserWindow.getAllWindows().length === 0) {
            console.log('[Main:App] No hay ventanas, creando una nueva.');
            createWindow();
        }
    });
});
app.on('window-all-closed', () => {
  console.log('[Main:App] Evento window-all-closed recibido.');
  if (process.platform !== 'darwin') {
    console.log('[Main:App] Cerrando la aplicación (no es macOS).');
    app.quit();
  } else {
      console.log('[Main:App] No cerrando la aplicación (es macOS).');
  }
});
app.on('quit', () => {
   console.log('[Main:App] Evento app.quit recibido. La aplicación se está cerrando.');
});

process.on('uncaughtException', (error, origin) => {
  console.error('!!!! [Main:Error] ERROR NO CAPTURADO !!!!');
  console.error('[Main:Error] Origen:', origin);
  console.error('[Main:Error] Error:', error);
  if (dialog && typeof dialog.showErrorBox === 'function') { // Verificar si dialog y showErrorBox están disponibles
      dialog.showErrorBox('Error Crítico', `Ha ocurrido un error inesperado y la aplicación debe cerrarse:\n\n${error.message}\n\nOrigen: ${origin}`);
  }
});
process.on('unhandledRejection', (reason, promise) => {
  console.error('!!!! [Main:Error] PROMESA RECHAZADA NO MANEJADA !!!!');
  console.error('[Main:Error] Promesa:', promise);
  console.error('[Main:Error] Razón:', reason);
});
