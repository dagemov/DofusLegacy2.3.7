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
    langPicker: document.getElementById('lang-picker'),
    langPickerToggle: document.getElementById('lang-picker-toggle'),
    langPickerMenu: document.getElementById('lang-picker-menu'),
    langPickerCurrentFlag: document.getElementById('lang-picker-current-flag'),
    clearLogsButton: document.getElementById('clear-logs-button'),
    minimizeButton: document.getElementById('minimize-button'),
    closeButton: document.getElementById('close-button')
  };

  const authElements = {
    titlebarAccount: document.getElementById('titlebar-account'),
    titlebarUsername: document.getElementById('titlebar-username'),
    accountProfileCenter: document.getElementById('account-profile-center'),
    accountCenterUsername: document.getElementById('account-center-username'),
    homeAuthGuest: document.getElementById('home-auth-guest'),
    authTabLogin: document.getElementById('auth-tab-login'),
    authTabRegister: document.getElementById('auth-tab-register'),
    authPaneLogin: document.getElementById('auth-pane-login'),
    authPaneRegister: document.getElementById('auth-pane-register'),
    loginForm: document.getElementById('auth-login-form'),
    registerForm: document.getElementById('auth-register-form'),
    logoutButton: document.getElementById('auth-logout-button'),
    openFolderAccount: document.getElementById('open-client-folder-account'),
    feedback: document.getElementById('auth-feedback'),
    usernameInput: document.getElementById('auth-username')
  };

  const setActiveView = (viewName) => {
    document.querySelectorAll('.nav-item[data-view]').forEach((item) => {
      item.classList.toggle('active', item.getAttribute('data-view') === viewName);
    });

    document.querySelectorAll('.view-pane').forEach((pane) => {
      pane.classList.toggle('active', pane.id === `view-${viewName}`);
    });
  };

  const openClientFolder = async () => {
    if (!window.api?.openClientFolder) {
      addLog('No se puede abrir la carpeta del cliente.', 'error');
      return;
    }
    try {
      const result = await window.api.openClientFolder();
      addLog(`Carpeta del cliente: ${result?.path || ''}`, 'success');
    } catch (error) {
      addLog(`Error al abrir carpeta: ${error.message}`, 'error');
    }
  };

  const setAuthFeedback = (message, type = 'info') => {
    if (!authElements.feedback) return;
    authElements.feedback.textContent = message;
    authElements.feedback.className = `auth-form__feedback auth-form__feedback--inline ${type}`;
  };

  const setAuthTab = (tabName) => {
    const isLogin = tabName === 'login';

    authElements.authTabLogin?.classList.toggle('active', isLogin);
    authElements.authTabRegister?.classList.toggle('active', !isLogin);
    authElements.authTabLogin?.setAttribute('aria-selected', String(isLogin));
    authElements.authTabRegister?.setAttribute('aria-selected', String(!isLogin));

    authElements.authPaneLogin?.classList.toggle('active', isLogin);
    authElements.authPaneRegister?.classList.toggle('active', !isLogin);
  };

  document.querySelectorAll('[data-auth-tab]').forEach((button) => {
    button.addEventListener('click', () => {
      setAuthTab(button.getAttribute('data-auth-tab'));
    });
  });

  const refreshSessionUi = async () => {
    const session = await window.api?.getSession?.();
    const isLoggedIn = Boolean(session?.username);
    const displayName = session?.nickname || session?.username || '—';

    if (authElements.titlebarAccount) {
      authElements.titlebarAccount.hidden = !isLoggedIn;
    }
    if (authElements.titlebarUsername) {
      authElements.titlebarUsername.textContent = isLoggedIn ? displayName : '—';
    }
    if (authElements.accountProfileCenter) {
      authElements.accountProfileCenter.hidden = !isLoggedIn;
    }
    if (authElements.accountCenterUsername) {
      authElements.accountCenterUsername.textContent = isLoggedIn ? displayName : '—';
    }
    if (authElements.homeAuthGuest) {
      authElements.homeAuthGuest.classList.toggle('is-hidden', isLoggedIn);
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
    const username = document.getElementById('register-username')?.value?.trim()
      || document.getElementById('auth-username')?.value?.trim();
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
      setAuthTab('login');
      return;
    }

    setAuthFeedback(result?.message || 'No se pudo registrar.', 'error');
    addLog(`Registro fallido: ${result?.message || 'error'}`, 'error');
  });

  const handleLogout = async () => {
    await window.api?.authLogout?.();
    setAuthFeedback('Sesion cerrada.');
    await refreshSessionUi();
    addLog('Sesion cerrada.', 'info');
  };

  authElements.logoutButton?.addEventListener('click', handleLogout);
  authElements.openFolderAccount?.addEventListener('click', openClientFolder);
  authElements.titlebarAccount?.addEventListener('click', () => setActiveView('home'));

  document.querySelectorAll('.nav-item[data-view]').forEach((button) => {
    button.addEventListener('click', () => {
      setActiveView(button.getAttribute('data-view'));
    });
  });

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

    const maxLines = 80;
    while (elements.logOutput.childElementCount > maxLines) {
      elements.logOutput.removeChild(elements.logOutput.firstElementChild);
    }

    elements.logOutput.scrollTop = elements.logOutput.scrollHeight;
  };

  const FLAG_ICON_BY_CODE = {
    fr: 'fr',
    en: 'gb',
    de: 'de',
    es: 'es',
    it: 'it',
    pt: 'pt',
    jp: 'jp',
    nl: 'nl',
    ru: 'ru'
  };

  const languageState = {
    supportedLanguages: [],
    selected: 'es',
    applying: false
  };

  const getFlagIconPath = (code) => {
    const normalizedCode = String(code || '').trim().toLowerCase();
    const icon = FLAG_ICON_BY_CODE[normalizedCode] || normalizedCode || 'es';
    return `./icons/flags/${icon}.svg`;
  };

  const getLanguageLabel = (code) => {
    const normalizedCode = String(code || '').trim().toLowerCase();
    const language = languageState.supportedLanguages.find((item) => item.code === normalizedCode);
    return language?.label || normalizedCode.toUpperCase() || 'Desconocido';
  };

  const updateLangPickerCurrent = (code) => {
    if (!elements.langPickerCurrentFlag) return;
    const label = getLanguageLabel(code);
    elements.langPickerCurrentFlag.src = getFlagIconPath(code);
    elements.langPickerCurrentFlag.alt = label;
    if (elements.langPickerToggle) {
      elements.langPickerToggle.title = `Idioma: ${label}`;
      elements.langPickerToggle.setAttribute('aria-label', `Idioma del cliente: ${label}`);
    }
  };

  const closeLangMenu = () => {
    if (!elements.langPickerMenu || !elements.langPickerToggle) return;
    elements.langPickerMenu.hidden = true;
    elements.langPickerToggle.setAttribute('aria-expanded', 'false');
  };

  const openLangMenu = () => {
    if (!elements.langPickerMenu || !elements.langPickerToggle) return;
    elements.langPickerMenu.hidden = false;
    elements.langPickerToggle.setAttribute('aria-expanded', 'true');
  };

  const toggleLangMenu = () => {
    if (!elements.langPickerMenu) return;
    if (elements.langPickerMenu.hidden) openLangMenu();
    else closeLangMenu();
  };

  const renderLangMenu = () => {
    if (!elements.langPickerMenu) return;

    elements.langPickerMenu.innerHTML = '';

    languageState.supportedLanguages.forEach((language) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'lang-picker__option';
      button.setAttribute('role', 'menuitem');
      button.title = language.label;
      button.setAttribute('aria-label', language.label);

      if (language.code === languageState.selected) {
        button.classList.add('is-active');
        button.setAttribute('aria-current', 'true');
      }

      const img = document.createElement('img');
      img.src = getFlagIconPath(language.code);
      img.alt = '';
      img.width = 26;
      img.height = 17;
      button.appendChild(img);

      button.addEventListener('click', async (event) => {
        event.stopPropagation();
        if (language.code === languageState.selected || languageState.applying) {
          closeLangMenu();
          return;
        }
        await applyLanguagePreference(language.code);
        closeLangMenu();
      });

      elements.langPickerMenu.appendChild(button);
    });
  };

  const applyLanguagePreference = async (languageCode) => {
    if (!window.api?.setLanguagePreference) {
      addLog('IPC de idioma no disponible.', 'error');
      return;
    }

    if (languageState.applying) return;

    languageState.applying = true;
    const previousCode = languageState.selected;
    const targetLabel = getLanguageLabel(languageCode);
    addLog(`Aplicando idioma: ${targetLabel}...`, 'info');

    try {
      const result = await window.api.setLanguagePreference(languageCode);

      if (result?.success) {
        languageState.selected = result.selected || languageCode;
        updateLangPickerCurrent(languageState.selected);
        renderLangMenu();
        addLog(result.message || `Idioma actualizado: ${result.label || targetLabel}`, 'success');
        if (typeof result.updatedConfigCount === 'number') {
          addLog(`Archivos de configuracion actualizados: ${result.updatedConfigCount}`, 'info');
        }
        return;
      }

      addLog(result?.message || 'No se pudo guardar el idioma.', 'error');
    } catch (error) {
      addLog(`Error al cambiar idioma: ${error.message}`, 'error');
      languageState.selected = previousCode;
      updateLangPickerCurrent(previousCode);
      renderLangMenu();
    } finally {
      languageState.applying = false;
    }
  };

  const loadLanguageSettings = async () => {
    if (!window.api?.getLanguageSettings) {
      addLog('IPC de idioma no disponible.', 'error');
      return;
    }

    const result = await window.api.getLanguageSettings();
    languageState.supportedLanguages = result?.supportedLanguages || [];
    languageState.selected = result?.selected || 'es';

    updateLangPickerCurrent(languageState.selected);
    renderLangMenu();

    if (result?.error) {
      addLog(`No se pudo leer el idioma actual: ${result.error}`, 'error');
      return;
    }

    const label = getLanguageLabel(languageState.selected);
    if (result?.clientConfigFound) {
      addLog(
        result.hasStoredPreference
          ? `Idioma del cliente: ${label} (guardado en launcher y cliente).`
          : `Idioma del cliente: ${label} (leido desde config del cliente).`,
        'info'
      );
    } else {
      addLog(`Idioma preferido: ${label}. Se aplicara al terminar la descarga del cliente.`, 'info');
    }
  };

  const initLangPicker = () => {
    elements.langPickerToggle?.addEventListener('click', (event) => {
      event.stopPropagation();
      toggleLangMenu();
    });

    document.addEventListener('click', (event) => {
      if (!elements.langPicker?.contains(event.target)) {
        closeLangMenu();
      }
    });

    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') closeLangMenu();
    });
  };

  initLangPicker();
  setAuthTab('login');
  setActiveView(document.querySelector('.nav-item[data-view].active')?.getAttribute('data-view') || 'home');

  await Promise.all([
    refreshSessionUi(),
    loadLanguageSettings()
  ]);

  const setApiState = (state, label) => {
    if (!elements.apiPill) return;

    elements.apiPill.classList.remove('online', 'offline');
    if (state) elements.apiPill.classList.add(state);
    setText(elements.apiState, label);
  };

  const setPlayState = ({ enabled, title, subtitle }) => {
    if (elements.playButton) elements.playButton.disabled = !enabled;
    setText(elements.playTitle, title);
    if (elements.playSubtitle) {
      elements.playSubtitle.textContent = subtitle || '';
      elements.playSubtitle.hidden = !subtitle;
    }
  };

  const refreshPlayAvailability = async (latestVersion) => {
    const clientState = await window.api?.checkClientReady?.();

    if (clientState?.ready) {
      setPlayState({
        enabled: true,
        title: 'JUGAR',
        subtitle: 'Cliente listo'
      });
      return true;
    }

    setPlayState({
      enabled: false,
      title: 'Instalar',
      subtitle: 'Descarga pendiente'
    });
    setStatus('Cliente no instalado. Espera a que termine la descarga e instalacion.', 'warn');
    addLog(`Dofus.exe no encontrado. Version remota: ${latestVersion || '--'}`, 'warn');
    return false;
  };

  const setStatus = (message, type = 'info') => {
    setText(elements.statusMessage, message);
    addLog(message, type);
  };

  const summarizePackages = (updates) => {
    if (!updates?.length) return 'Sin paquetes pendientes';
    return updates.map(item => item.file || item.name).filter(Boolean).join(', ');
  };

  const updateManifestUi = ({ neededUpdates, localVersion, latestVersion, source, manifest, apiOnline }) => {
    const launcher = manifest?.launcher;
    const sourceLabel = 'Parches VPS (8090)';

    setText(elements.localVersion, localVersion || '--');
    setText(elements.latestVersion, latestVersion || manifest?.version || '--');
    setText(elements.packageValue, summarizePackages(neededUpdates));
    setText(elements.sourceBadge, `Fuente: ${sourceLabel}`);
    setText(elements.launcherStatus, launcher?.status || (apiOnline ? 'online' : 'parches'));
    setText(elements.minimumVersion, `Minimo ${launcher?.minimumVersion || '1.0.0'}`);
    setText(elements.serverStatus, apiOnline ? 'ESTABLE' : 'PARCHES');

    if (apiOnline) {
      setApiState('online', 'API online');
      addLog('Parches: Updates.xml VPS', 'success');
      if (manifest?.version) {
        addLog(`API informativa: ${manifest.version}`, 'info');
      }
    } else {
      setApiState('offline', 'Solo parches');
      addLog('Parches VPS (8090); API informativa offline', 'warn');
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

  window.api?.onLauncherUpdateStatus?.((data) => {
    if (!data?.message) return;
    const type = data.phase === 'error' ? 'error' : data.phase === 'downloaded' ? 'success' : 'info';
    addLog(data.message, type);
  });

  window.api?.onLauncherUpdateProgress?.((data) => {
    const percent = data?.percent ?? 0;
    setProgress(percent, `${percent}%`);
    setText(elements.statusMessage, `Actualizando launcher... ${percent}%`);
    setPlayState({ enabled: false, title: 'Launcher', subtitle: 'Descargando actualizacion' });
  });

  try {
    if (!window.api?.checkUpdates) {
      throw new Error('La API de verificacion de actualizaciones no esta disponible.');
    }

    setApiState('', 'Conectando');
    setPlayState({ enabled: false, title: 'Actualizando', subtitle: 'Verificando cliente' });
    setProgress(0, '0%');
    setStatus('Cargando datos del launcher...');

    const updateResult = await Promise.all([
      window.api?.whenLauncherReady?.() ?? Promise.resolve(),
      window.api.checkUpdates()
    ]).then(([, result]) => result);
    const { neededUpdates, localVersion, latestVersion, error, source, manifest } = updateResult;

    if (error) {
      throw new Error(`Error al verificar: ${error}`);
    }

    console.log('Resultado de checkUpdates:', updateResult);
    updateManifestUi({
      neededUpdates,
      localVersion,
      latestVersion,
      source,
      manifest,
      apiOnline: updateResult.apiOnline
    });

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

        if (!window.api?.updateLocalVersion) {
          throw new Error('La API de actualizacion de version no esta disponible.');
        }

        await window.api.updateLocalVersion(update.version);
        setText(elements.localVersion, update.version || '--');
        addLog(`Version local actualizada a ${update.version}`, 'success');
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

    await refreshPlayAvailability(latestVersion);
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
