const path = require('path');
const fs = require('fs-extra');
const { spawn } = require('child_process');
const { parentPort, workerData } = require('worker_threads');
const { path7za } = require('7zip-bin');

const PROGRESS_UPDATE_INTERVAL_MS = 250;

function extractWith7zip({ archivePath, extractPath }) {
  return new Promise((resolve, reject) => {
    const args = ['x', archivePath, `-o${extractPath}`, '-y', '-bsp1'];
    const binaryPath = path7za;

    if (!binaryPath || !fs.existsSync(binaryPath)) {
      reject(new Error('No se encontro 7zip para extraer el cliente.'));
      return;
    }

    const child = spawn(binaryPath, args, {
      windowsHide: true,
      stdio: ['ignore', 'pipe', 'pipe']
    });

    let lastProgressUpdateTime = 0;
    let lastProgress = 0;
    let stderr = '';

    const emitProgress = (progress) => {
      const normalized = Math.max(0, Math.min(100, progress));
      const currentTime = Date.now();

      if (normalized === lastProgress && normalized !== 100) {
        return;
      }

      if (currentTime - lastProgressUpdateTime < PROGRESS_UPDATE_INTERVAL_MS && normalized !== 100) {
        return;
      }

      lastProgress = normalized;
      lastProgressUpdateTime = currentTime;
      parentPort.postMessage({
        type: 'progress',
        progress: normalized,
        current: normalized,
        total: 100
      });
    };

    parentPort.postMessage({ type: 'start', total: 100 });
    emitProgress(0);

    const handleStream = (chunk) => {
      const text = chunk.toString();
      const matches = text.match(/(\d{1,3})%/g);

      if (!matches) {
        return;
      }

      const latest = Number.parseInt(matches[matches.length - 1], 10);
      if (Number.isFinite(latest)) {
        emitProgress(latest);
      }
    };

    child.stdout.on('data', handleStream);
    child.stderr.on('data', (chunk) => {
      stderr += chunk.toString();
      handleStream(chunk);
    });

    child.on('error', (error) => {
      reject(error);
    });

    child.on('close', (code) => {
      if (code === 0) {
        emitProgress(100);
        resolve();
        return;
      }

      reject(new Error(stderr.trim() || `7zip termino con codigo ${code}.`));
    });
  });
}

async function extractArchive({ archivePath, extractPath }) {
  if (!await fs.pathExists(archivePath)) {
    throw new Error(`El archivo ${archivePath} no existe.`);
  }

  await fs.ensureDir(extractPath);
  await extractWith7zip({ archivePath, extractPath });
  await fs.remove(archivePath);
}

extractArchive({
  archivePath: workerData.zipFilePath || workerData.archivePath,
  extractPath: workerData.extractPath
})
  .then(() => {
    parentPort.postMessage({ type: 'done', message: 'Extraccion completada.' });
  })
  .catch((error) => {
    parentPort.postMessage({
      type: 'error',
      error: error.message,
      stack: error.stack
    });
  });
