import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  AdminOptionDto,
  ItemClientIdentityDto,
  ItemDetailDto,
  ItemIconCategoryStatsDto,
  ItemIconOptionDto,
  ItemIconSearchRequest,
  ItemListItemDto,
  ItemPagedResultDto,
  ItemPreviewStateDto,
  ItemAppearancePreviewStateDto,
  ItemPublicationManifestDto,
  ItemPublicationStatusDto,
  ItemQaSummaryDto,
  ItemSearchRequest,
  ItemWriteRequest,
  ItemWriteResultDto,
  AdminEffectOptionDto,
  ItemEffectsEditDto,
  ItemEffectsUpdateRequest,
  ItemEffectsUpdateResultDto,
  ItemSetDetailDto,
  ItemSetListItemDto
} from './items.models';
import { toItemIconQueryParams, toItemQueryParams } from './items.queries';

@Injectable({
  providedIn: 'root'
})
export class ItemsApi {
  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.adminApiBaseUrl;

  getItems(
    request: ItemSearchRequest
  ): Observable<ItemPagedResultDto<ItemListItemDto>> {
    return this.httpClient.get<ItemPagedResultDto<ItemListItemDto>>(
      `${this.baseUrl}/items`,
      {
        params: toItemQueryParams(request)
      }
    );
  }

  getItemIcons(
    request: ItemIconSearchRequest
  ): Observable<ItemPagedResultDto<ItemIconOptionDto>> {
    return this.httpClient.get<ItemPagedResultDto<ItemIconOptionDto>>(
      `${this.baseUrl}/item-icons`,
      {
        params: toItemIconQueryParams(request)
      }
    );
  }

  getItemIconCategoryStats(): Observable<ItemIconCategoryStatsDto> {
    return this.httpClient.get<ItemIconCategoryStatsDto>(`${this.baseUrl}/item-icons/category-stats`);
  }

  getItem(itemId: number): Observable<ItemDetailDto> {
    return this.httpClient.get<ItemDetailDto>(`${this.baseUrl}/items/${itemId}`);
  }

  getItemIdentity(itemId: number): Observable<ItemClientIdentityDto> {
    return this.httpClient.get<ItemClientIdentityDto>(
      `${this.baseUrl}/items/${itemId}/identity`
    );
  }

  getItemQaSummary(itemId: number): Observable<ItemQaSummaryDto> {
    return this.httpClient.get<ItemQaSummaryDto>(`${this.baseUrl}/items/${itemId}/qa-summary`);
  }

  getItemPublicationStatus(itemId: number): Observable<ItemPublicationStatusDto> {
    return this.httpClient.get<ItemPublicationStatusDto>(`${this.baseUrl}/items/${itemId}/publication-status`);
  }

  getItemPublicationManifest(itemId: number): Observable<ItemPublicationManifestDto> {
    return this.httpClient.get<ItemPublicationManifestDto>(`${this.baseUrl}/items/${itemId}/publication-manifest`);
  }

  getTypeOptions(): Observable<AdminOptionDto[]> {
    return this.httpClient.get<AdminOptionDto[]>(`${this.baseUrl}/items/types/options`);
  }

  getItemSetOptions(): Observable<AdminOptionDto[]> {
    return this.httpClient.get<AdminOptionDto[]>(`${this.baseUrl}/item-sets/options`);
  }

  getItemSets(): Observable<ItemSetListItemDto[]> {
    return this.httpClient.get<ItemSetListItemDto[]>(`${this.baseUrl}/item-sets`);
  }

  getItemSet(setId: number): Observable<ItemSetDetailDto> {
    return this.httpClient.get<ItemSetDetailDto>(`${this.baseUrl}/item-sets/${setId}`);
  }

  getPreviewState(itemId?: number | null, iconId?: number | null): Observable<ItemPreviewStateDto> {
    const params: Record<string, number> = {};

    if (itemId && itemId > 0) {
      params['itemId'] = itemId;
    }

    if (iconId !== null && iconId !== undefined && iconId >= 0) {
      params['iconId'] = iconId;
    }

    return this.httpClient.get<ItemPreviewStateDto>(`${this.baseUrl}/items/preview-state`, {
      params
    });
  }

  getAppearancePreviewState(
    appearanceId: number,
    appearanceKnown?: boolean | null
  ): Observable<ItemAppearancePreviewStateDto> {
    const params: Record<string, string | number> = {
      appearanceId
    };

    if (appearanceKnown !== null && appearanceKnown !== undefined) {
      params['appearanceKnown'] = appearanceKnown ? 'true' : 'false';
    }

    return this.httpClient.get<ItemAppearancePreviewStateDto>(
      `${this.baseUrl}/items/appearance-preview-state`,
      { params }
    );
  }

  createItem(request: ItemWriteRequest): Observable<ItemWriteResultDto> {
    return this.httpClient.post<ItemWriteResultDto>(`${this.baseUrl}/items`, request);
  }

  updateItem(itemId: number, request: ItemWriteRequest): Observable<ItemWriteResultDto> {
    return this.httpClient.put<ItemWriteResultDto>(`${this.baseUrl}/items/${itemId}`, request);
  }

  duplicateItem(itemId: number, request: ItemWriteRequest): Observable<ItemWriteResultDto> {
    return this.httpClient.post<ItemWriteResultDto>(
      `${this.baseUrl}/items/${itemId}/duplicate`,
      request
    );
  }

  getItemEffectsEdit(itemId: number): Observable<ItemEffectsEditDto> {
    return this.httpClient.get<ItemEffectsEditDto>(`${this.baseUrl}/items/${itemId}/effects/edit`);
  }

  updateItemEffects(
    itemId: number,
    request: ItemEffectsUpdateRequest
  ): Observable<ItemEffectsUpdateResultDto> {
    return this.httpClient.put<ItemEffectsUpdateResultDto>(
      `${this.baseUrl}/items/${itemId}/effects`,
      request
    );
  }

  getItemEffectOptions(): Observable<AdminEffectOptionDto[]> {
    return this.httpClient.get<AdminEffectOptionDto[]>(`${this.baseUrl}/item-effects/options`);
  }
}
