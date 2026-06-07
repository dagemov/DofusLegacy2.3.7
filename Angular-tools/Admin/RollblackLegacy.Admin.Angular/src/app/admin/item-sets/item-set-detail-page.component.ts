import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemPreviewCardComponent } from '../items/components/item-preview-card.component';
import { ItemsFacade } from '../items/data-access/items.facade';
import {
  AdminApiProblem,
  ItemSetDetailDto,
  createUnknownPreviewState,
  toAdminApiProblem
} from '../items/data-access/items.models';

@Component({
  selector: 'app-item-set-detail-page',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent, ItemPreviewCardComponent],
  templateUrl: './item-set-detail-page.component.html',
  styleUrl: './item-set-detail-page.component.scss'
})
export class ItemSetDetailPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected detail: ItemSetDetailDto | null = null;
  protected problem: AdminApiProblem | null = null;
  protected isLoading = false;
  protected readonly unknownPreview = createUnknownPreviewState();

  ngOnInit(): void {
    this.activatedRoute.paramMap
      .pipe(
        switchMap((paramMap) => {
          const setId = Number(paramMap.get('setId'));
          if (!Number.isInteger(setId) || setId <= 0) {
            this.ngZone.run(() => {
              this.problem = {
                title: 'SetId inválido',
                detail: 'El identificador del set no es válido.',
                status: 400
              };
              this.detail = null;
              this.isLoading = false;
              this.refreshView();
            });
            return of(null);
          }

          this.ngZone.run(() => {
            this.isLoading = true;
            this.problem = null;
            this.refreshView();
          });

          return this.itemsFacade.getItemSet(setId).pipe(
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
      .subscribe((detail) => {
        this.ngZone.run(() => {
          this.detail = detail;
          this.refreshView();
        });
      });
  }

  private refreshView(): void {
    this.changeDetectorRef.markForCheck();
  }
}
