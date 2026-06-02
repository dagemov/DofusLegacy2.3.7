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
        'Clipboard write is limited in this browser context. If the button cannot copy automatically, use the visible value for manual copy.'
    };
  }

  return {
    canUseNavigatorClipboard,
    canAttemptLegacyCopy,
    isSecureContext,
    helpText:
      'Clipboard write is unavailable in this browser context. Use the visible value for manual copy.'
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
