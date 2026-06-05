import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
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
            this.problem = {
              title: 'SetId inválido',
              detail: 'El identificador del set no es válido.',
              status: 400
            };
            return of(null);
          }

          this.isLoading = true;
          this.problem = null;
          return this.itemsFacade.getItemSet(setId).pipe(
            catchError((error: unknown) => {
              this.problem = toAdminApiProblem(error);
              return of(null);
            }),
            finalize(() => {
              this.isLoading = false;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((detail) => {
        this.detail = detail;
      });
  }
}
