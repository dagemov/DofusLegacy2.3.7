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
const { login, register, checkHealth } = require('./services/auth-client');

let mainWindow;

const userDataPath = process.env.ONELAUNCHER_USER_DATA_PATH || app.getPath('userData');
const clientePath = path.join(userDataPath, 'cliente');

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

function createWindow() {
  console.log('[Main:Window] Creando ventana principal...');
  mainWindow = new BrowserWindow({
    width: 1180,
    height: 720,
    frame: false,
    resizable: false,
    maximizable: false,
    backgroundColor: '#1a1a1a',
    icon: path.join(__dirname, 'icons', 'favicon.ico'),
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
  } catch (bootstrapError) {
    console.warn(`[Main:UpdateCheck] Servicio de actualizaciones modular fallo: ${bootstrapError.message}`);
    console.warn('[Main:UpdateCheck] Usando flujo XML legacy embebido como ultimo fallback.');
  }

  console.log('[Main:UpdateCheck] Verificando actualizaciones...');
  const server = 'https://beta.1emu.fun/updates/';
  const versionFileName = 'version';
  const versionPath = path.join(clientePath, versionFileName);
  console.log(`[Main:UpdateCheck] Ruta del archivo de versión local: ${versionPath}`);
  let localVersion = '0.0.0';
  try {
    if (await fs.pathExists(versionPath)) {
      localVersion = (await fs.readFile(versionPath, 'utf8')).trim();
      console.log(`[Main:UpdateCheck] Versión local encontrada: ${localVersion}`);
    } else {
      console.log('[Main:UpdateCheck] Archivo de versión no encontrado, creando con 0.0.0');
      await fs.writeFile(versionPath, localVersion);
    }
    console.log(`[Main:UpdateCheck] Consultando ${server}Updates.xml`);
    const response = await axios.get(`${server}Updates.xml`);
    const updates = await new Promise((resolve, reject) => {
      parseString(response.data, (err, result) => {
        if (err) {
            console.error('[Main:UpdateCheck] Error parseando XML:', err);
            return reject(new Error(`Error parseando XML: ${err.message}`));
        }
        if (!result || !result.updates || !Array.isArray(result.updates.update)) {
            console.warn('[Main:UpdateCheck] Formato de Updates.xml inesperado o vacío.');
            return resolve([]);
        }
        const parsedUpdates = result.updates.update.map(u => ({
          version: u.version && u.version[0],
          file: u.file && u.file[0]
        })).filter(u => u.version && u.file);
         console.log(`[Main:UpdateCheck] Updates parseados: ${parsedUpdates.length}`);
        resolve(parsedUpdates);
      });
    });
    if (updates.length === 0) {
        console.log('[Main:UpdateCheck] No se encontraron versiones válidas en Updates.xml.');
        return { neededUpdates: [], localVersion, latestVersion: localVersion };
    }
    const latestUpdate = updates.reduce((latest, current) => {
        try { return new Version(latest.version).compare(current.version) >= 0 ? latest : current; }
        catch (e) { console.warn(`[Main:UpdateCheck] Formato de versión inválido encontrado: ${current.version}, omitiendo.`); return latest; }
    }, updates[0]);
    const latestVersion = latestUpdate.version;
    console.log(`[Main:UpdateCheck] Última versión disponible en el servidor: ${latestVersion}`);
    const neededUpdates = updates.filter(update => {
        try { return new Version(update.version).compare(localVersion) > 0; }
        catch (e) { console.warn(`[Main:UpdateCheck] Formato de versión inválido en filtro: ${update.version}, omitiendo.`); return false; }
    });
    console.log(`[Main:UpdateCheck] Actualizaciones necesarias: ${neededUpdates.length}`);
    return { neededUpdates, localVersion, latestVersion };
  } catch (error) {
    console.error(`[Main:UpdateCheck] Error en check-updates: ${error.message}`, error.stack);
    return { error: `Error al verificar actualizaciones: ${error.message}`, neededUpdates: [], localVersion: 'Error', latestVersion: 'Error' };
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
    // Asegúrate de que 'extractor-worker.js' esté en el mismo directorio que main.js
    // o ajusta la ruta según sea necesario.
    const workerPath = path.join(__dirname, 'extractor-worker.js');

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
        resolve({ success: true });
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

ipcMain.handle('launch-game', async () => {
  const gameExeName = 'Dofus.exe';
  const gamePath = path.join(clientePath, gameExeName);
  console.log(`[Main:Launch] Intentando lanzar el juego desde: ${gamePath}`);
  try {
    if (!await fs.pathExists(gamePath)) {
      console.error(`[Main:Launch] Error: El ejecutable no se encontró en ${gamePath}`);
      throw new Error(`No se encontró ${gameExeName} en ${clientePath}. Por favor, verifica la instalación o actualiza.`);
    }
    console.log(`[Main:Launch] Ejecutable encontrado. Lanzando con spawn...`);
    const gameProcess = spawn(gamePath, [], {
      cwd: clientePath,
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
const sessionFilePath = path.join(userDataPath, 'launcher-session.json');

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

app.whenReady().then(() => {
    console.log('[Main:App] Evento app.whenReady recibido.');
    createWindow();
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
