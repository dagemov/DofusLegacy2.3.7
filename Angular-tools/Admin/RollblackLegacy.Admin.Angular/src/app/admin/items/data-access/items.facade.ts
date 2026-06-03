import { Injectable, signal } from '@angular/core';
import { Observable, catchError, forkJoin, of, tap } from 'rxjs';

import {
  AdminOptionDto,
  ItemClientIdentityDto,
  ItemDetailBundle,
  ItemDetailDto,
  ItemIconOptionDto,
  ItemIconSearchRequest,
  ItemListItemDto,
  ItemPagedResultDto,
  ItemPreviewStateDto,
  ItemPublicationStatusDto,
  ItemQaSummaryDto,
  ItemSearchRequest,
  ItemWriteRequest,
  ItemWriteResultDto,
  AdminEffectOptionDto,
  ItemEffectsEditDto,
  ItemEffectsUpdateRequest,
  ItemEffectsUpdateResultDto
} from './items.models';
import { ClientIdentityApi } from './client-identity.api';
import { ClientItemIdentityCheckResultDto } from './client-identity.models';
import { ItemsApi } from './items.api';

@Injectable({
  providedIn: 'root'
})
export class ItemsFacade {
  private readonly typeOptionsState = signal<AdminOptionDto[]>([]);
  private readonly itemSetOptionsState = signal<AdminOptionDto[]>([]);
  private readonly hasLoadedTypeOptionsState = signal(false);
  private readonly hasLoadedItemSetOptionsState = signal(false);

  readonly typeOptions = this.typeOptionsState.asReadonly();
  readonly itemSetOptions = this.itemSetOptionsState.asReadonly();

  constructor(
    private readonly itemsApi: ItemsApi,
    private readonly clientIdentityApi: ClientIdentityApi
  ) {}

  getItems(
    request: ItemSearchRequest
  ): Observable<ItemPagedResultDto<ItemListItemDto>> {
    return this.itemsApi.getItems(request);
  }

  getItemIcons(
    request: ItemIconSearchRequest
  ): Observable<ItemPagedResultDto<ItemIconOptionDto>> {
    return this.itemsApi.getItemIcons(request);
  }

  getItem(itemId: number): Observable<ItemDetailDto> {
    return this.itemsApi.getItem(itemId);
  }

  getItemIdentity(itemId: number): Observable<ItemClientIdentityDto> {
    return this.itemsApi.getItemIdentity(itemId);
  }

  getClientIdentityDiagnostic(itemId: number): Observable<ClientItemIdentityCheckResultDto> {
    return this.clientIdentityApi.getItem(itemId);
  }

  checkClientIdentity(itemIds: readonly number[]): Observable<ClientItemIdentityCheckResultDto[]> {
    return this.clientIdentityApi.checkItems(itemIds);
  }

  getItemQaSummary(itemId: number): Observable<ItemQaSummaryDto> {
    return this.itemsApi.getItemQaSummary(itemId);
  }

  getItemPublicationStatus(itemId: number): Observable<ItemPublicationStatusDto> {
    return this.itemsApi.getItemPublicationStatus(itemId);
  }

  getPreviewState(itemId?: number | null, iconId?: number | null): Observable<ItemPreviewStateDto> {
    return this.itemsApi.getPreviewState(itemId, iconId);
  }

  ensureTypeOptions(): Observable<AdminOptionDto[]> {
    if (this.hasLoadedTypeOptionsState()) {
      return of(this.typeOptionsState());
    }

    return this.itemsApi.getTypeOptions().pipe(
      tap((options) => {
        this.typeOptionsState.set(options);
        this.hasLoadedTypeOptionsState.set(true);
      })
    );
  }

  ensureItemSetOptions(): Observable<AdminOptionDto[]> {
    if (this.hasLoadedItemSetOptionsState()) {
      return of(this.itemSetOptionsState());
    }

    return this.itemsApi.getItemSetOptions().pipe(
      tap((options) => {
        this.itemSetOptionsState.set(options);
        this.hasLoadedItemSetOptionsState.set(true);
      })
    );
  }

  getItemDetailBundle(itemId: number): Observable<ItemDetailBundle> {
    return forkJoin({
      detail: this.getItem(itemId),
      itemSetOptions: this.ensureItemSetOptions().pipe(catchError(() => of([])))
    });
  }

  createItem(request: ItemWriteRequest): Observable<ItemWriteResultDto> {
    return this.itemsApi.createItem(request);
  }

  updateItem(itemId: number, request: ItemWriteRequest): Observable<ItemWriteResultDto> {
    return this.itemsApi.updateItem(itemId, request);
  }

  duplicateItem(itemId: number, request: ItemWriteRequest): Observable<ItemWriteResultDto> {
    return this.itemsApi.duplicateItem(itemId, request);
  }

  getItemEffectsEdit(itemId: number): Observable<ItemEffectsEditDto> {
    return this.itemsApi.getItemEffectsEdit(itemId);
  }

  updateItemEffects(
    itemId: number,
    request: ItemEffectsUpdateRequest
  ): Observable<ItemEffectsUpdateResultDto> {
    return this.itemsApi.updateItemEffects(itemId, request);
  }

  getItemEffectOptions(): Observable<AdminEffectOptionDto[]> {
    return this.itemsApi.getItemEffectOptions();
  }
}
