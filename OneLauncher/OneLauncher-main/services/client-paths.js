const path = require('path');
const fs = require('fs-extra');

const GAME_EXE_NAME = 'Dofus.exe';
const MAX_SEARCH_DEPTH = 4;

async function findGameExecutable(rootPath, depth = 0) {
  if (!rootPath || depth > MAX_SEARCH_DEPTH) {
    return null;
  }

  const directPath = path.join(rootPath, GAME_EXE_NAME);
  if (await fs.pathExists(directPath)) {
    return directPath;
  }

  let entries;

  try {
    entries = await fs.readdir(rootPath, { withFileTypes: true });
  } catch {
    return null;
  }

  for (const entry of entries) {
    if (!entry.isDirectory()) {
      continue;
    }

    const nestedPath = await findGameExecutable(path.join(rootPath, entry.name), depth + 1);
    if (nestedPath) {
      return nestedPath;
    }
  }

  return null;
}

async function isClientReady(clientePath) {
  const gamePath = await findGameExecutable(clientePath);
  return {
    ready: Boolean(gamePath),
    gamePath
  };
}

module.exports = {
  GAME_EXE_NAME,
  findGameExecutable,
  isClientReady
};
