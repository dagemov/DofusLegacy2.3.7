export type ClipboardCopyStatus = 'copied' | 'manual' | 'unavailable';

export interface ClipboardSupportInfo {
  canUseNavigatorClipboard: boolean;
  canAttemptLegacyCopy: boolean;
  isSecureContext: boolean;
  helpText: string | null;
}

export function getClipboardSupportInfo(): ClipboardSupportInfo {
  const view = typeof document !== 'undefined' ? document.defaultView : undefined;
  const isSecureContext = !!view?.isSecureContext;
  const canUseNavigatorClipboard = !!view?.navigator?.clipboard?.writeText && isSecureContext;
  const canAttemptLegacyCopy = typeof document !== 'undefined' && typeof document.execCommand === 'function';

  if (canUseNavigatorClipboard) {
    return {
      canUseNavigatorClipboard,
      canAttemptLegacyCopy,
      isSecureContext,
      helpText: null
    };
  }

  if (canAttemptLegacyCopy) {
    return {
      canUseNavigatorClipboard,
      canAttemptLegacyCopy,
      isSecureContext,
      helpText:
        'La escritura al portapapeles está limitada en este navegador. Si el botón no puede copiar automáticamente, usa el valor visible para copiar manualmente.'
    };
  }

    return {
      canUseNavigatorClipboard,
      canAttemptLegacyCopy,
      isSecureContext,
      helpText:
      'La escritura al portapapeles no está disponible en este navegador. Usa el valor visible para copiar manualmente.'
  };
}

export async function copyTextToClipboard(text: string): Promise<ClipboardCopyStatus> {
  if (!text || text.trim().length === 0) {
    return 'unavailable';
  }

  const support = getClipboardSupportInfo();

  if (support.canUseNavigatorClipboard && typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text);
      return 'copied';
    } catch {
      // Fall back to a DOM-based copy path when clipboard permissions are unavailable.
    }
  }

  if (typeof document === 'undefined') {
    return 'unavailable';
  }

  const textArea = document.createElement('textarea');
  textArea.value = text;
  textArea.setAttribute('readonly', 'true');
  textArea.style.position = 'fixed';
  textArea.style.opacity = '0';

  document.body.appendChild(textArea);
  textArea.focus();
  textArea.select();

  try {
    if (!support.canAttemptLegacyCopy) {
      return 'manual';
    }

    return document.execCommand('copy') ? 'copied' : 'manual';
  } catch {
    return 'manual';
  } finally {
    document.body.removeChild(textArea);
  }
}
