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

export type ItemIconCatalogMode = 'by-icon' | 'by-category';

export const ITEM_ICON_CATEGORY_OPTIONS = [
  { id: 'dofus', label: 'Dofus' },
  { id: 'sombreros', label: 'Sombreros' },
  { id: 'capas', label: 'Capas' },
  { id: 'botas', label: 'Botas' },
  { id: 'mascotas', label: 'Mascotas' },
  { id: 'escudos', label: 'Escudos' },
  { id: 'anillos', label: 'Anillos' },
  { id: 'amuletos', label: 'Amuletos' },
  { id: 'cinturones', label: 'Cinturones' },
  { id: 'recursos', label: 'Recursos' },
  { id: 'trofeos', label: 'Trofeos' },
  { id: 'consumibles', label: 'Consumibles' }
] as const;

export interface ItemIconCategoryStatDto {
  category: string;
  label: string;
  count: number;
  lastExtractionUtc?: string | null;
  previewSource: string;
}

export interface ItemIconCategoryStatsDto {
  totalPngInAngular: number;
  totalCataloged: number;
  weaponsExcluded: number;
  previewSource: string;
  categories: ItemIconCategoryStatDto[];
}

export interface ItemIconSearchRequest {
  search?: string;
  nameEs?: string;
  nameEn?: string;
  itemId?: number;
  iconId?: number;
  catalogMode?: ItemIconCatalogMode;
  category?: string;
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
  previewState: string;
  source: string;
  hasPreview: boolean;
  linkedItemCount?: number | null;
  sampleItemNames: string[];
  category?: string | null;
  nameEs?: string | null;
  nameEn?: string | null;
  sampleItemId?: number | null;
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
  byCategoryPath?: string;
  previewSource: string;
  resolvedPath?: string | null;
  fallbackUsed: string;
}

export interface ItemSetSearchRequest {
  search?: string;
  minLevel?: number;
  maxLevel?: number;
  minParts?: number;
  maxParts?: number;
  page: number;
  pageSize: number;
}

export interface ItemSetListItemDto {
  setId: number;
  name: string;
  level: number;
  itemCount: number;
  bonusTierCount: number;
  previewItemIcons: string[];
}

export interface ItemSetBonusEffectDto {
  effectId: number;
  label: string;
  protocolName: string;
  value: number;
  diceNum?: number | null;
  diceSide?: number | null;
  format: string;
}

export interface ItemSetBonusEffectWriteDto {
  effectId: number;
  value: number;
  diceNum?: number | null;
  diceSide?: number | null;
  format: string;
}

export interface ItemSetBonusTierWriteDto {
  pieceCount: number;
  effects: ItemSetBonusEffectWriteDto[];
}

export interface ItemSetWriteRequest {
  name: string;
  level: number;
  itemIds: number[];
  bonusTiers: ItemSetBonusTierWriteDto[];
}

export interface ItemSetWriteResultDto {
  setId: number;
  message: string;
}

export interface ItemSetBonusTierDto {
  pieceCount: number;
  tierLabel: string;
  effects: ItemSetBonusEffectDto[];
}

export interface ItemSetMemberDto {
  itemId: number;
  name: string;
  typeId: number;
  typeName: string;
  iconId: number;
  previewState: ItemPreviewStateDto;
  previewPath?: string | null;
  publicationSummary?: string | null;
}

export interface ItemSetDetailDto {
  setId: number;
  name: string;
  level: number;
  bonusIsSecret: boolean;
  items: ItemSetMemberDto[];
  bonusTiers: ItemSetBonusTierDto[];
}

export function createEmptyItemSetSearchRequest(): ItemSetSearchRequest {
  return {
    page: 1,
    pageSize: 20
  };
}

export interface ItemAppearancePreviewStateDto {
  appearanceId: number;
  appearanceKnown: boolean | null;
  state: string;
  byAppearancePath: string;
  previewSource: string;
  resolvedPath?: string | null;
  appearancesD2oPath?: string | null;
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
  appearancePreviewState: ItemAppearancePreviewStateDto;
  warnings: ItemWarningDto[];
  effects: ItemEffectDto[];
}

export interface ItemQaSummaryDto {
  itemId: number;
  resolvedName?: string | null;
  type?: string | null;
  level: number;
  iconId: number;
  appearanceId: number;
  previewState: ItemPreviewStateDto;
  appearancePreviewState: ItemAppearancePreviewStateDto;
  warnings: ItemWarningDto[];
  workflowState: string;
  canQa: boolean;
  canPublish: boolean;
  blockingReasons: string[];
  recommendedChecks: string[];
}

