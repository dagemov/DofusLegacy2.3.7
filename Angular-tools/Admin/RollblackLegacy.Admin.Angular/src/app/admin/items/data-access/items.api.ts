import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  AdminOptionDto,
  ItemClientIdentityDto,
  ItemDetailDto,
  ItemIconOptionDto,
  ItemIconSearchRequest,
  ItemListItemDto,
  ItemPagedResultDto,
  ItemPreviewStateDto,
  ItemSearchRequest,
  ItemWriteRequest,
  ItemWriteResultDto
} from './items.models';
import { toItemQueryParams } from './items.queries';

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
        params: toItemQueryParams(request)
      }
    );
  }

  getItem(itemId: number): Observable<ItemDetailDto> {
    return this.httpClient.get<ItemDetailDto>(`${this.baseUrl}/items/${itemId}`);
  }

  getItemIdentity(itemId: number): Observable<ItemClientIdentityDto> {
    return this.httpClient.get<ItemClientIdentityDto>(
      `${this.baseUrl}/items/${itemId}/identity`
    );
  }

  getTypeOptions(): Observable<AdminOptionDto[]> {
    return this.httpClient.get<AdminOptionDto[]>(`${this.baseUrl}/items/types/options`);
  }

  getItemSetOptions(): Observable<AdminOptionDto[]> {
    return this.httpClient.get<AdminOptionDto[]>(`${this.baseUrl}/item-sets/options`);
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
}
