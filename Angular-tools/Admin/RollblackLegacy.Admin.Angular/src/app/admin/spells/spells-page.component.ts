import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap, tap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { SpellsFacade } from './data-access/spells.facade';
import {
  AdminApiProblem,
  SpellBreedSummaryDto,
  SpellCatalogItemDto,
  SpellPagedResultDto,
  SpellSearchRequest,
  createEmptyPagedResult,
  createEmptySpellSearchRequest,
  toAdminApiProblem
} from './data-access/spells.models';
import {
  DEFAULT_SPELLS_PAGE_SIZE,
  normalizeSpellSearchRequest,
  readSpellSearchRequest,
  toSpellQueryParams
} from './data-access/spells.queries';

@Component({
  selector: 'app-spells-page',
  imports: [CommonModule, FormsModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './spells-page.component.html',
  styleUrl: './spells-page.component.scss'
})
export class SpellsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly spellsFacade = inject(SpellsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected readonly defaultPageSize = DEFAULT_SPELLS_PAGE_SIZE;

  protected filters: SpellSearchRequest = createEmptySpellSearchRequest();
  protected query: SpellSearchRequest = createEmptySpellSearchRequest();
  protected result: SpellPagedResultDto<SpellCatalogItemDto> = createEmptyPagedResult();
  protected listProblem: AdminApiProblem | null = null;
  protected isLoadingList = false;

  ngOnInit(): void {
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
          const request = normalizeSpellSearchRequest(readSpellSearchRequest(paramMap));
          this.ngZone.run(() => {
            this.query = request;
            this.filters = { ...request };
            this.refreshView();
          });

          return this.spellsFacade.getSpells(request).pipe(
            catchError((error: unknown) => {
              this.ngZone.run(() => {
                this.listProblem = toAdminApiProblem(error);
                this.refreshView();
              });
              return of(createEmptyPagedResult<SpellCatalogItemDto>(request.page, request.pageSize));
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
    const nextRequest = normalizeSpellSearchRequest({
      ...this.filters,
      page: 1
    });

    this.navigateWithQuery(nextRequest);
  }

  protected resetFilters(): void {
    const nextRequest = createEmptySpellSearchRequest();
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

  protected trackBySpellId(_index: number, spell: SpellCatalogItemDto): number {
    return spell.spellId;
  }

  protected formatBreeds(breeds: SpellBreedSummaryDto[]): string {
    if (!breeds || breeds.length === 0) {
      return 'No disponible';
    }

    return breeds
      .map((breed) => breed.label?.trim() || `BreedId ${breed.breedId}`)
      .join(', ');
  }

  protected formatDescription(description?: string | null): string {
    const normalized = description?.trim();
    if (!normalized) {
      return 'Sin descripcion';
    }

    return normalized.length > 140 ? `${normalized.slice(0, 137)}...` : normalized;
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

  private navigateWithQuery(request: SpellSearchRequest): void {
    void this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams: toSpellQueryParams(request)
    });
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
