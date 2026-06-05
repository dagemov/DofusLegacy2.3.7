import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemsFacade } from '../items/data-access/items.facade';
import { AdminApiProblem, ItemSetListItemDto, toAdminApiProblem } from '../items/data-access/items.models';

@Component({
  selector: 'app-item-sets-page',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './item-sets-page.component.html',
  styleUrl: './item-sets-page.component.scss'
})
export class ItemSetsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly itemsFacade = inject(ItemsFacade);

  protected sets: ItemSetListItemDto[] = [];
  protected problem: AdminApiProblem | null = null;
  protected isLoading = false;

  ngOnInit(): void {
    this.isLoading = true;
    this.itemsFacade
      .getItemSets()
      .pipe(
        catchError((error: unknown) => {
          this.problem = toAdminApiProblem(error);
          return of([]);
        }),
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((sets) => {
        this.sets = sets;
      });
  }
}
