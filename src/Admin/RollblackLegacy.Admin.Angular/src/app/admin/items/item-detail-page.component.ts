import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemClientIdentityCardComponent } from './components/item-client-identity-card.component';
import { ItemDiagnosticPanelComponent } from './components/item-diagnostic-panel.component';
import { ItemPreviewCardComponent } from './components/item-preview-card.component';
import { ItemRuntimeSummaryCardComponent } from './components/item-runtime-summary-card.component';
import { ItemsFacade } from './data-access/items.facade';
import { AdminApiProblem, AdminOptionDto, ItemClientIdentityDto, ItemDetailDto, toAdminApiProblem } from './data-access/items.models';

@Component({
  selector: 'app-item-detail-page',
  imports: [
    CommonModule,
    RouterLink,
    ApiProblemPanelComponent,
    ItemRuntimeSummaryCardComponent,
    ItemClientIdentityCardComponent,
    ItemPreviewCardComponent,
    ItemDiagnosticPanelComponent
  ],
  templateUrl: './item-detail-page.component.html',
  styleUrl: './item-detail-page.component.scss'
})
export class ItemDetailPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected detail: ItemDetailDto | null = null;
  protected identity: ItemClientIdentityDto | null = null;
  protected itemSetOptions: AdminOptionDto[] = [];
  protected problem: AdminApiProblem | null = null;
  protected isLoading = false;
  protected itemId: number | null = null;

  ngOnInit(): void {
    this.activatedRoute.paramMap
      .pipe(
        switchMap((paramMap) => {
          const itemId = Number(paramMap.get('itemId'));
          this.ngZone.run(() => {
            this.itemId = Number.isInteger(itemId) && itemId > 0 ? itemId : null;
            this.problem = null;
            this.detail = null;
            this.identity = null;
            this.isLoading = true;
            this.refreshView();
          });

          if (!this.itemId) {
            this.ngZone.run(() => {
              this.problem = {
                title: 'Invalid item id',
                detail: 'The route parameter must be a positive integer.',
                status: 400
              };
              this.isLoading = false;
              this.refreshView();
            });
            return of(null);
          }

          return this.itemsFacade.getItemDetailBundle(this.itemId).pipe(
            catchError((error: unknown) => {
              this.ngZone.run(() => {
                this.problem = toAdminApiProblem(error);
                this.refreshView();
              });
              return of(null);
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
      .subscribe((bundle) => {
        if (!bundle) {
          return;
        }

        this.ngZone.run(() => {
          this.detail = bundle.detail;
          this.identity = bundle.detail.clientIdentity ?? null;
          this.itemSetOptions = bundle.itemSetOptions;
          this.refreshView();
        });
      });
  }

  protected resolveSetName(detail: ItemDetailDto | null): string {
    if (!detail?.set) {
      return 'No set';
    }

    if (detail.set.setName) {
      return detail.set.setName;
    }

    const option = this.itemSetOptions.find((entry) => entry.value === detail.set?.setId);
    return option?.label || `Set #${detail.set.setId}`;
  }

  protected get resolvedIdentity(): ItemClientIdentityDto | null {
    return this.identity ?? this.detail?.clientIdentity ?? null;
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
