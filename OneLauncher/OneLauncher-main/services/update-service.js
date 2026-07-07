const path = require('path');
const fs = require('fs-extra');
const axios = require('axios');
const { endpoints } = require('../config/endpoints');
const { isClientReady } = require('./client-paths');

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

const API_MANIFEST_TIMEOUT_MS = Number(process.env.LAUNCHER_API_TIMEOUT_MS || 15000);

async function getLocalVersion(clientePath, logger = console) {
  const versionFileName = 'version';
  const versionPath = path.join(clientePath, versionFileName);
  let localVersion = '0.0.0';

  await fs.ensureDir(clientePath);
  logger.log(`[Main:UpdateCheck] Ruta del archivo de version local: ${versionPath}`);

  if (await fs.pathExists(versionPath)) {
    localVersion = (await fs.readFile(versionPath, 'utf8')).trim();
    logger.log(`[Main:UpdateCheck] Version local encontrada: ${localVersion}`);
  } else {
    logger.log('[Main:UpdateCheck] Archivo de version no encontrado, creando con 0.0.0');
    await fs.writeFile(versionPath, localVersion);
  }

  return localVersion;
}

async function fetchApiManifest(logger = console) {
  logger.log(`[Main:UpdateCheck] Consultando API: ${endpoints.apiManifestUrl}`);

  const response = await axios.get(endpoints.apiManifestUrl, {
    timeout: API_MANIFEST_TIMEOUT_MS,
    headers: {
      Accept: 'application/json'
    }
  });

  logger.log('[Main:UpdateCheck] API online');
  logger.log('[Main:UpdateCheck] Manifest recibido desde API:', response.data);

  return response.data;
}

function readManifestVersion(manifest) {
  return manifest.version || manifest.Version;
}

function readManifestPackages(manifest) {
  return manifest.packages || manifest.Packages || [];
}

function normalizeManifestUpdates(manifest) {
  const version = readManifestVersion(manifest);
  const packages = readManifestPackages(manifest);

  if (!version || !Array.isArray(packages)) {
    throw new Error('El manifiesto de la API no tiene el formato esperado.');
  }

  return packages
    .map((packageInfo) => ({
      version,
      file: packageInfo.name || packageInfo.Name,
      url: packageInfo.url || packageInfo.Url,
      checksum: packageInfo.checksum || packageInfo.Checksum,
      size: packageInfo.size ?? packageInfo.Size,
      source: 'api'
    }))
    .filter((update) => update.version && update.file && update.url);
}

async function getUpdatesFromApi(localVersion, clientePath, logger = console) {
  const manifest = await fetchApiManifest(logger);
  const updates = normalizeManifestUpdates(manifest);
  const latestVersion = readManifestVersion(manifest);

  logger.log(`[Main:UpdateCheck] Version detectada por API: ${latestVersion}`);

  const versionNeedsUpdate = new Version(latestVersion).compare(localVersion) > 0;
  const clientState = await isClientReady(clientePath);
  const clientMissing = !clientState.ready;

  if (clientMissing && !versionNeedsUpdate) {
    logger.warn('[Main:UpdateCheck] Version local coincide pero Dofus.exe no existe. Forzando reinstalacion.');
  }

  const neededUpdates = versionNeedsUpdate || clientMissing
    ? updates
    : [];

  return {
    neededUpdates,
    localVersion,
    latestVersion,
    manifest,
    source: 'api',
    apiOnline: true,
    clientReady: clientState.ready,
    clientPath: clientState.gamePath
  };
}

async function checkUpdates(clientePath, logger = console) {
  logger.log('[Main:UpdateCheck] Verificando actualizaciones...');
  const localVersion = await getLocalVersion(clientePath, logger);

  try {
    return await getUpdatesFromApi(localVersion, clientePath, logger);
  } catch (error) {
    logger.error(`[Main:UpdateCheck] Error consultando API: ${error.message}`);
    throw error;
  }
}

module.exports = {
  checkUpdates
};
