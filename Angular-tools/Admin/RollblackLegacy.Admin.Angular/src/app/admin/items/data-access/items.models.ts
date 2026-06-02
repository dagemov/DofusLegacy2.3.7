import { HttpErrorResponse } from '@angular/common/http';

export interface AdminOptionDto {
  value: number;
  label: string;
}

export interface AdminWarningLike {
  code: string;
  severity: string;
  message: string;
  field?: string | null;
}

export interface ItemSearchRequest {
  search?: string;
  itemId?: number;
  iconId?: number;
  typeId?: number;
  levelMin?: number;
  levelMax?: number;
  page: number;
  pageSize: number;
}

export interface ItemIconSearchRequest {
  search?: string;
  iconId?: number;
  page: number;
  pageSize: number;
}

export interface ItemPagedResultDto<TItem> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: TItem[];
}

export interface ItemIconOptionDto {
  iconId: number;
  previewPath?: string | null;
  source: string;
  hasPreview: boolean;
  linkedItemCount?: number | null;
  sampleItemNames: string[];
}

export interface ItemIconSelection {
  iconId: number;
  previewPath: string | null;
}

export interface ItemPreviewStateDto {
  state: string;
  byItemPath: string;
  byIconPath: string;
  manualPath: string;
  previewSource: string;
  resolvedPath?: string | null;
  fallbackUsed: string;
}

export interface ItemWarningDto extends AdminWarningLike {}

export interface ItemSetLinkDto {
  setId: number;
  setName?: string | null;
  state: string;
}

export interface ItemEffectDto {
  effectId: number;
  diceNum: number;
  diceSide: number;
  value: number;
  description: string;
}

export interface ItemClientIdentityDto {
  itemId: number;
  clientNameId?: number | null;
  clientName?: string | null;
  iconId?: number | null;
  appearanceId?: number | null;
  source: string;
  confidence: number;
}

export interface ItemListItemDto {
  itemId: number;
  resolvedName?: string | null;
  typeId: number;
  typeName?: string | null;
  level: number;
  setId?: number | null;
  setName?: string | null;
  iconId: number;
  appearanceId: number;
  previewState: ItemPreviewStateDto;
  warningCount: number;
}

export interface ItemDetailDto {
  itemId: number;
  resolvedName?: string | null;
  description?: string | null;
  descriptionId: number;
  typeId: number;
  typeName?: string | null;
  level: number;
  weight: number;
  price: number;
  usable: boolean;
  targetable: boolean;
  twoHanded: boolean;
  etheral: boolean;
  criteria?: string | null;
  iconId: number;
  appearanceId: number;
  set?: ItemSetLinkDto | null;
  clientIdentity: ItemClientIdentityDto;
  previewState: ItemPreviewStateDto;
  warnings: ItemWarningDto[];
  effects: ItemEffectDto[];
}

export type ItemWriteMode = 'create' | 'edit' | 'duplicate';

export interface ItemWriteRequest {
  resolvedName: string;
  description?: string | null;
  typeId: number;
  level: number;
  weight: number;
  price: number;
  iconId: number;
  appearanceId: number;
  setId?: number | null;
  conditions?: string | null;
  isVisible?: boolean | null;
  usable: boolean;
  targetable: boolean;
  twoHanded: boolean;
  etheral: boolean;
}

export interface ItemWriteValidationProblem extends AdminWarningLike {}

export interface ItemWriteResultDto {
  itemId: number;
  operation: string;
  resolvedName?: string | null;
  descriptionId: number;
  descriptionPersisted: boolean;
  isVisiblePersisted: boolean;
  detailPath: string;
  previewState: ItemPreviewStateDto;
  warnings: ItemWriteValidationProblem[];
}

