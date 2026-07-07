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

function readManifestUpdates(manifest) {
  return manifest?.updates || manifest?.Updates || [];
}

function sortUpdatesByVersion(updates) {
  return [...updates].sort((left, right) => new Version(left.version).compare(right.version));
}

function resolveNeededUpdates(manifest, localVersion, clientMissing, logger = console) {
  const versionedUpdates = readManifestUpdates(manifest)
    .map((entry) => ({
      version: entry.version || entry.Version,
      file: entry.file || entry.File || entry.name || entry.Name,
      url: entry.url || entry.Url,
      checksum: entry.checksum || entry.Checksum || 'TEMP',
      size: entry.size ?? entry.Size ?? 0,
      source: 'updates-xml'
    }))
    .filter((entry) => entry.version && entry.file && entry.url);

  if (versionedUpdates.length > 0) {
    const sortedUpdates = sortUpdatesByVersion(versionedUpdates);
    const pendingUpdates = sortedUpdates.filter(
      (entry) => new Version(entry.version).compare(localVersion) > 0
    );

    if (clientMissing && pendingUpdates.length === 0) {
      const latestPatch = sortedUpdates[sortedUpdates.length - 1];
      const localIsFresh = new Version(localVersion).compare(latestPatch.version) >= 0;

      if (localIsFresh) {
        logger.warn(
          '[Main:UpdateCheck] Cliente ausente pero parches al dia. Reaplicando ultimo parche: %s',
          latestPatch.file
        );
        return [latestPatch];
      }

      if (new Version(localVersion).compare('0.0.0') === 0) {
        logger.warn('[Main:UpdateCheck] Cliente ausente sin version local: aplicando cadena completa.');
        return sortedUpdates;
      }

      logger.warn('[Main:UpdateCheck] Cliente ausente con version parcial: aplicando parches pendientes.');
      return pendingUpdates;
    }

    return pendingUpdates;
  }

  const updates = normalizeManifestUpdates(manifest);
  const latestVersion = readManifestVersion(manifest);
  const versionNeedsUpdate = new Version(latestVersion).compare(localVersion) > 0;

  return versionNeedsUpdate || clientMissing ? updates : [];
}

async function getUpdatesFromApi(localVersion, clientePath, logger = console) {
  const manifest = await fetchApiManifest(logger);
  const latestVersion = readManifestVersion(manifest);
  const clientState = await isClientReady(clientePath);
  const clientMissing = !clientState.ready;
  const neededUpdates = resolveNeededUpdates(manifest, localVersion, clientMissing, logger);

  logger.log(`[Main:UpdateCheck] Version detectada por API: ${latestVersion}`);
  logger.log(`[Main:UpdateCheck] Fuente manifiesto: ${manifest.manifestSource || manifest.ManifestSource || 'api'}`);

  if (clientMissing && neededUpdates.length === 0) {
    logger.warn('[Main:UpdateCheck] Cliente ausente pero Updates.xml no tiene parches pendientes.');
  }

  return {
    neededUpdates,
    localVersion,
    latestVersion,
    manifest,
    source: manifest.manifestSource || manifest.ManifestSource || 'api',
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