export interface ItemPublicationManifestDto {
  dbItemId: number;
  targetClientItemId: number;
  nameEs?: string | null;
  nameEn?: string | null;
  descriptionId: number;
  typeId: number;
  typeName?: string | null;
  iconId: number;
  appearanceId: number;
  effectsSummary: string;
  criteria?: string | null;
  sourceTemplateItemId?: number | null;
  clientKnown: boolean;
  primaryState: string;
  states: string[];
  requiredClientActions: string[];
  filesToPatch: string[];
  risks: string[];
  canPublishAutomatically: boolean;
  blockingReasons: string[];
  clientRootPath?: string | null;
  stagingOutputPath?: string | null;
  stagingPackageStatus: string;
  stagingPackagePath?: string | null;
  stagingPackageId?: string | null;
  stagingValidationStatus?: string | null;
  stagingWarnings: string[];
  nextManualSteps: string[];
  generatedAtUtc: string;
}

export interface ItemPublicationStatusDto {
  itemId: number;
  resolvedName?: string | null;
  iconId: number;
  appearanceId: number;
  previewState: ItemPreviewStateDto;
  appearancePreviewState: ItemAppearancePreviewStateDto;
  visibilityState: string;
  clientTemplateState: string;
  publicationState: string;
  clientKnown: boolean;
  published: boolean;
  needsClientPatch: boolean;
  needsAsset: boolean;
  needsQa: boolean;
  clientRootPath?: string | null;
  itemsD2oPath?: string | null;
  reasons: string[];
  recommendedActions: string[];
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

export type AdminFeedbackKind = 'success' | 'error';

export interface AdminFeedback {
  kind: AdminFeedbackKind;
  title: string;
  detail?: string | null;
  status?: number | null;
  traceId?: string | null;
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

export interface AdminEffectOptionDto {
  effectId: number;
  label: string;
  protocolName: string;
  group: string;
  defaultSerializationTypeId: number;
  format: string;
  operatorMode: string;
  sortPriority: number;
  isCharacteristic: boolean;
  isSupported: boolean;
}

export interface ItemEffectEditDto {
  rowId: string;
  serializationTypeId: number;
  effectId: number;
  label: string;
  diceNum: number;
  diceSide: number;
  value: number;
  minValue: number;
  maxValue: number;
  operatorMode: string;
  group: string;
  isCharacteristic: boolean;
  isSupported: boolean;
  warning?: string | null;
  preservedEffectHex?: string | null;
  previewText: string;
}

export interface ItemEffectsEditDto {
  itemId: number;
  effectsHex: string;
  effects: ItemEffectEditDto[];
  preservedSuffixHex?: string | null;
  warnings: string[];
  hasUnsupportedEffects: boolean;
}

export interface ItemEffectEditRowRequest {
  rowId?: string | null;
  serializationTypeId: number;
  effectId: number;
  diceNum: number;
  diceSide: number;
  value: number;
  minValue: number;
  maxValue: number;
  preservedEffectHex?: string | null;
}

export interface ItemEffectsUpdateRequest {
  effects: ItemEffectEditRowRequest[];
  preservedSuffixHex?: string | null;
  removedUnsupportedRowIds?: string[];
}

export interface ItemEffectsUpdateResultDto {
  itemId: number;
  effectsHex: string;
  effects: ItemEffectEditDto[];
  warnings: string[];
}

export function createEmptyItemSearchRequest(): ItemSearchRequest {
  return {
    page: 1,
    pageSize: 20
  };
}

export function createEmptyItemIconSearchRequest(): ItemIconSearchRequest {
  return {
    catalogMode: 'by-category',
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
    byCategoryPath: '',
    previewSource: 'PLACEHOLDER',
    resolvedPath: null,
    fallbackUsed: 'PLACEHOLDER'
  };
}

export function createUnknownAppearancePreviewState(appearanceId = 0): ItemAppearancePreviewStateDto {
  return {
    appearanceId,
    appearanceKnown: null,
    state: appearanceId > 0 ? 'UNKNOWN' : 'NOT_APPLICABLE',
    byAppearancePath: appearanceId > 0 ? `/assets/item-previews/by-appearance/${appearanceId}.png` : '',
    previewSource: 'PLACEHOLDER',
    resolvedPath: null,
    appearancesD2oPath: null
  };
}

export function createAdminSuccessFeedback(
  title: string,
  detail?: string | null
): AdminFeedback {
  return {
    kind: 'success',
    title,
    detail: normalizeProblemDetail(title, normalizeProblemText(detail ?? undefined)) ?? null,
    status: null,
    traceId: null,
    errors: undefined
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
    const isNetworkFailure = error.status === 0;
    const responseText =
      typeof error.error === 'string'
        ? error.error
        : typeof error.error === 'object' && error.error !== null && 'text' in error.error
          ? String((error.error as { text?: unknown }).text ?? '')
          : '';
    const isHttp200HtmlResponse =
      error.status === 200 && looksLikeHtmlDocument(responseText);
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
        ? 'Se recibió HTML (index.html del dev server) en lugar de JSON. Usa npm start o ng serve con proxy.conf.json y confirma que RollblackLegacy.Admin.Api escucha en http://localhost:5248.'
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
