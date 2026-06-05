import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, combineLatest, finalize, of } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ClientIdentityBatchCheckPanelComponent } from './components/client-identity-batch-check-panel.component';
import { ClientIdentityDiagnosticCardComponent } from './components/client-identity-diagnostic-card.component';
import { ItemClientIdentityCardComponent } from './components/item-client-identity-card.component';
import { ItemDiagnosticPanelComponent } from './components/item-diagnostic-panel.component';
import { ItemAppearancePreviewCardComponent } from './components/item-appearance-preview-card.component';
import { ItemPreviewCardComponent } from './components/item-preview-card.component';
import { ItemQaReadinessPanelComponent } from './components/item-qa-readiness-panel.component';
import { ItemRuntimeSummaryCardComponent } from './components/item-runtime-summary-card.component';
import { ItemsFacade } from './data-access/items.facade';
import { ClientItemIdentityCheckResultDto } from './data-access/client-identity.models';
import {
  AdminApiProblem,
  AdminFeedback,
  AdminOptionDto,
  ItemClientIdentityDto,
  ItemDetailDto,
  ItemQaSummaryDto,
  createAdminSuccessFeedback,
  toAdminApiProblem
} from './data-access/items.models';

@Component({
  selector: 'app-item-detail-page',
  imports: [
    CommonModule,
    RouterLink,
    ApiProblemPanelComponent,
    ItemRuntimeSummaryCardComponent,
    ClientIdentityDiagnosticCardComponent,
    ClientIdentityBatchCheckPanelComponent,
    ItemClientIdentityCardComponent,
    ItemPreviewCardComponent,
    ItemAppearancePreviewCardComponent,
    ItemDiagnosticPanelComponent,
    ItemQaReadinessPanelComponent
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
  protected clientIdentityDiagnostic: ClientItemIdentityCheckResultDto | null = null;
  protected clientIdentityProblem: AdminApiProblem | null = null;
  protected isLoadingClientIdentity = false;
  protected itemSetOptions: AdminOptionDto[] = [];
  protected problem: AdminApiProblem | null = null;
  protected pageFeedback: AdminFeedback | null = null;
  protected qaSummary: ItemQaSummaryDto | null = null;
  protected qaSummaryProblem: AdminApiProblem | null = null;
  protected isLoading = false;
  protected isLoadingQaSummary = false;
  protected itemId: number | null = null;

  ngOnInit(): void {
    combineLatest([this.activatedRoute.paramMap, this.activatedRoute.queryParamMap])
      .pipe(
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(([paramMap, queryParamMap]) => {
          const itemId = Number(paramMap.get('itemId'));
          const writeOperation = (queryParamMap.get('writeOperation') || '').trim();
          const wasSaved = queryParamMap.get('saved') === '1';
          this.ngZone.run(() => {
            this.itemId = Number.isInteger(itemId) && itemId > 0 ? itemId : null;
            this.problem = null;
            this.pageFeedback =
              wasSaved && this.itemId
                ? createAdminSuccessFeedback(
                    'SUCCESS',
                    this.buildSaveDetail(writeOperation, this.itemId)
                  )
                : null;
            this.detail = null;
            this.identity = null;
            this.clientIdentityDiagnostic = null;
            this.clientIdentityProblem = null;
            this.qaSummary = null;
            this.qaSummaryProblem = null;
            this.itemSetOptions = [];
            this.isLoading = true;
            this.isLoadingQaSummary = true;
            this.isLoadingClientIdentity = true;
            this.refreshView();
          });

          if (!this.itemId) {
            this.ngZone.run(() => {
              this.problem = {
                title: 'ItemId inválido',
                detail: 'El parámetro de la ruta debe ser un entero positivo.',
                status: 400
              };
              this.isLoading = false;
              this.isLoadingQaSummary = false;
              this.refreshView();
            });
            return;
          }

          this.loadDetailBundle(this.itemId);
          this.loadQaSummary(this.itemId);
          this.loadClientIdentityDiagnostic(this.itemId);
      });
  }

  protected resolveSetName(detail: ItemDetailDto | null): string {
    if (!detail?.set) {
      return 'Sin set';
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

  private loadDetailBundle(itemId: number): void {
    this.itemsFacade.getItemDetailBundle(itemId)
      .pipe(
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

  private loadClientIdentityDiagnostic(itemId: number): void {
    this.itemsFacade
      .getClientIdentityDiagnostic(itemId)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.clientIdentityProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isLoadingClientIdentity = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((diagnostic) => {
        if (!diagnostic) {
          return;
        }

        this.ngZone.run(() => {
          this.clientIdentityDiagnostic = diagnostic;
          this.refreshView();
        });
      });
  }

  private loadQaSummary(itemId: number): void {
    this.itemsFacade.getItemQaSummary(itemId)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.qaSummaryProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isLoadingQaSummary = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((qaSummary) => {
        if (!qaSummary) {
          return;
        }

        this.ngZone.run(() => {
          this.qaSummary = qaSummary;
          this.refreshView();
        });
      });
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }

  private buildSaveDetail(writeOperation: string, itemId: number): string {
    switch (writeOperation.toLowerCase()) {
      case 'duplicate':
        return `El item duplicado ${itemId} ya esta persistido y listo para validar detalle, preview y efectos.`;
      case 'update':
        return `Los cambios del item ${itemId} ya quedaron guardados. Si algo falla en cliente, conserva el traceId del ultimo error.`;
      case 'create':
        return `El item ${itemId} ya fue creado. Ahora puedes validar identidad, preview y QA readiness desde este detalle.`;
      default:
        return `La operacion de escritura del item ${itemId} termino correctamente.`;
    }
  }
}
