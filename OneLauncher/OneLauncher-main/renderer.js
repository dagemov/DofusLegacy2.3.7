// OnesvUpdater V1.0 | 2025
// By https://maestro-yaco.blogspot.com/
// Suport: https://discord.gg/yZnADDUKHx

document.addEventListener('DOMContentLoaded', async () => {
  const elements = {
    playButton: document.getElementById('play-button'),
    playTitle: document.getElementById('play-button-title'),
    playSubtitle: document.getElementById('play-button-subtitle'),
    progressBar: document.getElementById('download-progress'),
    progressText: document.getElementById('progress-text'),
    statusMessage: document.getElementById('status-message'),
    apiPill: document.getElementById('api-pill'),
    apiState: document.getElementById('api-state'),
    serverStatus: document.getElementById('server-status'),
    localVersion: document.getElementById('local-version-value'),
    latestVersion: document.getElementById('latest-version-value'),
    packageValue: document.getElementById('package-value'),
    sourceBadge: document.getElementById('source-badge'),
    launcherStatus: document.getElementById('launcher-status'),
    minimumVersion: document.getElementById('minimum-version'),
    logOutput: document.getElementById('log-output'),
    clearLogsButton: document.getElementById('clear-logs-button'),
    minimizeButton: document.getElementById('minimize-button'),
    closeButton: document.getElementById('close-button')
  };

  const authElements = {
    sessionLabel: document.getElementById('account-session-label'),
    loginForm: document.getElementById('auth-login-form'),
    registerForm: document.getElementById('auth-register-form'),
    logoutButton: document.getElementById('auth-logout-button'),
    feedback: document.getElementById('auth-feedback'),
    usernameInput: document.getElementById('auth-username')
  };

  const setAuthFeedback = (message, type = 'info') => {
    if (!authElements.feedback) return;
    authElements.feedback.textContent = message;
    authElements.feedback.className = `auth-form__feedback ${type}`;
  };

  const refreshSessionUi = async () => {
    const session = await window.api?.getSession?.();
    const isLoggedIn = Boolean(session?.username);

    if (authElements.sessionLabel) {
      authElements.sessionLabel.textContent = isLoggedIn
        ? `Conectado: ${session.username}`
        : 'Sin sesion';
    }

    if (authElements.logoutButton) {
      authElements.logoutButton.hidden = !isLoggedIn;
    }

    if (authElements.usernameInput && session?.username) {
      authElements.usernameInput.value = session.username;
    }
  };

  authElements.loginForm?.addEventListener('submit', async (event) => {
    event.preventDefault();
    const username = document.getElementById('auth-username')?.value?.trim();
    const password = document.getElementById('auth-password')?.value ?? '';

    if (!window.api?.authLogin) {
      setAuthFeedback('IPC de login no disponible.', 'error');
      return;
    }

    setAuthFeedback('Validando credenciales...');
    const result = await window.api.authLogin({ username, password });

    if (result?.success) {
      setAuthFeedback(result.message || 'Sesion iniciada.', 'success');
      addLog(`Login OK: ${result.username}`, 'success');
      await refreshSessionUi();
      return;
    }

    setAuthFeedback(result?.message || 'Credenciales invalidas.', 'error');
    addLog(`Login fallido: ${result?.message || 'error'}`, 'error');
  });

  authElements.registerForm?.addEventListener('submit', async (event) => {
    event.preventDefault();
    const username = document.getElementById('auth-username')?.value?.trim();
    const email = document.getElementById('register-email')?.value?.trim();
    const password = document.getElementById('register-password')?.value ?? '';
    const confirmPassword = document.getElementById('register-confirm')?.value ?? '';

    if (!window.api?.authRegister) {
      setAuthFeedback('IPC de registro no disponible.', 'error');
      return;
    }

    setAuthFeedback('Creando cuenta...');
    const result = await window.api.authRegister({
      username,
      email,
      password,
      confirmPassword
    });

    if (result?.success) {
      setAuthFeedback(result.message || 'Cuenta creada.', 'success');
      addLog(`Registro OK: ${result.username}`, 'success');
      return;
    }

    setAuthFeedback(result?.message || 'No se pudo registrar.', 'error');
    addLog(`Registro fallido: ${result?.message || 'error'}`, 'error');
  });

  authElements.logoutButton?.addEventListener('click', async () => {
    await window.api?.authLogout?.();
    setAuthFeedback('Sesion cerrada.');
    await refreshSessionUi();
  });

  document.querySelectorAll('.nav-item[data-view]').forEach((button) => {
    button.addEventListener('click', () => {
      const view = button.getAttribute('data-view');
      const logsView = document.getElementById('logs-view');
      const accountPanel = document.getElementById('account-panel');

      document.querySelectorAll('.nav-item[data-view]').forEach((item) => {
        item.classList.toggle('active', item === button);
      });

      if (logsView) logsView.hidden = view !== 'logs';
      if (accountPanel) accountPanel.hidden = view !== 'account';
      if (view === 'home') {
        if (logsView) logsView.hidden = false;
        if (accountPanel) accountPanel.hidden = true;
      }
    });
  });

  if (window.api?.checkApiHealth) {
    window.api.checkApiHealth().then((health) => {
      if (health?.online) {
        addLog('Health API: online', 'success');
      } else {
        addLog(`Health API offline: ${health?.error || 'sin respuesta'}`, 'warn');
      }
    });
  }

  refreshSessionUi();

  const requiredElements = ['playButton', 'progressBar', 'statusMessage', 'logOutput'];
  for (const key of requiredElements) {
    if (!elements[key]) {
      console.error(`[Renderer:UI] Elemento requerido no encontrado: ${key}`);
    }
  }

  const setText = (element, value) => {
    if (element) element.textContent = value;
  };

  const setProgress = (value, label) => {
    const normalized = Number.isFinite(value) ? Math.max(0, Math.min(100, value)) : 0;
    if (elements.progressBar) elements.progressBar.value = normalized;
    setText(elements.progressText, label || `${Math.round(normalized)}%`);
  };

  const addLog = (message, type = 'info') => {
    const time = new Date().toLocaleTimeString('es-CO', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
    const line = `[${time}] ${message}`;
    console.log(`[Renderer:Log] ${line}`);

    if (!elements.logOutput) return;

    const node = document.createElement('p');
    node.className = `log-line ${type}`;
    node.textContent = line;
    elements.logOutput.appendChild(node);
    elements.logOutput.scrollTop = elements.logOutput.scrollHeight;
  };

  const setApiState = (state, label) => {
    if (!elements.apiPill) return;

    elements.apiPill.classList.remove('online', 'offline');
    if (state) elements.apiPill.classList.add(state);
    setText(elements.apiState, label);
  };

  const setPlayState = ({ enabled, title, subtitle }) => {
    if (elements.playButton) elements.playButton.disabled = !enabled;
    setText(elements.playTitle, title);
    setText(elements.playSubtitle, subtitle);
  };

  const setStatus = (message, type = 'info') => {
    setText(elements.statusMessage, message);
    addLog(message, type);
  };

  const summarizePackages = (updates, manifest) => {
    const packages = manifest?.packages || updates || [];
    if (!packages.length) return 'Sin paquetes pendientes';
    return packages.map(item => item.name || item.file).filter(Boolean).join(', ');
  };

  const updateManifestUi = ({ neededUpdates, localVersion, latestVersion, source, manifest }) => {
    const launcher = manifest?.launcher;
    const sourceLabel = source === 'api' ? 'API local' : source === 'xml' ? 'XML legacy' : 'Desconocido';

    setText(elements.localVersion, localVersion || '--');
    setText(elements.latestVersion, latestVersion || manifest?.version || '--');
    setText(elements.packageValue, summarizePackages(neededUpdates, manifest));
    setText(elements.sourceBadge, `Fuente: ${sourceLabel}`);
    setText(elements.launcherStatus, launcher?.status || (source === 'api' ? 'online' : 'fallback'));
    setText(elements.minimumVersion, `Minimo ${launcher?.minimumVersion || '1.0.0'}`);
    setText(elements.serverStatus, source === 'api' ? 'ESTABLE' : 'FALLBACK');

    if (source === 'api') {
      setApiState('online', 'API online');
      addLog('API online', 'success');
      addLog(`Manifest recibido: version ${manifest?.version || latestVersion}`, 'success');
    } else {
      setApiState('offline', 'API offline');
      addLog('API offline: usando Updates.xml legacy', 'warn');
    }

    addLog(`Version detectada: local=${localVersion}, latest=${latestVersion}`, 'info');
  };

  document.querySelectorAll('.social-button, .open-link').forEach(element => {
    element.addEventListener('click', (event) => {
      event.preventDefault();
      const url = event.currentTarget.dataset.url || event.currentTarget.href;

      if (!url) {
        addLog('No se encontro URL para abrir.', 'error');
        return;
      }

      if (window.api?.openUrl) {
        window.api.openUrl(url);
        addLog(`Abriendo enlace externo: ${url}`);
      } else {
        window.open(url, '_blank');
        addLog(`Abriendo enlace en navegador: ${url}`, 'warn');
      }
    });
  });

  elements.minimizeButton?.addEventListener('click', () => window.api?.minimize?.());
  elements.closeButton?.addEventListener('click', () => window.api?.close?.());
  elements.clearLogsButton?.addEventListener('click', () => {
    if (elements.logOutput) elements.logOutput.innerHTML = '';
    addLog('Registro limpiado.');
  });

  document.addEventListener('mousemove', (event) => {
    const layer = document.querySelector('.scene-layer img');
    if (!layer) return;

    const x = event.clientX / window.innerWidth - 0.5;
    const y = event.clientY / window.innerHeight - 0.5;
    layer.style.transform = `scale(1.04) translate(${x * 8}px, ${y * 8}px)`;
  });

  if (window.api?.onDownloadProgress) {
    window.api.onDownloadProgress((data) => {
      const { progress, receivedMB, totalMB, speed } = data;
      setProgress(progress, `${progress}%`);
      setText(
        elements.statusMessage,
        `Descargando ${progress}% - ${receivedMB} de ${totalMB} MB - ${speed} KB/s`
      );
    });
  } else {
    addLog('window.api.onDownloadProgress no esta disponible.', 'error');
  }

  if (window.api?.onExtractProgress) {
    window.api.onExtractProgress((data) => {
      const { progress, current, total } = data;
      setProgress(progress, `${progress}%`);

      if (total > 0) {
        setText(elements.statusMessage, `Extrayendo archivos... ${progress}% (${current}/${total})`);
      } else {
        setText(elements.statusMessage, `Extrayendo archivos... ${progress}%`);
      }
    });
  } else {
    addLog('window.api.onExtractProgress no esta disponible.', 'error');
  }

  try {
    if (!window.api?.checkUpdates) {
      throw new Error('La API de verificacion de actualizaciones no esta disponible.');
    }

    setApiState('', 'Conectando');
    setPlayState({ enabled: false, title: 'Actualizando', subtitle: 'Verificando API' });
    setProgress(0, '0%');
    setStatus('Verificando actualizaciones...');

    const updateResult = await window.api.checkUpdates();
    const { neededUpdates, localVersion, latestVersion, error, source, manifest } = updateResult;

    if (error) {
      throw new Error(`Error al verificar: ${error}`);
    }

    console.log('Resultado de checkUpdates:', updateResult);
    updateManifestUi({ neededUpdates, localVersion, latestVersion, source, manifest });

    if (neededUpdates && neededUpdates.length > 0) {
      setPlayState({ enabled: false, title: 'Actualizando', subtitle: 'Por favor espera' });
      setStatus(`Preparando ${neededUpdates.length} paquete(s)...`);

      if (!window.api?.downloadFile) throw new Error('La API de descarga no esta disponible.');
      if (!window.api?.extractZip) throw new Error('La API de extraccion no esta disponible.');

      for (const update of neededUpdates) {
        if (!update.file || typeof update.file !== 'string' || update.file.trim() === '') {
          addLog(`Paquete invalido omitido: ${JSON.stringify(update)}`, 'warn');
          continue;
        }

        if (!update.url || typeof update.url !== 'string') {
          throw new Error(`La actualizacion ${update.file} no incluye una URL de descarga valida.`);
        }

        const downloadPayload = {
          url: update.url,
          fileName: update.file
        };

        setProgress(0, '0%');
        setStatus(`Descarga iniciada: ${update.file}`);
        addLog(`URL: ${downloadPayload.url}`);

        const filePath = await window.api.downloadFile(downloadPayload);
        if (!filePath) {
          throw new Error(`La descarga de ${update.file} no devolvio una ruta valida.`);
        }

        addLog(`Descarga completada: ${filePath}`, 'success');
        setStatus(`Extrayendo ${update.file}...`);

        const extractionResult = await window.api.extractZip(filePath);
        if (!extractionResult || !extractionResult.success) {
          throw new Error(`La extraccion de ${update.file} fallo.`);
        }

        addLog(`Extraccion completada: ${update.file}`, 'success');
      }

      if (!window.api?.updateLocalVersion) {
        throw new Error('La API de actualizacion de version no esta disponible.');
      }

      await window.api.updateLocalVersion(latestVersion);
      setText(elements.localVersion, latestVersion || '--');
      setProgress(100, '100%');
      setStatus('Actualizacion completada.', 'success');
    } else {
      setProgress(100, '100%');
      setStatus('Cliente actualizado.', 'success');
    }

    setPlayState({ enabled: true, title: 'JUGAR', subtitle: 'Cliente listo' });
  } catch (error) {
    console.error('Error en el proceso principal del renderer:', error);
    setApiState('offline', 'Error runtime');
    setProgress(0, '0%');
    setStatus(`Error: ${error.message.replace(/^Error:\s*/, '')}`, 'error');
    setPlayState({ enabled: false, title: 'Bloqueado', subtitle: 'Revisa registros' });
  }

  elements.playButton?.addEventListener('click', async () => {
    if (!window.api?.launchGame) {
      setStatus('No se puede iniciar el juego: IPC no disponible.', 'error');
      return;
    }

    setStatus('Lanzando Dofus.exe...');

    try {
      const result = await window.api.launchGame();
      console.log('Resultado de launchGame:', result);

      if (!result.success) {
        throw new Error(result.error || 'Fallo desconocido al iniciar el juego.');
      }

      setStatus('Se abrio una ventana del cliente.', 'success');
    } catch (error) {
      console.error('Error al lanzar el juego:', error);
      setStatus(`Error al iniciar: ${error.message.replace(/^Error:\s*/, '')}`, 'error');
    }
  });
});
