import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap, tap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemsFacade } from './data-access/items.facade';
import {
  AdminApiProblem,
  AdminOptionDto,
  ItemListItemDto,
  ItemPagedResultDto,
  ItemSearchRequest,
  createEmptyItemSearchRequest,
  createEmptyPagedResult,
  toAdminApiProblem
} from './data-access/items.models';
import {
  DEFAULT_ITEMS_PAGE_SIZE,
  normalizeItemSearchRequest,
  readItemSearchRequest,
  toItemQueryParams
} from './data-access/items.queries';

@Component({
  selector: 'app-items-page',
  imports: [CommonModule, FormsModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './items-page.component.html',
  styleUrl: './items-page.component.scss'
})
export class ItemsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected readonly defaultPageSize = DEFAULT_ITEMS_PAGE_SIZE;

  protected filters: ItemSearchRequest = createEmptyItemSearchRequest();
  protected query: ItemSearchRequest = createEmptyItemSearchRequest();
  protected result: ItemPagedResultDto<ItemListItemDto> = createEmptyPagedResult();
  protected typeOptions: AdminOptionDto[] = [];
  protected listProblem: AdminApiProblem | null = null;
  protected lookupProblem: AdminApiProblem | null = null;
  protected isLoadingList = false;
  protected isLoadingTypeOptions = false;

  ngOnInit(): void {
    this.loadTypeOptions();

    this.activatedRoute.queryParamMap
      .pipe(
        tap(() => {
          this.ngZone.run(() => {
            this.isLoadingList = true;
            this.listProblem = null;
            this.refreshView();
          });
        }),
        switchMap((paramMap) => {
          const request = normalizeItemSearchRequest(readItemSearchRequest(paramMap));
          this.ngZone.run(() => {
            this.query = request;
            this.filters = { ...request };
            this.refreshView();
          });

          return this.itemsFacade.getItems(request).pipe(
            catchError((error: unknown) => {
              this.ngZone.run(() => {
                this.listProblem = toAdminApiProblem(error);
                this.refreshView();
              });
              return of(createEmptyPagedResult<ItemListItemDto>(request.page, request.pageSize));
            }),
            finalize(() => {
              this.ngZone.run(() => {
                this.isLoadingList = false;
                this.refreshView();
              });
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        this.ngZone.run(() => {
          this.result = result;
          this.refreshView();
        });
      });
  }

  protected applyFilters(): void {
    const nextRequest = normalizeItemSearchRequest({
      ...this.filters,
      page: 1
    });

    this.navigateWithQuery(nextRequest);
  }

  protected resetFilters(): void {
    const nextRequest = createEmptyItemSearchRequest();
    this.filters = nextRequest;
    this.navigateWithQuery(nextRequest);
  }

  protected goToPage(page: number): void {
    if (page < 1 || page === this.query.page) {
      return;
    }

    this.navigateWithQuery({
      ...this.query,
      page
    });
  }

  protected changePageSize(pageSize: number): void {
    this.navigateWithQuery({
      ...this.query,
      page: 1,
      pageSize
    });
  }

  protected trackByItemId(_index: number, item: ItemListItemDto): number {
    return item.itemId;
  }

  protected formatPreviewState(state: string | undefined): string {
    return (state || 'UNKNOWN').toUpperCase();
  }

  protected previewBadgeClass(state: string | undefined): string {
    switch ((state || 'UNKNOWN').toUpperCase()) {
      case 'FOUND':
        return 'state-badge--found';
      case 'MANUAL':
        return 'state-badge--manual';
      case 'MISSING':
        return 'state-badge--missing';
      default:
        return 'state-badge--unknown';
    }
  }

  protected get totalPages(): number {
    return Math.max(1, Math.ceil(this.result.totalCount / this.result.pageSize || 1));
  }

  protected get hasPreviousPage(): boolean {
    return this.result.page > 1;
  }

  protected get hasNextPage(): boolean {
    return this.result.page < this.totalPages;
  }

  protected get pageProblem(): AdminApiProblem | null {
    return this.listProblem ?? this.lookupProblem;
  }

  private loadTypeOptions(): void {
    this.isLoadingTypeOptions = true;
    this.lookupProblem = null;

    this.itemsFacade
      .ensureTypeOptions()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.lookupProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of([] as AdminOptionDto[]);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isLoadingTypeOptions = false;
            this.refreshView();
          });
        })
      )
      .subscribe((options) => {
        this.ngZone.run(() => {
          this.typeOptions = options;
          this.refreshView();
        });
      });
  }

  private navigateWithQuery(request: ItemSearchRequest): void {
    void this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams: toItemQueryParams(request)
    });
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
