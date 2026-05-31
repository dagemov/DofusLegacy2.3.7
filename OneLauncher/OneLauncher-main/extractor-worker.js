// extractor-worker.js
const AdmZip = require('adm-zip');
const fs = require('fs-extra');
const { parentPort, workerData } = require('worker_threads');

const PROGRESS_UPDATE_INTERVAL_MS = 100; // Actualizar UI cada 100ms (ajusta según sea necesario)

async function extractZip({ zipFilePath, extractPath }) {
  let lastProgressUpdateTime = 0;

  try {
    if (!await fs.pathExists(zipFilePath)) {
      parentPort.postMessage({ type: 'error', error: `El archivo zip ${zipFilePath} no existe en el worker.` });
      return;
    }

    console.time('WorkerTotalExtractionTime'); // Para medir el tiempo total en el worker
    const zip = new AdmZip(zipFilePath);
    const entries = zip.getEntries().filter(entry => !entry.isDirectory); // Solo contar/procesar archivos
    const totalFiles = entries.length;
    let extractedFilesCount = 0;

    if (totalFiles === 0) {
        console.log('[Worker:Extract] El archivo ZIP no contiene archivos (solo directorios o está vacío).');
        // Si AdmZip maneja bien la creación de directorios vacíos, esto podría ser un éxito.
        // O podría considerarse un caso especial. Por ahora, lo trataremos como extracción completada.
    }

    parentPort.postMessage({ type: 'start', total: totalFiles });
    console.log(`[Worker:Extract] Iniciando extracción. Total de archivos a extraer: ${totalFiles}`);


    for (const entry of entries) {
      // No es necesario verificar entry.isDirectory aquí si ya filtramos arriba
      // console.time('WorkerSingleFileExtractTime'); // Descomentar para medir extracción de archivo individual
      zip.extractEntryTo(entry, extractPath, true, true); // El tercer arg (mantenimiento de atributos de dir) y cuarto (overwrite) son importantes
      // console.timeEnd('WorkerSingleFileExtractTime');
      extractedFilesCount++;

      const currentTime = Date.now();
      if (currentTime - lastProgressUpdateTime > PROGRESS_UPDATE_INTERVAL_MS || extractedFilesCount === totalFiles) {
        const progress = totalFiles > 0 ? Math.floor((extractedFilesCount / totalFiles) * 100) : 100;
        parentPort.postMessage({
          type: 'progress',
          progress: progress,
          current: extractedFilesCount,
          total: totalFiles
          // Ya no enviamos entryName para simplificar
        });
        lastProgressUpdateTime = currentTime;
        // console.log(`[Worker:Extract] Progreso enviado: ${progress}%, ${extractedFilesCount}/${totalFiles}`);
      }
    }

    // Asegurar un mensaje final de progreso al 100% si el throttling lo omitió
    // (aunque la condición `extractedFilesCount === totalFiles` debería cubrirlo)
    if (extractedFilesCount === totalFiles) {
        const finalProgressState = {
            type: 'progress',
            progress: 100,
            current: extractedFilesCount,
            total: totalFiles
        };
        // Podrías verificar si el último mensaje enviado ya era 100% para no duplicar,
        // pero el renderer usualmente manejará bien mensajes duplicados idénticos.
        parentPort.postMessage(finalProgressState);
        console.log(`[Worker:Extract] Enviando estado final de progreso: 100%`);
    }


    console.log(`[Worker:Extract] Extracción de ${extractedFilesCount} archivos completada. Eliminando ZIP...`);
    await fs.remove(zipFilePath);
    console.log(`[Worker:Extract] Archivo ZIP ${zipFilePath} eliminado.`);
    parentPort.postMessage({ type: 'done', message: 'Extracción completada y ZIP eliminado.' });
    console.timeEnd('WorkerTotalExtractionTime');

  } catch (error) {
    console.error('[Worker:Extract] Error durante la extracción:', error);
    parentPort.postMessage({ type: 'error', error: error.message, stack: error.stack });
    if (console.timeLog) console.timeLog('WorkerTotalExtractionTime'); // Detener si hubo error
    else console.timeEnd('WorkerTotalExtractionTime');
  }
}

extractZip(workerData);