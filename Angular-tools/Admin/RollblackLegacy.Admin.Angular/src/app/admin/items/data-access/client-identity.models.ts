export interface ClientItemI18nResolutionDto {
  languageCode: string;
  textId: number | null;
  exists: boolean;
  text: string | null;
  sourcePath: string | null;
}

export interface ClientItemAppearanceResolutionDto {
  appearanceId: number | null;
  exists: boolean | null;
  sourcePath: string | null;
}

export interface ClientItemIdentityStatusDto {
  primaryStatus: string;
  clientKnown: boolean;
  needsClientPatch: boolean;
  statuses: string[];
  warnings: string[];
  recommendedAction: string;
}

export interface ClientItemIdentityCheckResultDto {
  itemId: number;
  dbName: string | null;
  dbDescriptionId: number | null;
  clientDescriptionId: number | null;
  clientNameId: number | null;
  clientKnown: boolean;
  status: ClientItemIdentityStatusDto;
  descriptionEs: ClientItemI18nResolutionDto;
  descriptionEn: ClientItemI18nResolutionDto;
  clientNameEs: ClientItemI18nResolutionDto;
  clientNameEn: ClientItemI18nResolutionDto;
  dbTypeId: number | null;
  clientTypeId: number | null;
  clientTypeNameEs: string | null;
  clientTypeNameEn: string | null;
  dbSetId: number | null;
  clientSetId: number | null;
  clientSetNameEs: string | null;
  clientSetNameEn: string | null;
  dbIconId: number | null;
  clientIconId: number | null;
  dbAppearanceId: number | null;
  clientAppearanceId: number | null;
  appearance: ClientItemAppearanceResolutionDto;
  iconPreviewFound: boolean;
  previewPath: string | null;
  itemsD2oPath: string | null;
  itemTypesD2oPath: string | null;
  itemSetsD2oPath: string | null;
  appearancesD2oPath: string | null;
  i18nEsPath: string | null;
  i18nEnPath: string | null;
}

export type ClientIdentityVisualTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

export interface ClientIdentityStatusPresentation {
  label: string;
  tone: ClientIdentityVisualTone;
  badgeClass: string;
}
