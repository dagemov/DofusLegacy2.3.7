import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap, tap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemsFacade } from '../items/data-access/items.facade';
import {
  AdminApiProblem,
  ItemPagedResultDto,
  ItemSetListItemDto,
  ItemSetSearchRequest,
  createEmptyItemSetSearchRequest,
  createEmptyPagedResult,
  toAdminApiProblem
} from '../items/data-access/items.models';
import {
  DEFAULT_ITEM_SETS_PAGE_SIZE,
  normalizeItemSetSearchRequest,
  readItemSetSearchRequest,
  toItemSetQueryParams
} from './data-access/item-sets.queries';

@Component({
  selector: 'app-item-sets-page',
  imports: [CommonModule, FormsModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './item-sets-page.component.html',
  styleUrl: './item-sets-page.component.scss'
})
export class ItemSetsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected readonly defaultPageSize = DEFAULT_ITEM_SETS_PAGE_SIZE;
  protected filters: ItemSetSearchRequest = createEmptyItemSetSearchRequest();
  protected query: ItemSetSearchRequest = createEmptyItemSetSearchRequest();
  protected result: ItemPagedResultDto<ItemSetListItemDto> = createEmptyPagedResult();
  protected problem: AdminApiProblem | null = null;
  protected isLoading = false;
  protected readonly brokenPreviewIcons = new Set<string>();

  ngOnInit(): void {
    this.activatedRoute.queryParamMap
      .pipe(
        tap(() => {
          this.ngZone.run(() => {
            this.isLoading = true;
            this.problem = null;
            this.refreshView();
          });
        }),
        switchMap((paramMap) => {
          const request = normalizeItemSetSearchRequest(readItemSetSearchRequest(paramMap));
          this.ngZone.run(() => {
            this.query = request;
            this.filters = { ...request };
            this.refreshView();
          });

          return this.itemsFacade.searchItemSets(request).pipe(
            catchError((error: unknown) => {
              this.ngZone.run(() => {
                this.problem = toAdminApiProblem(error);
                this.refreshView();
              });
              return of(createEmptyPagedResult<ItemSetListItemDto>(request.page, request.pageSize));
            }),
            finalize(() => {
              this.ngZone.run(() => {
                this.isLoading = false;
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
    this.navigateWithQuery(normalizeItemSetSearchRequest({ ...this.filters, page: 1 }));
  }

  protected clearFilters(): void {
    this.navigateWithQuery(createEmptyItemSetSearchRequest());
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }

    this.navigateWithQuery({ ...this.query, page });
  }

  protected get totalPages(): number {
    return Math.max(1, Math.ceil(this.result.totalCount / this.result.pageSize));
  }

  protected previewIconKey(setId: number, index: number): string {
    return `${setId}-${index}`;
  }

  protected onPreviewIconError(setId: number, index: number): void {
    this.brokenPreviewIcons.add(this.previewIconKey(setId, index));
    this.refreshView();
  }

  protected showPreviewIcon(set: ItemSetListItemDto, index: number): boolean {
    return (
      !!set.previewItemIcons[index] && !this.brokenPreviewIcons.has(this.previewIconKey(set.setId, index))
    );
  }

  private navigateWithQuery(request: ItemSetSearchRequest): void {
    void this.router.navigate(['/admin/item-sets'], { queryParams: toItemSetQueryParams(request) });
  }

  private refreshView(): void {
    this.changeDetectorRef.markForCheck();
  }
}
