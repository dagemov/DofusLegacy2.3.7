import { Injectable, signal } from '@angular/core';
import { Observable, catchError, forkJoin, of, tap } from 'rxjs';

import {
  AdminOptionDto,
  ItemClientIdentityDto,
  ItemDetailBundle,
  ItemDetailDto,
  ItemListItemDto,
  ItemPagedResultDto,
  ItemPreviewStateDto,
  ItemSearchRequest,
  ItemWriteRequest,
  ItemWriteResultDto
} from './items.models';
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

  constructor(private readonly itemsApi: ItemsApi) {}

  getItems(
    request: ItemSearchRequest
  ): Observable<ItemPagedResultDto<ItemListItemDto>> {
    return this.itemsApi.getItems(request);
  }

  getItem(itemId: number): Observable<ItemDetailDto> {
    return this.itemsApi.getItem(itemId);
  }

  getItemIdentity(itemId: number): Observable<ItemClientIdentityDto> {
    return this.itemsApi.getItemIdentity(itemId);
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
}
