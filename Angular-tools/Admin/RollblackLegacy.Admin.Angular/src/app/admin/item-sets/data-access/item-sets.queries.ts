import { ParamMap, Params } from '@angular/router';

import { ItemSetSearchRequest, createEmptyItemSetSearchRequest } from '../../items/data-access/items.models';

export const DEFAULT_ITEM_SETS_PAGE_SIZE = 20;

export function readItemSetSearchRequest(paramMap: ParamMap): ItemSetSearchRequest {
  const defaults = createEmptyItemSetSearchRequest();

  return {
    search: readOptionalText(paramMap, 'search'),
    minLevel: readOptionalNonNegativeInt(paramMap, 'minLevel'),
    maxLevel: readOptionalNonNegativeInt(paramMap, 'maxLevel'),
    minParts: readOptionalNonNegativeInt(paramMap, 'minParts'),
    maxParts: readOptionalNonNegativeInt(paramMap, 'maxParts'),
    page: readOptionalPositiveInt(paramMap, 'page') ?? defaults.page,
    pageSize: readOptionalPositiveInt(paramMap, 'pageSize') ?? defaults.pageSize
  };
}

export function toItemSetQueryParams(request: ItemSetSearchRequest): Params {
  const query = normalizeItemSetSearchRequest(request);
  const params: Params = {
    page: query.page,
    pageSize: query.pageSize
  };

  if (query.search) {
    params['search'] = query.search;
  }

  if (query.minLevel !== undefined) {
    params['minLevel'] = query.minLevel;
  }

  if (query.maxLevel !== undefined) {
    params['maxLevel'] = query.maxLevel;
  }

  if (query.minParts !== undefined) {
    params['minParts'] = query.minParts;
  }

  if (query.maxParts !== undefined) {
    params['maxParts'] = query.maxParts;
  }

  return params;
}

export function normalizeItemSetSearchRequest(request: ItemSetSearchRequest): ItemSetSearchRequest {
  return {
    ...request,
    search: request.search?.trim() || undefined,
    page: request.page > 0 ? request.page : 1,
    pageSize: request.pageSize > 0 && request.pageSize <= 100 ? request.pageSize : DEFAULT_ITEM_SETS_PAGE_SIZE
  };
}

function readOptionalText(paramMap: ParamMap, key: string): string | undefined {
  const value = paramMap.get(key)?.trim();
  return value ? value : undefined;
}

function readOptionalPositiveInt(paramMap: ParamMap, key: string): number | undefined {
  const raw = paramMap.get(key);
  if (!raw) {
    return undefined;
  }

  const parsed = Number(raw);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
}

function readOptionalNonNegativeInt(paramMap: ParamMap, key: string): number | undefined {
  const raw = paramMap.get(key);
  if (!raw) {
    return undefined;
  }

  const parsed = Number(raw);
  return Number.isInteger(parsed) && parsed >= 0 ? parsed : undefined;
}
