import { ParamMap, Params } from '@angular/router';

import { ItemSearchRequest, createEmptyItemSearchRequest } from './items.models';

export const DEFAULT_ITEMS_PAGE_SIZE = 20;

export function readItemSearchRequest(paramMap: ParamMap): ItemSearchRequest {
  const defaults = createEmptyItemSearchRequest();

  return {
    search: readOptionalText(paramMap, 'search'),
    itemId: readOptionalPositiveInt(paramMap, 'itemId'),
    iconId: readOptionalPositiveInt(paramMap, 'iconId'),
    typeId: readOptionalPositiveInt(paramMap, 'typeId'),
    levelMin: readOptionalNonNegativeInt(paramMap, 'levelMin'),
    levelMax: readOptionalNonNegativeInt(paramMap, 'levelMax'),
    page: readOptionalPositiveInt(paramMap, 'page') ?? defaults.page,
    pageSize: readOptionalPositiveInt(paramMap, 'pageSize') ?? defaults.pageSize
  };
}

export function toItemQueryParams(request: ItemSearchRequest): Params {
  const query = normalizeItemSearchRequest(request);
  const params: Params = {
    page: query.page,
    pageSize: query.pageSize
  };

  if (query.search) {
    params['search'] = query.search;
  }

  if (query.itemId) {
    params['itemId'] = query.itemId;
  }

  if (query.iconId) {
    params['iconId'] = query.iconId;
  }

  if (query.typeId) {
    params['typeId'] = query.typeId;
  }

  if (query.levelMin !== undefined) {
    params['levelMin'] = query.levelMin;
  }

  if (query.levelMax !== undefined) {
    params['levelMax'] = query.levelMax;
  }

  return params;
}

export function normalizeItemSearchRequest(request: ItemSearchRequest): ItemSearchRequest {
  const defaults = createEmptyItemSearchRequest();

  return {
    search: normalizeOptionalText(request.search),
    itemId: normalizePositiveInt(request.itemId),
    iconId: normalizePositiveInt(request.iconId),
    typeId: normalizePositiveInt(request.typeId),
    levelMin: normalizeNonNegativeInt(request.levelMin),
    levelMax: normalizeNonNegativeInt(request.levelMax),
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
