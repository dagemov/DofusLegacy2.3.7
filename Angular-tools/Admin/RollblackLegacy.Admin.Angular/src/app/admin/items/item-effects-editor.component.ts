import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { catchError, finalize, forkJoin, of } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemsFacade } from './data-access/items.facade';
import {
  AdminFeedback,
  AdminApiProblem,
  AdminEffectOptionDto,
  ItemEffectEditDto,
  ItemEffectEditRowRequest,
  ItemEffectsEditDto,
  ItemEffectsUpdateRequest,
  createAdminSuccessFeedback,
  toAdminApiProblem
} from './data-access/items.models';

@Component({
  selector: 'app-item-effects-editor',
  imports: [CommonModule, FormsModule, ApiProblemPanelComponent],
  templateUrl: './item-effects-editor.component.html',
  styleUrl: './item-effects-editor.component.scss'
})
export class ItemEffectsEditorComponent implements OnChanges {
  private readonly destroyRef = inject(DestroyRef);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  @Input({ required: true }) itemId!: number;

  protected effectOptions: AdminEffectOptionDto[] = [];
  protected editState: ItemEffectsEditDto | null = null;
  protected rows: ItemEffectEditDto[] = [];
  protected preservedSuffixHex: string | null = null;
  protected warnings: string[] = [];
  protected loadProblem: AdminApiProblem | null = null;
  protected saveProblem: AdminApiProblem | null = null;
  protected saveMessage: string | null = null;
  protected isLoading = false;
  protected isSaving = false;
  protected selectedEffectId: number | null = null;

  protected get saveFeedback(): AdminFeedback | null {
    if (!this.saveMessage) {
      return null;
    }

    return createAdminSuccessFeedback('Efectos guardados', this.saveMessage);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['itemId']) {
      this.loadEditor();
    }
  }

  protected get groupedRows(): { group: string; rows: ItemEffectEditDto[] }[] {
    const groups = new Map<string, ItemEffectEditDto[]>();

    for (const row of this.rows) {
      const key = row.group || 'Other / unsupported';
      const bucket = groups.get(key) ?? [];
      bucket.push(row);
      groups.set(key, bucket);
    }

    return Array.from(groups.entries()).map(([group, rows]) => ({ group, rows }));
  }

  protected addCharacteristic(): void {
    if (!this.selectedEffectId) {
      return;
    }

    const option = this.effectOptions.find((entry) => entry.effectId === this.selectedEffectId);
    if (!option) {
      return;
    }

    const row: ItemEffectEditDto = {
      rowId: `new-${option.effectId}-${Date.now()}`,
      serializationTypeId: option.defaultSerializationTypeId,
      effectId: option.effectId,
      label: option.label,
      diceNum: 0,
      diceSide: 0,
      value: 0,
      minValue: 0,
      maxValue: 0,
      operatorMode: option.operatorMode,
      group: option.group,
      isCharacteristic: true,
      isSupported: true,
      previewText: `${option.label}: 0`
    };

    this.rows = [...this.rows, row];
    this.selectedEffectId = null;
    this.saveMessage = null;
    this.refreshView();
  }

  protected removeRow(row: ItemEffectEditDto): void {
    if (!row.isSupported) {
      const confirmed = window.confirm(
        'Este efecto no es editable en Phase 7B. Si lo eliminas, se perderá del payload preservado. ¿Continuar?'
      );

      if (!confirmed) {
        return;
      }
    }

    this.rows = this.rows.filter((entry) => entry.rowId !== row.rowId);
    this.saveMessage = null;
    this.refreshView();
  }

  protected saveEffects(): void {
    if (!this.editState) {
      return;
    }

    this.isSaving = true;
    this.saveProblem = null;
    this.saveMessage = null;

    const removedUnsupportedRowIds = (this.editState.effects ?? [])
      .filter((row) => !row.isSupported && !this.rows.some((entry) => entry.rowId === row.rowId))
      .map((row) => row.rowId);

    const request: ItemEffectsUpdateRequest = {
      effects: this.rows.map((row) => this.toRequestRow(row)),
      preservedSuffixHex: this.preservedSuffixHex,
      removedUnsupportedRowIds
    };

    this.itemsFacade
      .updateItemEffects(this.itemId, request)
      .pipe(
        catchError((error: unknown) => {
          this.saveProblem = toAdminApiProblem(error);
          return of(null);
        }),
        finalize(() => {
          this.isSaving = false;
          this.refreshView();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        if (!result) {
          return;
        }

        this.editState = {
          itemId: result.itemId,
          effectsHex: result.effectsHex,
          effects: result.effects,
          preservedSuffixHex: this.preservedSuffixHex,
          warnings: result.warnings,
          hasUnsupportedEffects: result.effects.some((row) => !row.isSupported)
        };
        this.rows = [...result.effects];
        this.warnings = result.warnings;
        this.saveMessage = 'Efectos guardados en sunshine.items.Effects.';
        this.refreshView();
      });
  }

  private loadEditor(): void {
    if (!this.itemId || this.itemId <= 0) {
      return;
    }

    this.isLoading = true;
    this.loadProblem = null;
    this.saveProblem = null;
    this.saveMessage = null;

    forkJoin({
      edit: this.itemsFacade.getItemEffectsEdit(this.itemId),
      options: this.itemsFacade.getItemEffectOptions().pipe(catchError(() => of([] as AdminEffectOptionDto[])))
    })
      .pipe(
        catchError((error: unknown) => {
          this.loadProblem = toAdminApiProblem(error);
          return of(null);
        }),
        finalize(() => {
          this.isLoading = false;
          this.refreshView();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((bundle) => {
        if (!bundle) {
          return;
        }

        this.editState = bundle.edit;
        this.rows = [...bundle.edit.effects];
        this.preservedSuffixHex = bundle.edit.preservedSuffixHex ?? null;
        this.warnings = bundle.edit.warnings ?? [];
        this.effectOptions = bundle.options;
        this.refreshView();
      });
  }

  private toRequestRow(row: ItemEffectEditDto): ItemEffectEditRowRequest {
    return {
      rowId: row.rowId,
      serializationTypeId: row.serializationTypeId,
      effectId: row.effectId,
      diceNum: row.diceNum,
      diceSide: row.diceSide,
      value: row.value,
      minValue: row.minValue,
      maxValue: row.maxValue,
      preservedEffectHex: row.preservedEffectHex ?? null
    };
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
