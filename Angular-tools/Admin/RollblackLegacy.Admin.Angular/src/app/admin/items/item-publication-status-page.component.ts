import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ClientIdentityDiagnosticCardComponent } from './components/client-identity-diagnostic-card.component';
import { ClientItemIdentityCheckResultDto } from './data-access/client-identity.models';
import { ItemsFacade } from './data-access/items.facade';
import {
  AdminApiProblem,
  ItemPublicationStatusDto,
  toAdminApiProblem
} from './data-access/items.models';

@Component({
  selector: 'app-item-publication-status-page',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent, ClientIdentityDiagnosticCardComponent],
  templateUrl: './item-publication-status-page.component.html',
  styleUrl: './item-publication-status-page.component.scss'
})
export class ItemPublicationStatusPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected itemId: number | null = null;
  protected status: ItemPublicationStatusDto | null = null;
  protected clientIdentityDiagnostic: ClientItemIdentityCheckResultDto | null = null;
  protected problem: AdminApiProblem | null = null;
  protected clientIdentityProblem: AdminApiProblem | null = null;
  protected isLoading = false;
  protected isLoadingClientIdentity = false;

  ngOnInit(): void {
    this.activatedRoute.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((paramMap) => {
        const nextItemId = Number(paramMap.get('itemId'));

        this.ngZone.run(() => {
          this.itemId = Number.isInteger(nextItemId) && nextItemId > 0 ? nextItemId : null;
          this.status = null;
          this.clientIdentityDiagnostic = null;
          this.problem = null;
          this.clientIdentityProblem = null;
          this.isLoading = true;
          this.isLoadingClientIdentity = true;
          this.refreshView();
        });

        if (!this.itemId) {
          this.ngZone.run(() => {
            this.problem = {
              title: 'ItemId inválido',
              detail: 'La ruta de publication status requiere un ItemId positivo.',
              status: 400
            };
            this.isLoading = false;
            this.refreshView();
          });
          return;
        }

        this.loadPublicationBundle(this.itemId);
      });
  }

  protected get visibilityBadgeClass(): string {
    switch ((this.status?.visibilityState || '').toUpperCase()) {
      case 'VISIBLE':
        return 'text-bg-success';
      case 'VISIBLE_WITH_PATCH':
        return 'text-bg-warning';
      case 'INVISIBLE':
        return 'text-bg-danger';
      default:
        return 'text-bg-secondary';
    }
  }

  protected get publicationBadgeClass(): string {
    switch ((this.status?.publicationState || '').toUpperCase()) {
      case 'PUBLISHED':
        return 'text-bg-success';
      case 'NEEDS_CLIENT_PATCH':
        return 'text-bg-warning';
      default:
        return 'text-bg-secondary';
    }
  }

  protected get clientTemplateBadgeClass(): string {
    switch ((this.status?.clientTemplateState || '').toUpperCase()) {
      case 'CLIENT_KNOWN':
        return 'text-bg-success';
      case 'CLIENT_UNKNOWN':
        return 'text-bg-danger';
      default:
        return 'text-bg-secondary';
    }
  }

  protected get booleanChips(): Array<{ label: string; value: boolean; positiveText: string; negativeText: string }> {
    if (!this.status) {
      return [];
    }

    return [
      {
        label: 'Client Known',
        value: this.status.clientKnown,
        positiveText: 'Sí',
        negativeText: 'No'
      },
      {
        label: 'Published',
        value: this.status.published,
        positiveText: 'Sí',
        negativeText: 'No'
      },
      {
        label: 'Needs Client Patch',
        value: this.status.needsClientPatch,
        positiveText: 'Sí',
        negativeText: 'No'
      },
      {
        label: 'Needs Asset',
        value: this.status.needsAsset,
        positiveText: 'Sí',
        negativeText: 'No'
      },
      {
        label: 'Needs QA',
        value: this.status.needsQa,
        positiveText: 'Sí',
        negativeText: 'No'
      }
    ];
  }

  private loadPublicationBundle(itemId: number): void {
    this.itemsFacade
      .getItemPublicationStatus(itemId)
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
      .subscribe((status) => {
        if (!status) {
          return;
        }

        this.ngZone.run(() => {
          this.status = status;
          this.refreshView();
        });
      });

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

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