export interface AdminApiProblem {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface ItemDetailBundle {
  detail: ItemDetailDto;
  itemSetOptions: AdminOptionDto[];
}

export interface ItemWriteBundle {
  sourceDetail: ItemDetailDto | null;
  typeOptions: AdminOptionDto[];
  itemSetOptions: AdminOptionDto[];
}

export function createEmptyItemSearchRequest(): ItemSearchRequest {
  return {
    page: 1,
    pageSize: 20
  };
}

export function createEmptyItemIconSearchRequest(): ItemIconSearchRequest {
  return {
    page: 1,
    pageSize: 24
  };
}

export function createEmptyPagedResult<TItem>(
  page = 1,
  pageSize = 20
): ItemPagedResultDto<TItem> {
  return {
    page,
    pageSize,
    totalCount: 0,
    items: []
  };
}

export function createUnknownPreviewState(): ItemPreviewStateDto {
  return {
    state: 'UNKNOWN',
    byItemPath: '',
    byIconPath: '',
    manualPath: '',
    previewSource: 'PLACEHOLDER',
    resolvedPath: null,
    fallbackUsed: 'PLACEHOLDER'
  };
}

export function createEmptyItemWriteRequest(): ItemWriteRequest {
  return {
    resolvedName: '',
    description: '',
    typeId: 0,
    level: 1,
    weight: 0,
    price: 0,
    iconId: 0,
    appearanceId: 0,
    setId: null,
    conditions: '',
    isVisible: true,
    usable: false,
    targetable: false,
    twoHanded: false,
    etheral: false
  };
}

export function createItemWriteRequestFromDetail(detail: ItemDetailDto): ItemWriteRequest {
  return {
    resolvedName: detail.resolvedName ?? '',
    description: detail.description ?? '',
    typeId: detail.typeId,
    level: detail.level,
    weight: detail.weight,
    price: detail.price,
    iconId: detail.iconId,
    appearanceId: detail.appearanceId,
    setId: detail.set?.setId && detail.set.setId > 0 ? detail.set.setId : null,
    conditions: detail.criteria && detail.criteria !== 'null' ? detail.criteria : '',
    isVisible: true,
    usable: detail.usable,
    targetable: detail.targetable,
    twoHanded: detail.twoHanded,
    etheral: detail.etheral
  };
}

export function normalizeItemWriteRequest(request: ItemWriteRequest): ItemWriteRequest {
  return {
    resolvedName: normalizeOptionalText(request.resolvedName) ?? '',
    description: normalizeOptionalText(request.description) ?? null,
    typeId: normalizePositiveInt(request.typeId) ?? 0,
    level: normalizePositiveInt(request.level) ?? 0,
    weight: normalizeNonNegativeInt(request.weight) ?? 0,
    price: normalizeNonNegativeNumber(request.price) ?? 0,
    iconId: normalizeNonNegativeInt(request.iconId) ?? 0,
    appearanceId: normalizeNonNegativeInt(request.appearanceId) ?? 0,
    setId: normalizePositiveInt(request.setId) ?? null,
    conditions: normalizeOptionalText(request.conditions) ?? null,
    isVisible: request.isVisible ?? null,
    usable: !!request.usable,
    targetable: !!request.targetable,
    twoHanded: !!request.twoHanded,
    etheral: !!request.etheral
  };
}

export function toAdminApiProblem(error: unknown): AdminApiProblem {
  if (error instanceof HttpErrorResponse) {
    const payload =
      error.error && typeof error.error === 'object'
        ? (error.error as Partial<AdminApiProblem>)
        : undefined;

    return {
      type: payload?.type,
      title: payload?.title || error.statusText || 'Admin API request failed',
      status: payload?.status ?? error.status,
      detail:
        payload?.detail ||
        (typeof error.error === 'string' ? error.error : undefined) ||
        'The request could not be completed.',
      instance: payload?.instance,
      traceId: readTraceId(payload),
      errors: readValidationErrors(payload)
    };
  }

  if (error instanceof Error) {
    return {
      title: 'Unexpected client error',
      detail: error.message
    };
  }

  return {
    title: 'Unexpected client error',
    detail: 'The request failed before a problem payload could be parsed.'
  };
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

function normalizeNonNegativeNumber(value: number | string | null | undefined): number | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : undefined;
}

function readTraceId(payload: Partial<AdminApiProblem> | undefined): string | undefined {
  const traceId = payload?.traceId;
  return typeof traceId === 'string' && traceId.trim().length > 0 ? traceId : undefined;
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
