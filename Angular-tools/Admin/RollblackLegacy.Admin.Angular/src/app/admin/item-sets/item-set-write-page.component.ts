import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of, switchMap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemPreviewCardComponent } from '../items/components/item-preview-card.component';
import { ItemsFacade } from '../items/data-access/items.facade';
import {
  AdminApiProblem,
  AdminEffectOptionDto,
  AdminFeedback,
  ItemDetailDto,
  ItemSetBonusEffectWriteDto,
  ItemSetBonusTierWriteDto,
  ItemSetDetailDto,
  ItemSetMemberDto,
  ItemSetWriteRequest,
  createAdminSuccessFeedback,
  createEmptyItemSearchRequest,
  createUnknownPreviewState,
  toAdminApiProblem
} from '../items/data-access/items.models';
import { ItemSetBonusEditorComponent } from './item-set-bonus-editor.component';

type WriteMode = 'create' | 'edit';

@Component({
  selector: 'app-item-set-write-page',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ApiProblemPanelComponent,
    ItemPreviewCardComponent,
    ItemSetBonusEditorComponent
  ],
  templateUrl: './item-set-write-page.component.html',
  styleUrl: './item-set-write-page.component.scss'
})
export class ItemSetWritePageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected mode: WriteMode = 'create';
  protected setId = 0;
  protected name = '';
  protected level = 0;
  protected members: ItemSetMemberDto[] = [];
  protected bonusTiers: ItemSetBonusTierWriteDto[] = [];
  protected effectOptions: AdminEffectOptionDto[] = [];
  protected loadProblem: AdminApiProblem | null = null;
  protected saveProblem: AdminApiProblem | null = null;
  protected saveMessage: string | null = null;
  protected isLoading = false;
  protected isSaving = false;
  protected addItemId: number | null = null;
  protected addItemSearch = '';
  protected readonly unknownPreview = createUnknownPreviewState();

  ngOnInit(): void {
    this.activatedRoute.paramMap
      .pipe(
        switchMap((paramMap) => {
          const rawSetId = paramMap.get('setId');
          this.mode = rawSetId ? 'edit' : 'create';
          this.setId = rawSetId ? Number(rawSetId) : 0;

          this.ngZone.run(() => {
            this.isLoading = true;
            this.loadProblem = null;
            this.saveProblem = null;
            this.refreshView();
          });

          const effectOptions$ = this.itemsFacade.getItemEffectOptions().pipe(catchError(() => of([])));
          const detail$ =
            this.mode === 'edit' && this.setId > 0
              ? this.itemsFacade.getItemSet(this.setId).pipe(catchError((error: unknown) => {
                  this.ngZone.run(() => {
                    this.loadProblem = toAdminApiProblem(error);
                    this.refreshView();
                  });
                  return of(null);
                }))
              : of(null);

          return forkJoin({ effectOptions: effectOptions$, detail: detail$ }).pipe(
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
      .subscribe(({ effectOptions, detail }) => {
        this.ngZone.run(() => {
          this.effectOptions = effectOptions;
          if (detail) {
            this.applyDetail(detail);
          }
          this.refreshView();
        });
      });
  }

  protected get saveFeedback(): AdminFeedback | null {
    return this.saveMessage ? createAdminSuccessFeedback('Set guardado', this.saveMessage) : null;
  }

  protected get memberItemIds(): number[] {
    return this.members.map((member) => member.itemId);
  }

  protected async addItemById(): Promise<void> {
    if (!this.addItemId || this.addItemId <= 0) {
      return;
    }

    await this.resolveAndAddItems([this.addItemId]);
    this.addItemId = null;
  }

  protected async searchAndAddItem(): Promise<void> {
    const term = this.addItemSearch.trim();
    if (!term) {
      return;
    }

    const request = createEmptyItemSearchRequest();
    const numeric = Number(term);
    if (Number.isInteger(numeric) && numeric > 0) {
      request.itemId = numeric;
    } else {
      request.search = term;
      request.pageSize = 5;
    }

    this.itemsFacade
      .getItems(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(async (result) => {
        const ids = result.items.map((item) => item.itemId);
        await this.resolveAndAddItems(ids);
        this.addItemSearch = '';
        this.refreshView();
      });
  }

  protected removeMember(itemId: number): void {
    this.members = this.members.filter((member) => member.itemId !== itemId);
    this.refreshView();
  }

  protected save(): void {
    const request: ItemSetWriteRequest = {
      name: this.name.trim(),
      level: this.level,
      itemIds: this.memberItemIds,
      bonusTiers: this.bonusTiers
        .filter((tier) => tier.effects.length > 0)
        .map((tier) => ({
          pieceCount: tier.pieceCount,
          effects: tier.effects.filter((effect) => effect.effectId > 0)
        }))
    };

    this.isSaving = true;
    this.saveProblem = null;
    this.saveMessage = null;

    const save$ =
      this.mode === 'edit'
        ? this.itemsFacade.updateItemSet(this.setId, request)
        : this.itemsFacade.createItemSet(request);

    save$
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.saveProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isSaving = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        if (!result) {
          return;
        }

        this.ngZone.run(() => {
          this.saveMessage = result.message;
          this.refreshView();
        });

        void this.router.navigate(['/admin/item-sets', result.setId]);
      });
  }

  protected deleteSet(): void {
    if (this.mode !== 'edit' || this.setId <= 0) {
      return;
    }

    if (!confirm(`¿Eliminar el set #${this.setId}?`)) {
      return;
    }

    this.isSaving = true;
    this.itemsFacade
      .deleteItemSet(this.setId)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.saveProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isSaving = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((deleted) => {
        if (deleted === null) {
          return;
        }

        void this.router.navigate(['/admin/item-sets']);
      });
  }

  private applyDetail(detail: ItemSetDetailDto): void {
    this.name = detail.name;
    this.level = detail.level;
    this.members = [...detail.items];
    this.bonusTiers = detail.bonusTiers.map((tier) => ({
      pieceCount: tier.pieceCount,
      effects: tier.effects.map(
        (effect): ItemSetBonusEffectWriteDto => ({
          effectId: effect.effectId,
          value: effect.value,
          diceNum: effect.diceNum ?? null,
          diceSide: effect.diceSide ?? null,
          format: effect.format
        })
      )
    }));
  }

  private async resolveAndAddItems(itemIds: number[]): Promise<void> {
    for (const itemId of itemIds) {
      if (this.members.some((member) => member.itemId === itemId)) {
        continue;
      }

      this.itemsFacade
        .getItem(itemId)
        .pipe(
          catchError(() => of(null)),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe((item) => {
          if (!item) {
            return;
          }

          this.members.push(this.mapDetailToMember(item));
          this.level = Math.min(
            this.level > 0 ? this.level : item.level,
            item.level
          );
          if (this.level <= 0) {
            this.level = item.level;
          }
          this.refreshView();
        });
    }
  }

  private mapDetailToMember(item: ItemDetailDto): ItemSetMemberDto {
    return {
      itemId: item.itemId,
      name: item.resolvedName ?? `Item #${item.itemId}`,
      typeId: item.typeId,
      typeName: item.typeName ?? `Type ${item.typeId}`,
      iconId: item.iconId,
      previewState: item.previewState,
      previewPath: item.previewState.resolvedPath,
      publicationSummary: null
    };
  }

  private refreshView(): void {
    this.changeDetectorRef.markForCheck();
  }
}
