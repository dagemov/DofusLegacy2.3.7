import { HttpErrorResponse } from '@angular/common/http';

export interface AdminApiProblem {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface SpellSearchRequest {
  search?: string;
  spellId?: number;
  breedId?: number;
  typeId?: number;
  page: number;
  pageSize: number;
}

export interface SpellPagedResultDto<TItem> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: TItem[];
}

export interface SpellBreedSummaryDto {
  breedId: number;
  label?: string | null;
}

export interface SpellCatalogItemDto {
  spellId: number;
  name?: string | null;
  description?: string | null;
  typeId?: number | null;
  typeLabel?: string | null;
  iconId?: number | null;
  breeds: SpellBreedSummaryDto[];
  levelCount: number;
  runtimeAvailable: boolean;
  referenceAvailable: boolean;
}

export function createEmptySpellSearchRequest(): SpellSearchRequest {
  return {
    page: 1,
    pageSize: 20
  };
}

export function createEmptyPagedResult<TItem>(
  page = 1,
  pageSize = 20
): SpellPagedResultDto<TItem> {
  return {
    page,
    pageSize,
    totalCount: 0,
    items: []
  };
}

export function toAdminApiProblem(error: unknown): AdminApiProblem {
  if (error instanceof HttpErrorResponse) {
    const payload =
      error.error && typeof error.error === 'object'
        ? (error.error as Partial<AdminApiProblem>)
        : undefined;
    const isNetworkFailure = error.status === 0;
    const responseText =
      typeof error.error === 'string'
        ? error.error
        : typeof error.error === 'object' && error.error !== null && 'text' in error.error
          ? String((error.error as { text?: unknown }).text ?? '')
          : '';
    const isHttp200HtmlResponse = error.status === 200 && looksLikeHtmlDocument(responseText);
    const isHttp200ParseFailure =
      error.status === 200 &&
      (isHttp200HtmlResponse ||
        error.error instanceof Error ||
        (!!error.message && error.message.toLowerCase().includes('parsing')));

    const title =
      normalizeProblemText(payload?.title) ||
      (isNetworkFailure
        ? 'No se pudo conectar con el Admin API.'
        : isHttp200HtmlResponse
          ? 'El proxy de desarrollo no está enviando /api al Admin API.'
          : isHttp200ParseFailure
            ? 'La respuesta del servidor no tiene el formato esperado.'
            : 'No se pudo completar la solicitud.');

    const detail =
      normalizeProblemText(payload?.detail) ||
      (isNetworkFailure
        ? 'Verifica que el Admin API esté levantado y que el proxy apunte a la URL correcta.'
        : undefined) ||
      (isHttp200HtmlResponse
        ? 'Se recibió HTML en lugar de JSON. Usa npm start o ng serve con proxy.conf.json y confirma que RollblackLegacy.Admin.Api escucha en http://localhost:5248.'
        : undefined) ||
      (isHttp200ParseFailure
        ? 'La solicitud respondió HTTP 200, pero el contenido no pudo interpretarse como JSON.'
        : undefined) ||
      normalizeProblemText(typeof error.error === 'string' ? error.error : undefined);

    return {
      type: payload?.type,
      title,
      status: payload?.status ?? error.status,
      detail: normalizeProblemDetail(title, detail),
      instance: payload?.instance,
      traceId: readTraceId(payload, error),
      errors: readValidationErrors(payload)
    };
  }

  if (error instanceof Error) {
    return {
      title: 'Error inesperado del cliente',
      detail: error.message
    };
  }

  return {
    title: 'Error inesperado del cliente',
    detail: 'La solicitud falló antes de poder interpretar una respuesta del servidor.'
  };
}

function looksLikeHtmlDocument(value: string): boolean {
  const normalized = value.trimStart().toLowerCase();
  return (
    normalized.startsWith('<!doctype html') ||
    normalized.startsWith('<html') ||
    normalized.includes('<!doctype html')
  );
}

function normalizeProblemText(value: string | undefined): string | undefined {
  if (!value) {
    return undefined;
  }

  const normalized = value.trim();
  return normalized.length > 0 ? normalized : undefined;
}

function normalizeProblemDetail(title: string, detail: string | undefined): string | undefined {
  if (!detail) {
    return undefined;
  }

  if (detail === title) {
    return undefined;
  }

  if (
    detail === 'No se pudo completar la solicitud.' &&
    title === 'No se pudo completar la solicitud.'
  ) {
    return undefined;
  }

  return detail;
}

function readTraceId(
  payload: Partial<AdminApiProblem> | undefined,
  error?: HttpErrorResponse
): string | undefined {
  const fromPayload = payload?.traceId;
  if (typeof fromPayload === 'string' && fromPayload.trim().length > 0) {
    return fromPayload.trim();
  }

  const fromBody =
    error?.error && typeof error.error === 'object'
      ? (error.error as { traceId?: unknown }).traceId
      : undefined;
  if (typeof fromBody === 'string' && fromBody.trim().length > 0) {
    return fromBody.trim();
  }

  return undefined;
}

function readValidationErrors(
  payload: Partial<AdminApiProblem> | undefined
): Record<string, string[]> | undefined {
  if (!payload?.errors || typeof payload.errors !== 'object') {
    return undefined;
  }

  const result: Record<string, string[]> = {};
  for (const [key, value] of Object.entries(payload.errors)) {
    if (Array.isArray(value)) {
      result[key] = value.map((entry) => `${entry}`);
    }
  }

  return Object.keys(result).length > 0 ? result : undefined;
}
