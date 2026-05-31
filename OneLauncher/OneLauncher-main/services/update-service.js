const path = require('path');
const fs = require('fs-extra');
const axios = require('axios');
const { parseString } = require('xml2js');
const { endpoints } = require('../config/endpoints');

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
  logger.log(`[Main:UpdateCheck] Consultando API local: ${endpoints.apiManifestUrl}`);

  const response = await axios.get(endpoints.apiManifestUrl, {
    timeout: 2500,
    headers: {
      Accept: 'application/json'
    }
  });

  logger.log('[Main:UpdateCheck] API online');
  logger.log('[Main:UpdateCheck] Manifest recibido desde API local:', response.data);

  return response.data;
}

function normalizeManifestUpdates(manifest) {
  if (!manifest || typeof manifest.version !== 'string' || !Array.isArray(manifest.packages)) {
    throw new Error('El manifiesto de la API no tiene el formato esperado.');
  }

  return manifest.packages
    .map(packageInfo => ({
      version: manifest.version,
      file: packageInfo.name,
      url: packageInfo.url,
      checksum: packageInfo.checksum,
      size: packageInfo.size,
      source: 'api'
    }))
    .filter(update => update.version && update.file && update.url);
}

async function getUpdatesFromApi(localVersion, logger = console) {
  const manifest = await fetchApiManifest(logger);
  const updates = normalizeManifestUpdates(manifest);
  const latestVersion = manifest.version;

  logger.log(`[Main:UpdateCheck] Version detectada por API: ${latestVersion}`);

  const neededUpdates = new Version(latestVersion).compare(localVersion) > 0
    ? updates
    : [];

  return {
    neededUpdates,
    localVersion,
    latestVersion,
    manifest,
    source: 'api'
  };
}

async function getLegacyXmlUpdates(logger = console) {
  logger.log(`[Main:UpdateCheck] Consultando fallback XML: ${endpoints.legacyUpdatesXmlUrl}`);
  const response = await axios.get(endpoints.legacyUpdatesXmlUrl);

  return new Promise((resolve, reject) => {
    parseString(response.data, (err, result) => {
      if (err) {
        logger.error('[Main:UpdateCheck] Error parseando XML:', err);
        return reject(new Error(`Error parseando XML: ${err.message}`));
      }

      if (!result || !result.updates || !Array.isArray(result.updates.update)) {
        logger.warn('[Main:UpdateCheck] Formato de Updates.xml inesperado o vacio.');
        return resolve([]);
      }

      const parsedUpdates = result.updates.update.map(update => ({
        version: update.version && update.version[0],
        file: update.file && update.file[0],
        url: update.file && update.file[0] ? `${endpoints.legacyUpdateBaseUrl}${update.file[0]}` : undefined,
        source: 'xml'
      })).filter(update => update.version && update.file);

      logger.log(`[Main:UpdateCheck] Updates XML parseados: ${parsedUpdates.length}`);
      resolve(parsedUpdates);
    });
  });
}

async function getUpdatesFromLegacyXml(localVersion, logger = console) {
  const updates = await getLegacyXmlUpdates(logger);

  if (updates.length === 0) {
    logger.log('[Main:UpdateCheck] No se encontraron versiones validas en Updates.xml.');
    return { neededUpdates: [], localVersion, latestVersion: localVersion, source: 'xml' };
  }

  const latestUpdate = updates.reduce((latest, current) => {
    try {
      return new Version(latest.version).compare(current.version) >= 0 ? latest : current;
    } catch (error) {
      logger.warn(`[Main:UpdateCheck] Formato de version invalido encontrado: ${current.version}, omitiendo.`);
      return latest;
    }
  }, updates[0]);

  const latestVersion = latestUpdate.version;
  logger.log(`[Main:UpdateCheck] Ultima version disponible en XML: ${latestVersion}`);

  const neededUpdates = updates.filter(update => {
    try {
      return new Version(update.version).compare(localVersion) > 0;
    } catch (error) {
      logger.warn(`[Main:UpdateCheck] Formato de version invalido en filtro: ${update.version}, omitiendo.`);
      return false;
    }
  });

  return { neededUpdates, localVersion, latestVersion, source: 'xml' };
}

async function checkUpdates(clientePath, logger = console) {
  logger.log('[Main:UpdateCheck] Verificando actualizaciones...');
  const localVersion = await getLocalVersion(clientePath, logger);

  try {
    return await getUpdatesFromApi(localVersion, logger);
  } catch (error) {
    logger.warn(`[Main:UpdateCheck] API offline o invalida: ${error.message}`);
    logger.warn('[Main:UpdateCheck] Continuando con fallback a Updates.xml.');
    return await getUpdatesFromLegacyXml(localVersion, logger);
  }
}

module.exports = {
  checkUpdates
};
