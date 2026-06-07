import { ParamMap, Params } from '@angular/router';

import {
  SpellSearchRequest,
  createEmptySpellSearchRequest
} from './spells.models';

export const DEFAULT_SPELLS_PAGE_SIZE = 20;

export function readSpellSearchRequest(paramMap: ParamMap): SpellSearchRequest {
  const defaults = createEmptySpellSearchRequest();

  return {
    search: readOptionalText(paramMap, 'search'),
    spellId: readOptionalPositiveInt(paramMap, 'spellId'),
    breedId: readOptionalPositiveInt(paramMap, 'breedId'),
    typeId: readOptionalNonNegativeInt(paramMap, 'typeId'),
    page: readOptionalPositiveInt(paramMap, 'page') ?? defaults.page,
    pageSize: readOptionalPositiveInt(paramMap, 'pageSize') ?? defaults.pageSize
  };
}

export function toSpellQueryParams(request: SpellSearchRequest): Params {
  const query = normalizeSpellSearchRequest(request);
  const params: Params = {
    page: query.page,
    pageSize: query.pageSize
  };

  if (query.search) {
    params['search'] = query.search;
  }

  if (query.spellId) {
    params['spellId'] = query.spellId;
  }

  if (query.breedId) {
    params['breedId'] = query.breedId;
  }

  if (query.typeId !== undefined) {
    params['typeId'] = query.typeId;
  }

  return params;
}

export function normalizeSpellSearchRequest(request: SpellSearchRequest): SpellSearchRequest {
  const defaults = createEmptySpellSearchRequest();

  return {
    search: normalizeOptionalText(request.search),
    spellId: normalizePositiveInt(request.spellId),
    breedId: normalizePositiveInt(request.breedId),
    typeId: normalizeNonNegativeInt(request.typeId),
    page: normalizePositiveInt(request.page) ?? defaults.page,
    pageSize: normalizePageSize(request.pageSize) ?? defaults.pageSize
  };
}

function readOptionalText(paramMap: ParamMap, key: string): string | undefined {
  return normalizeOptionalText(paramMap.get(key) ?? undefined);
}

function readOptionalPositiveInt(paramMap: ParamMap, key: string): number | undefined {
  return normalizePositiveInt(paramMap.get(key) ?? undefined);
}

function readOptionalNonNegativeInt(paramMap: ParamMap, key: string): number | undefined {
  return normalizeNonNegativeInt(paramMap.get(key) ?? undefined);
}

function normalizeOptionalText(value: string | null | undefined): string | undefined {
  if (!value) {
    return undefined;
  }

  const normalized = value.trim();
  return normalized.length > 0 ? normalized : undefined;
}

function normalizePositiveInt(value: number | string | null | undefined): number | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
}

function normalizeNonNegativeInt(value: number | string | null | undefined): number | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= 0 ? parsed : undefined;
}

function normalizePageSize(value: number | string | null | undefined): number | undefined {
  const parsed = normalizePositiveInt(value);

  if (!parsed) {
    return undefined;
  }

  return Math.min(parsed, 100);
}
