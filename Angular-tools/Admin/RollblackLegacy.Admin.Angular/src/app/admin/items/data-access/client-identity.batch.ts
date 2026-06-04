import { ClientItemIdentityCheckResultDto } from './client-identity.models';

export const CLIENT_IDENTITY_MAX_BATCH_IDS = 100;

export interface ClientIdentityBatchParseResult {
  ids: number[];
  error: string | null;
}

export function parseClientIdentityItemIds(raw: string): ClientIdentityBatchParseResult {
  const trimmed = raw.trim();
  if (!trimmed) {
    return { ids: [], error: 'Ingresa al menos un ItemId.' };
  }

  const tokens = trimmed.split(/[\s,;]+/).filter((token) => token.length > 0);
  if (tokens.length === 0) {
    return { ids: [], error: 'Ingresa al menos un ItemId.' };
  }

  const ids: number[] = [];
  const invalid: string[] = [];

  for (const token of tokens) {
    const value = Number.parseInt(token, 10);
    if (!Number.isInteger(value) || value <= 0) {
      invalid.push(token);
      continue;
    }

    ids.push(value);
  }

  if (invalid.length > 0) {
    return {
      ids: [],
      error: `IDs inválidos: ${invalid.join(', ')}. Usa enteros positivos.`
    };
  }

  const distinct = [...new Set(ids)];
  if (distinct.length > CLIENT_IDENTITY_MAX_BATCH_IDS) {
    return {
      ids: [],
      error: `Máximo ${CLIENT_IDENTITY_MAX_BATCH_IDS} IDs por auditoría. Recibiste ${distinct.length}.`
    };
  }

  return { ids: distinct, error: null };
}

export interface ClientIdentityStatusCount {
  code: string;
  label: string;
  count: number;
}

export function buildPrimaryStatusCounts(
  results: ClientItemIdentityCheckResultDto[]
): ClientIdentityStatusCount[] {
  const map = new Map<string, number>();

  for (const row of results) {
    const code = row.status.primaryStatus || 'UNKNOWN';
    map.set(code, (map.get(code) ?? 0) + 1);
  }

  return [...map.entries()]
    .map(([code, count]) => ({ code, label: code, count }))
    .sort((a, b) => b.count - a.count);
}

export function buildClientIdentityCsv(results: ClientItemIdentityCheckResultDto[]): string {
  const header = [
    'ItemId',
    'DbName',
    'ClientKnown',
    'PrimaryStatus',
    'NeedsClientPatch',
    'Statuses',
    'Warnings',
    'RecommendedAction',
    'IconPreviewFound'
  ];

  const rows = results.map((row) =>
    [
      row.itemId,
      csvEscape(row.dbName),
      row.clientKnown,
      csvEscape(row.status.primaryStatus),
      row.status.needsClientPatch,
      csvEscape(row.status.statuses.join('|')),
      csvEscape(row.status.warnings.join('|')),
      csvEscape(row.status.recommendedAction),
      row.iconPreviewFound
    ].join(',')
  );

  return [header.join(','), ...rows].join('\n');
}

function csvEscape(value: string | null | undefined): string {
  const normalized = value ?? '';
  if (/[",\n\r]/.test(normalized)) {
    return `"${normalized.replace(/"/g, '""')}"`;
  }

  return normalized;
}
