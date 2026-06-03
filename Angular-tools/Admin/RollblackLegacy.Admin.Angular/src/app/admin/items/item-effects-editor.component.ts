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

const SERIALIZATION_TYPE_INTEGER = 70;
const SERIALIZATION_TYPE_DICE = 73;

const EDITABLE_FORMATS = ['Integer', 'Dice'] as const;
type EditableEffectFormat = (typeof EDITABLE_FORMATS)[number];

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
  protected effectOptionsById = new Map<number, AdminEffectOptionDto>();
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
  protected addSearchTerm = '';
  protected addGroupFilter = '';
  protected readonly editableFormats = EDITABLE_FORMATS;

  protected get saveFeedback(): AdminFeedback | null {
    if (!this.saveMessage) {
      return null;
    }

    return createAdminSuccessFeedback('Efectos guardados', this.saveMessage);
  }

  protected get effectOptionGroups(): { group: string; options: AdminEffectOptionDto[] }[] {
    const groups = new Map<string, AdminEffectOptionDto[]>();

    for (const option of this.effectOptions) {
      const bucket = groups.get(option.group) ?? [];
      bucket.push(option);
      groups.set(option.group, bucket);
    }

    return Array.from(groups.entries())
      .sort(([left], [right]) => left.localeCompare(right, 'es'))
      .map(([group, options]) => ({
        group,
        options: options.sort(
          (left, right) =>
            left.sortPriority - right.sortPriority || left.label.localeCompare(right.label, 'es')
        )
      }));
  }

  protected get addGroupChoices(): string[] {
    return [...new Set(this.effectOptions.map((option) => option.group))].sort((left, right) =>
      left.localeCompare(right, 'es')
    );
  }

  protected get filteredAddOptions(): AdminEffectOptionDto[] {
    const term = this.addSearchTerm.trim().toLowerCase();

    return this.effectOptions.filter((option) => {
      if (this.addGroupFilter && option.group !== this.addGroupFilter) {
        return false;
      }

      if (!term) {
        return true;
      }

      return (
        option.label.toLowerCase().includes(term) ||
        option.protocolName.toLowerCase().includes(term) ||
        String(option.effectId).includes(term)
      );
    });
  }

  protected get filteredAddOptionGroups(): { group: string; options: AdminEffectOptionDto[] }[] {
    const allowed = new Set(this.filteredAddOptions.map((option) => option.effectId));

    return this.effectOptionGroups
      .map((group) => ({
        group: group.group,
        options: group.options.filter((option) => allowed.has(option.effectId))
      }))
      .filter((group) => group.options.length > 0);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['itemId']) {
      this.loadEditor();
    }
  }

  protected trackRow(_index: number, row: ItemEffectEditDto): string {
    return row.rowId;
  }

  protected isFormatEditable(row: ItemEffectEditDto): boolean {
    return row.isSupported && EDITABLE_FORMATS.includes(row.operatorMode as EditableEffectFormat);
  }

  protected canChangeFormat(row: ItemEffectEditDto): boolean {
    return row.isSupported && !row.preservedEffectHex;
  }

  protected addEffect(): void {
    if (!this.selectedEffectId) {
      return;
    }

    const option = this.effectOptionsById.get(this.selectedEffectId);
    if (!option) {
      return;
    }

    this.rows = [...this.rows, this.createRowFromOption(option)];
    this.selectedEffectId = null;
    this.saveMessage = null;
    this.refreshView();
  }

  protected onRowEffectChange(row: ItemEffectEditDto, rawEffectId: string | number): void {
    if (!row.isSupported || row.preservedEffectHex) {
      return;
    }

    const effectId = Number(rawEffectId);
    const option = this.effectOptionsById.get(effectId);
    if (!option) {
      return;
    }

    row.effectId = option.effectId;
    row.label = option.label;
    row.group = option.group;
    row.operatorMode = option.format || option.operatorMode;
    row.serializationTypeId = option.defaultSerializationTypeId;
    row.isCharacteristic = option.isCharacteristic;
    row.isSupported = true;
    row.preservedEffectHex = null;
    row.warning = null;
    this.updatePreview(row);
    this.saveMessage = null;
    this.refreshView();
  }

  protected onRowFormatChange(row: ItemEffectEditDto, format: string): void {
    if (!this.canChangeFormat(row)) {
      return;
    }

    row.operatorMode = format;
    row.serializationTypeId = this.serializationTypeForFormat(format);
    this.updatePreview(row);
    this.saveMessage = null;
    this.refreshView();
  }

  protected onRowValueChange(row: ItemEffectEditDto): void {
    this.updatePreview(row);
    this.saveMessage = null;
  }

  protected moveRow(row: ItemEffectEditDto, direction: -1 | 1): void {
    const index = this.rows.findIndex((entry) => entry.rowId === row.rowId);
    if (index < 0) {
      return;
    }

    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= this.rows.length) {
      return;
    }

    const next = [...this.rows];
    [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
    this.rows = next;
    this.saveMessage = null;
    this.refreshView();
  }

  protected removeRow(row: ItemEffectEditDto): void {
    if (!row.isSupported) {
      const confirmed = window.confirm(
        'Este efecto no es editable en el codec actual. Si lo eliminas, se perderá del payload preservado. ¿Continuar?'
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
        this.setEffectOptions(bundle.options);
        this.refreshView();
      });
  }

  private setEffectOptions(options: AdminEffectOptionDto[]): void {
    this.effectOptions = [...options].sort(
      (left, right) =>
        left.sortPriority - right.sortPriority ||
        left.label.localeCompare(right.label, 'es') ||
        left.effectId - right.effectId
    );
    this.effectOptionsById = new Map(this.effectOptions.map((option) => [option.effectId, option]));
  }

  private createRowFromOption(option: AdminEffectOptionDto): ItemEffectEditDto {
    const format = option.format || option.operatorMode;

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
      operatorMode: format,
      group: option.group,
      isCharacteristic: option.isCharacteristic,
      isSupported: true,
      previewText: ''
    };

    this.updatePreview(row);
    return row;
  }

  private serializationTypeForFormat(format: string): number {
    switch (format) {
      case 'Dice':
        return SERIALIZATION_TYPE_DICE;
      case 'MinMax':
        return 82;
      case 'Duration':
        return 75;
      case 'Base':
        return 76;
      default:
        return SERIALIZATION_TYPE_INTEGER;
    }
  }

  private updatePreview(row: ItemEffectEditDto): void {
    if (!row.isSupported) {
      return;
    }

    const label = row.label;

    switch (row.operatorMode) {
      case 'Dice':
        row.previewText = `${label}: ${row.diceNum}d${row.diceSide}+${row.value}`;
        break;
      case 'MinMax':
        row.previewText = `${label}: ${row.minValue}..${row.maxValue}`;
        break;
      case 'Duration':
        row.previewText = `${label}: ${row.diceNum}d ${row.diceSide}h ${row.value}m`;
        break;
      case 'Base':
        row.previewText = label;
        break;
      default:
        row.previewText = `${label}: ${row.value}`;
        break;
    }
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
