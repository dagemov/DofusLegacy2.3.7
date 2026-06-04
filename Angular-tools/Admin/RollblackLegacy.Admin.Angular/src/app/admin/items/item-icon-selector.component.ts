import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  NgZone,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemsFacade } from './data-access/items.facade';
import {
  AdminApiProblem,
  ItemIconOptionDto,
  ItemIconSearchRequest,
  ItemIconSelection,
  ItemPagedResultDto,
  createEmptyItemIconSearchRequest,
  createEmptyPagedResult,
  toAdminApiProblem
} from './data-access/items.models';

const DEFAULT_ICON_PAGE_SIZE = 24;

@Component({
  selector: 'app-item-icon-selector',
  imports: [CommonModule, FormsModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './item-icon-selector.component.html',
  styleUrl: './item-icon-selector.component.scss'
})
export class ItemIconSelectorComponent implements OnInit, OnChanges {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly brokenPreviewIds = new Set<number>();

  @Input() embedded = false;
  @Input() initialIconId: number | null = null;
  @Input() showSelectionPayload = true;
  @Output() readonly iconSelected = new EventEmitter<ItemIconSelection>();

  protected filters: ItemIconSearchRequest = createEmptyItemIconSearchRequest();
  protected query: ItemIconSearchRequest = createEmptyItemIconSearchRequest();
  protected result: ItemPagedResultDto<ItemIconOptionDto> =
    createEmptyPagedResult<ItemIconOptionDto>(1, DEFAULT_ICON_PAGE_SIZE);
  protected selectedIcon: ItemIconSelection | null = null;
  protected problem: AdminApiProblem | null = null;
  protected isLoading = false;

  ngOnInit(): void {
    if (this.embedded) {
      const initialRequest = normalizeItemIconSearchRequest({
        ...createEmptyItemIconSearchRequest(),
        iconId: this.initialIconId ?? undefined
      });

      this.filters = { ...initialRequest };
      this.query = initialRequest;
      this.loadIcons(initialRequest);
      return;
    }

    this.activatedRoute.queryParamMap
      .pipe(
        switchMap((paramMap) => {
          const request = normalizeItemIconSearchRequest({
            search: normalizeOptionalText(paramMap.get('search') ?? undefined),
            iconId: normalizePositiveInt(paramMap.get('iconId') ?? undefined),
            page: normalizePositiveInt(paramMap.get('page') ?? undefined) ?? 1,
            pageSize: normalizePageSize(paramMap.get('pageSize') ?? undefined) ?? DEFAULT_ICON_PAGE_SIZE
          });

          this.ngZone.run(() => {
            this.query = request;
            this.filters = { ...request };
            this.refreshView();
          });

          return this.queryIcons(request);
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

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.embedded || !changes['initialIconId'] || changes['initialIconId'].firstChange) {
      return;
    }

    const currentIconId = normalizePositiveInt(changes['initialIconId'].currentValue);
    if (this.filters.iconId === currentIconId) {
      return;
    }

    const nextRequest = normalizeItemIconSearchRequest({
      ...this.query,
      iconId: currentIconId,
      page: 1
    });

    this.filters = { ...nextRequest };
    this.query = nextRequest;
    this.loadIcons(nextRequest);
  }

  protected applyFilters(): void {
    const nextRequest = normalizeItemIconSearchRequest({
      ...this.filters,
      page: 1
    });

    this.navigateWithQuery(nextRequest);
  }

  protected resetFilters(): void {
    const nextRequest = normalizeItemIconSearchRequest({
      ...createEmptyItemIconSearchRequest(),
      iconId: this.embedded ? this.initialIconId ?? undefined : undefined
    });
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

  protected selectIcon(option: ItemIconOptionDto): void {
    const selection = {
      iconId: option.iconId,
      previewPath: option.previewPath ?? null
    };

    this.selectedIcon = selection;
    this.iconSelected.emit(selection);
  }

  protected resolvePreviewPath(option: ItemIconOptionDto): string | null {
    if (!option.hasPreview || !option.previewPath || this.brokenPreviewIds.has(option.iconId)) {
      return null;
    }

    return option.previewPath;
  }

  protected markPreviewAsMissing(iconId: number): void {
    this.brokenPreviewIds.add(iconId);
  }

  protected previewAvailabilityLabel(option: ItemIconOptionDto): string {
    if (this.brokenPreviewIds.has(option.iconId)) {
      return 'Preview roto en host';
    }

    return option.hasPreview ? 'Preview disponible' : 'Preview faltante';
  }

  protected previewAvailabilityClass(option: ItemIconOptionDto): string {
    if (this.brokenPreviewIds.has(option.iconId)) {
      return 'text-warning';
    }

    return option.hasPreview ? 'text-success' : 'text-secondary';
  }

  protected previewSourceLabel(option: ItemIconOptionDto): string {
    switch (option.source) {
      case 'CURATED_BY_ICON':
        return 'CURATED_BY_ICON';
      case 'BY_ICON_PREVIEW':
        return 'CURATED_BY_ICON';
      case 'MISSING':
        return 'MISSING';
      default:
        return option.source || 'MISSING';
    }
  }

  protected previewStateLabel(option: ItemIconOptionDto): string {
    if (this.brokenPreviewIds.has(option.iconId)) {
      return 'BROKEN_HOST';
    }

    return option.hasPreview ? option.previewState || 'FOUND' : 'MISSING';
  }

  protected previewStateBadgeClass(option: ItemIconOptionDto): string {
    if (this.brokenPreviewIds.has(option.iconId) || !option.hasPreview) {
      return 'text-bg-warning';
    }

    return 'text-bg-success';
  }

  protected trackByIconId(_index: number, option: ItemIconOptionDto): number {
    return option.iconId;
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

  private navigateWithQuery(request: ItemIconSearchRequest): void {
    const normalized = normalizeItemIconSearchRequest(request);

    if (this.embedded) {
      this.query = normalized;
      this.filters = { ...normalized };
      this.loadIcons(normalized);
      return;
    }

    const queryParams: Record<string, string | number> = {
      page: normalized.page,
      pageSize: normalized.pageSize
    };

    if (normalized.search) {
      queryParams['search'] = normalized.search;
    }

    if (normalized.iconId) {
      queryParams['iconId'] = normalized.iconId;
    }

    void this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams
    });
  }

  private loadIcons(request: ItemIconSearchRequest): void {
    this.query = request;
    this.problem = null;
    this.isLoading = true;
    this.refreshView();

    this.queryIcons(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.ngZone.run(() => {
          this.result = result;
          this.refreshView();
        });
      });
  }

  private queryIcons(request: ItemIconSearchRequest) {
    return this.itemsFacade.getItemIcons(request).pipe(
      catchError((error: unknown) => {
        this.ngZone.run(() => {
          this.problem = toAdminApiProblem(error);
          this.refreshView();
        });
        return of(createEmptyPagedResult<ItemIconOptionDto>(request.page, request.pageSize));
      }),
      finalize(() => {
        this.ngZone.run(() => {
          this.isLoading = false;
          this.refreshView();
        });
      })
    );
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}

function normalizeItemIconSearchRequest(request: ItemIconSearchRequest): ItemIconSearchRequest {
  return {
    search: normalizeOptionalText(request.search),
    iconId: normalizePositiveInt(request.iconId),
    page: normalizePositiveInt(request.page) ?? 1,
    pageSize: normalizePageSize(request.pageSize) ?? DEFAULT_ICON_PAGE_SIZE
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

function normalizePageSize(value: number | string | null | undefined): number | undefined {
  const parsed = normalizePositiveInt(value);
  if (!parsed) {
    return undefined;
  }

  return Math.min(parsed, 100);
}
