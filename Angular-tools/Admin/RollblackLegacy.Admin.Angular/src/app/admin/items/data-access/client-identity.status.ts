import {
  ClientIdentityStatusPresentation,
  ClientIdentityVisualTone,
  ClientItemIdentityCheckResultDto
} from './client-identity.models';

const STATUS_LABELS: Record<string, string> = {
  SAFE_EXISTING_TEMPLATE: 'Template seguro en cliente',
  CLIENT_KNOWN: 'Cliente conoce el ItemId',
  CLIENT_UNKNOWN: 'Cliente no conoce el ItemId',
  NEEDS_CLIENT_PATCH: 'Requiere parche cliente',
  APPEARANCE_UNKNOWN: 'AppearanceId desconocido',
  I18N_MISSING_ES: 'Falta i18n ES',
  I18N_MISSING_EN: 'Falta i18n EN',
  ICON_PREVIEW_FOUND: 'Preview icono encontrado',
  ICON_PREVIEW_MISSING: 'Preview icono ausente',
  ICON_MISSING: 'IconId ausente en DB',
  CLIENT_DATA_UNAVAILABLE: 'Metadata cliente no disponible',
  ERROR: 'Error'
};

export function presentClientIdentityStatus(code: string): ClientIdentityStatusPresentation {
  const normalized = (code || '').trim().toUpperCase();
  const tone = resolveTone(normalized);

  return {
    label: STATUS_LABELS[normalized] ?? normalized.replaceAll('_', ' '),
    tone,
    badgeClass: toneToBadgeClass(tone)
  };
}

export function resolvePrimaryTone(result: ClientItemIdentityCheckResultDto | null): ClientIdentityVisualTone {
  if (!result) {
    return 'neutral';
  }

  return presentClientIdentityStatus(result.status.primaryStatus).tone;
}

export function resolveCardAccentClass(result: ClientItemIdentityCheckResultDto | null): string {
  switch (resolvePrimaryTone(result)) {
    case 'success':
      return 'client-identity-card--success';
    case 'warning':
      return 'client-identity-card--warning';
    case 'danger':
      return 'client-identity-card--danger';
    case 'info':
      return 'client-identity-card--info';
    default:
      return 'client-identity-card--neutral';
  }
}

function resolveTone(code: string): ClientIdentityVisualTone {
  if (code === 'SAFE_EXISTING_TEMPLATE' || code === 'CLIENT_KNOWN' || code === 'ICON_PREVIEW_FOUND') {
    return 'success';
  }

  if (
    code === 'CLIENT_UNKNOWN' ||
    code === 'NEEDS_CLIENT_PATCH' ||
    code === 'APPEARANCE_UNKNOWN' ||
    code === 'ICON_PREVIEW_MISSING' ||
    code === 'ICON_MISSING'
  ) {
    return 'warning';
  }

  if (code === 'I18N_MISSING_ES' || code === 'I18N_MISSING_EN') {
    return 'info';
  }

  if (code === 'CLIENT_DATA_UNAVAILABLE' || code === 'ERROR') {
    return 'danger';
  }

  return 'neutral';
}

function toneToBadgeClass(tone: ClientIdentityVisualTone): string {
  switch (tone) {
    case 'success':
      return 'text-bg-success';
    case 'warning':
      return 'text-bg-warning';
    case 'danger':
      return 'text-bg-danger';
    case 'info':
      return 'text-bg-info';
    default:
      return 'text-bg-secondary';
  }
}
