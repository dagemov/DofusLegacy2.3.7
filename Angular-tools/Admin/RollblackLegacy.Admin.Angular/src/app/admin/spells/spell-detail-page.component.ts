import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';

import {
  AdminFeedback,
  createAdminSuccessFeedback
} from '../items/data-access/items.models';
import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { SpellsFacade } from './data-access/spells.facade';
import {
  AdminApiProblem,
  SpellBreedSummaryDto,
  SpellDetailDto,
  SpellEffectRowDto,
  SpellLevelDetailDto,
  SpellLevelEffectsDto,
  SpellLevelUpdateRequest,
  SpellLevelUpdateResultDto,
  toAdminApiProblem
} from './data-access/spells.models';

type SpellLevelEditFormControls = {
  apCost: FormControl<number>;
  minRange: FormControl<number>;
  maxRange: FormControl<number>;
  castInLine: FormControl<boolean>;
  castInDiagonal: FormControl<boolean>;
  castTestLos: FormControl<boolean>;
  criticalHitProbability: FormControl<number>;
  criticalFailureProbability: FormControl<number>;
  needFreeCell: FormControl<boolean>;
  needTakenCell: FormControl<boolean>;
  minCastInterval: FormControl<number>;
  initialCooldown: FormControl<number>;
  maxCastPerTurn: FormControl<number>;
  maxCastPerTarget: FormControl<number>;
};

type SpellLevelEditableField = keyof SpellLevelEditFormControls;

@Component({
  selector: 'app-spell-detail-page',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './spell-detail-page.component.html',
  styleUrl: './spell-detail-page.component.scss'
})
export class SpellDetailPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly spellsFacade = inject(SpellsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  protected readonly effectsEditorBlockedReasons = [
    'No existe endpoint PATCH, PUT ni POST para effects o criticalEffects en el Admin API actual.',
    'El schema runtime actual no tiene identidad por fila de effect; cualquier write exigiria reserializar el payload completo del nivel.',
    'Sunshine acepta payload hex serializado y fallback binario legacy; la regla segura para preservarlos todavia no esta definida.'
  ];

  protected readonly levelForm = new FormGroup<SpellLevelEditFormControls>({
    apCost: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    minRange: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    maxRange: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    castInLine: new FormControl(false, { nonNullable: true }),
    castInDiagonal: new FormControl(false, { nonNullable: true }),
    castTestLos: new FormControl(false, { nonNullable: true }),
    criticalHitProbability: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    criticalFailureProbability: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    needFreeCell: new FormControl(false, { nonNullable: true }),
    needTakenCell: new FormControl(false, { nonNullable: true }),
    minCastInterval: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    initialCooldown: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    maxCastPerTurn: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    }),
    maxCastPerTarget: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.min(0)]
    })
  });

  protected spellId: number | null = null;
  protected detail: SpellDetailDto | null = null;
  protected levels: SpellLevelDetailDto[] = [];
  protected selectedLevelNumber: number | null = null;
  protected expandedEffectsLevelNumber: number | null = null;

  protected detailProblem: AdminApiProblem | null = null;
  protected levelsProblem: AdminApiProblem | null = null;
  protected effectsProblems: Record<number, AdminApiProblem | null> = {};
  protected levelSaveProblem: AdminApiProblem | null = null;
  protected levelSaveFeedback: AdminFeedback | null = null;
  protected levelSaveWarnings: string[] = [];

  protected isLoadingDetail = false;
  protected isLoadingLevels = false;
  protected effectsLoading: Record<number, boolean> = {};
  protected isEditingLevel = false;
  protected isSavingLevel = false;

  protected levelEffectsCache: Record<number, SpellLevelEffectsDto | null> = {};

  private hasTriedLevelSubmit = false;
  private editingLevelSnapshot: SpellLevelDetailDto | null = null;

  ngOnInit(): void {
    this.activatedRoute.paramMap
      .pipe(
        switchMap((paramMap) => {
          const spellId = Number(paramMap.get('spellId'));
          this.resetPageState();

          if (!Number.isInteger(spellId) || spellId <= 0) {
            this.problemInvalidSpellId();
            return of(null);
          }

          this.ngZone.run(() => {
            this.spellId = spellId;
            this.isLoadingDetail = true;
            this.isLoadingLevels = true;
            this.refreshView();
          });

          this.loadDetail(spellId);
          this.loadLevels(spellId);
          return of(spellId);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();
  }

  protected selectLevel(levelNumber: number): void {
    if (this.isEditingLevel || this.isSavingLevel) {
      return;
    }

    this.selectedLevelNumber = levelNumber;
  }

  protected startEditLevel(): void {
    const level = this.selectedLevel;
    if (!this.canEditSelectedLevel || !level) {
      return;
    }

    this.expandedEffectsLevelNumber = null;
    this.levelSaveProblem = null;
    this.levelSaveFeedback = null;
    this.levelSaveWarnings = [];
    this.hasTriedLevelSubmit = false;
    this.isEditingLevel = true;
    this.applyLevelToForm(level);
    this.refreshView();
  }

  protected cancelEditLevel(): void {
    const level = this.selectedLevel;
    this.isEditingLevel = false;
    this.isSavingLevel = false;
    this.hasTriedLevelSubmit = false;
    this.levelSaveProblem = null;

    if (level) {
      this.applyLevelToForm(level);
    }

    this.refreshView();
  }

  protected saveLevel(): void {
    const spellId = this.spellId;
    const level = this.selectedLevel;
    const baseline = this.editingLevelSnapshot;
    if (!spellId || !level || !baseline) {
      return;
    }

    this.hasTriedLevelSubmit = true;
    this.levelForm.markAllAsTouched();

    const request = this.buildLevelUpdateRequest(baseline);
    if (
      this.levelForm.invalid ||
      !!this.levelRangeError ||
      !request ||
      !this.canEditSelectedLevel
    ) {
      this.refreshView();
      return;
    }

    this.isSavingLevel = true;
    this.levelSaveProblem = null;
    this.levelSaveFeedback = null;
    this.levelSaveWarnings = [];
    this.refreshView();

    this.spellsFacade
      .updateSpellLevel(spellId, level.levelNumber, request)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.levelSaveProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isSavingLevel = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((result) => {
        if (!result || this.spellId !== spellId) {
          return;
        }

        this.ngZone.run(() => {
          this.levelSaveFeedback = createAdminSuccessFeedback(
            'Nivel guardado',
            this.buildLevelSaveDetail(result)
          );
          this.levelSaveWarnings = result.warnings;
          this.isEditingLevel = false;
          this.hasTriedLevelSubmit = false;
          this.updateLevelInCollection(result.level);
          this.applyLevelToForm(result.level);
          this.refreshView();
        });

        this.reloadSpellDataAfterSave(spellId, result.levelNumber);
      });
  }

  protected toggleEffects(levelNumber: number): void {
    if (!this.spellId || this.isEditingLevel || this.isSavingLevel) {
      return;
    }

    if (this.expandedEffectsLevelNumber === levelNumber) {
      this.expandedEffectsLevelNumber = null;
      return;
    }

    this.expandedEffectsLevelNumber = levelNumber;
    if (this.levelEffectsCache[levelNumber] || this.effectsLoading[levelNumber]) {
      return;
    }

    this.loadEffects(this.spellId, levelNumber);
  }

  protected reloadEffects(levelNumber: number): void {
    if (!this.spellId || this.isEditingLevel || this.isSavingLevel) {
      return;
    }

    this.expandedEffectsLevelNumber = levelNumber;
    this.loadEffects(this.spellId, levelNumber);
  }

  protected trackByLevelNumber(_index: number, level: SpellLevelDetailDto): number {
    return level.levelNumber;
  }

  protected trackByEffectRow(_index: number, row: SpellEffectRowDto): string {
    return `${row.rowIndex}-${row.effectId}-${row.protocolName}`;
  }

  protected get selectedLevel(): SpellLevelDetailDto | null {
    if (this.levels.length > 0 && this.selectedLevelNumber !== null) {
      return (
        this.levels.find((level) => level.levelNumber === this.selectedLevelNumber) ??
        this.levels[0] ??
        null
      );
    }

    if (this.levels.length > 0) {
      return this.levels[0] ?? null;
    }

    return null;
  }

  protected get selectedLevelEffects(): SpellLevelEffectsDto | null {
    const levelNumber = this.selectedLevel?.levelNumber;
    if (!levelNumber) {
      return null;
    }

    return this.levelEffectsCache[levelNumber] ?? null;
  }

  protected get canEditSelectedLevel(): boolean {
    const level = this.selectedLevel;
    return !!this.spellId && !!level && level.runtimeAvailable && !this.isLoadingLevels;
  }

  protected get isLegacySelectedLevel(): boolean {
    const runtimeLevelId = this.selectedLevel?.runtimeLevelId;
    return runtimeLevelId !== null && runtimeLevelId !== undefined;
  }

  protected get hasLevelChanges(): boolean {
    return !!this.editingLevelSnapshot && !!this.buildLevelUpdateRequest(this.editingLevelSnapshot);
  }

  protected get levelSaveDisabled(): boolean {
    return (
      !this.isEditingLevel ||
      this.isSavingLevel ||
      !this.canEditSelectedLevel ||
      this.levelForm.invalid ||
      !!this.levelRangeError ||
      !this.hasLevelChanges
    );
  }

  protected get levelRangeError(): string | null {
    if (!this.isEditingLevel) {
      return null;
    }

    const minRange = this.levelForm.controls.minRange.value;
    const maxRange = this.levelForm.controls.maxRange.value;
    return maxRange < minRange ? 'maxRange debe ser mayor o igual a minRange.' : null;
  }

  protected isEffectsExpanded(levelNumber: number): boolean {
    return this.expandedEffectsLevelNumber === levelNumber;
  }

  protected isEffectsLoading(levelNumber: number): boolean {
    return this.effectsLoading[levelNumber] === true;
  }

  protected getEffectsProblem(levelNumber: number): AdminApiProblem | null {
    return this.effectsProblems[levelNumber] ?? null;
  }

  protected formatSpellName(detail: SpellDetailDto | null): string {
    if (!detail) {
      return 'Detalle de spell';
    }

    return detail.name?.trim() || `Hechizo #${detail.spellId}`;
  }

  protected formatDescription(description?: string | null): string {
    return description?.trim() || 'Sin descripcion runtime disponible.';
  }

  protected formatBreeds(breeds: SpellBreedSummaryDto[]): string {
    if (!breeds || breeds.length === 0) {
      return 'Sin clases runtime registradas';
    }

    return breeds
      .map((breed) => breed.label?.trim() || `BreedId ${breed.breedId}`)
      .join(', ');
  }

  protected formatReferenceBreeds(): string {
    const reference = this.detail?.reference;
    if (!reference || reference.breedIds.length === 0) {
      return 'Sin clases en referencia';
    }

    const runtimeLabels = new Map<number, string>();
    for (const breed of this.detail?.breeds ?? []) {
      const label = breed.label?.trim();
      if (label) {
        runtimeLabels.set(breed.breedId, label);
      }
    }

    return reference.breedIds
      .map((breedId) => runtimeLabels.get(breedId) ?? `BreedId ${breedId}`)
      .join(', ');
  }

  protected buildLevelPillSummary(level: SpellLevelDetailDto): string {
    const rangeSummary = `Rango ${level.minRange}-${level.maxRange}`;
    const effectsSummary = level.hasCriticalEffects
      ? `${level.apCost} PA, normal + critico`
      : `${level.apCost} PA, normal`;
    return `${rangeSummary} | ${effectsSummary}`;
  }

  protected formatBooleanFlag(value: boolean): string {
    return value ? 'Si' : 'No';
  }

  protected buildEffectValueSummary(row: SpellEffectRowDto): string {
    return `${row.minValue} / ${row.maxValue} / ${row.value}`;
  }

  protected buildEffectBehaviorSummary(row: SpellEffectRowDto): string {
    const parts = [row.operatorMode, `dur ${row.duration}`];

    if (row.delay !== null && row.delay !== undefined) {
      parts.push(`delay ${row.delay}`);
    }

    if (row.random !== null && row.random !== undefined) {
      parts.push(`random ${row.random}`);
    }

    return parts.join(' | ');
  }

  protected buildEffectSourceSummary(row: SpellEffectRowDto): string {
    return `${row.protocolName} | ${row.group}`;
  }

  protected buildEffectTargetZoneSummary(row: SpellEffectRowDto): string {
    return `target ${row.targetType} | zone ${row.zoneShape}/${row.zoneMinSize}/${row.zoneSize}`;
  }

  protected getLevelFieldErrors(fieldName: string): string[] {
    return this.levelSaveProblem?.errors?.[fieldName] ?? [];
  }

  protected hasLevelFieldIssue(fieldName: SpellLevelEditableField): boolean {
    return (
      this.getLevelFieldErrors(fieldName).length > 0 || !!this.getLevelLocalError(fieldName)
    );
  }

  protected getLevelLocalError(fieldName: SpellLevelEditableField): string | null {
    const control = this.levelForm.controls[fieldName];
    const shouldShow = this.shouldShowLevelLocalError(control);
    const shouldShowRange =
      fieldName === 'maxRange' &&
      !!this.levelRangeError &&
      (this.hasTriedLevelSubmit ||
        this.levelForm.controls.maxRange.touched ||
        this.levelForm.controls.minRange.touched);

    if (!shouldShow && !shouldShowRange) {
      return null;
    }

    if (fieldName === 'maxRange' && this.levelRangeError) {
      return this.levelRangeError;
    }

    if (!control.hasError('min')) {
      return null;
    }

    switch (fieldName) {
      case 'apCost':
        return 'apCost no puede ser negativo.';
      case 'minRange':
        return 'minRange no puede ser negativo.';
      case 'maxRange':
        return 'maxRange no puede ser negativo.';
      case 'criticalHitProbability':
        return 'criticalHitProbability no puede ser negativo.';
      case 'criticalFailureProbability':
        return 'criticalFailureProbability no puede ser negativo.';
      case 'minCastInterval':
        return 'minCastInterval no puede ser negativo.';
      case 'initialCooldown':
        return 'initialCooldown no puede ser negativo.';
      case 'maxCastPerTurn':
        return 'maxCastPerTurn no puede ser negativo.';
      case 'maxCastPerTarget':
        return 'maxCastPerTarget no puede ser negativo.';
      default:
        return 'El valor esta por debajo del minimo soportado.';
    }
  }

  private loadDetail(spellId: number): void {
    this.spellsFacade
      .getSpell(spellId)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.detailProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isLoadingDetail = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((detail) => {
        if (!detail || this.spellId !== spellId) {
          return;
        }

        this.ngZone.run(() => {
          this.detail = detail;
          this.refreshSelectedLevelFromDetail(detail);
          this.refreshView();
        });
      });
  }

  private loadLevels(spellId: number): void {
    this.spellsFacade
      .getSpellLevels(spellId)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.levelsProblem = toAdminApiProblem(error);
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.isLoadingLevels = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((levels) => {
        if (!levels || this.spellId !== spellId) {
          return;
        }

        this.ngZone.run(() => {
          this.levels = levels;
          this.ensureSelectedLevel();

          if (this.isEditingLevel && this.selectedLevel) {
            this.applyLevelToForm(this.selectedLevel);
          }

          this.refreshView();
        });
      });
  }

  private loadEffects(spellId: number, levelNumber: number): void {
    this.effectsLoading[levelNumber] = true;
    this.effectsProblems[levelNumber] = null;
    this.refreshView();

    this.spellsFacade
      .getSpellLevelEffects(spellId, levelNumber)
      .pipe(
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            this.effectsProblems[levelNumber] = toAdminApiProblem(error);
            this.levelEffectsCache[levelNumber] = null;
            this.refreshView();
          });
          return of(null);
        }),
        finalize(() => {
          this.ngZone.run(() => {
            this.effectsLoading[levelNumber] = false;
            this.refreshView();
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((effects) => {
        if (!effects || this.spellId !== spellId) {
          return;
        }

        this.ngZone.run(() => {
          this.levelEffectsCache[levelNumber] = effects;
          this.refreshView();
        });
      });
  }

  private reloadSpellDataAfterSave(spellId: number, levelNumber: number): void {
    this.ngZone.run(() => {
      this.selectedLevelNumber = levelNumber;
      this.isLoadingDetail = true;
      this.isLoadingLevels = true;
      this.refreshView();
    });

    this.loadDetail(spellId);
    this.loadLevels(spellId);
  }

  private updateLevelInCollection(updatedLevel: SpellLevelDetailDto): void {
    this.levels = this.levels.map((level) =>
      level.levelNumber === updatedLevel.levelNumber ? updatedLevel : level
    );
  }

  private applyLevelToForm(level: SpellLevelDetailDto): void {
    this.editingLevelSnapshot = {
      ...level,
      statesRequired: [...level.statesRequired],
      statesForbidden: [...level.statesForbidden]
    };

    this.levelForm.reset(
      {
        apCost: level.apCost,
        minRange: level.minRange,
        maxRange: level.maxRange,
        castInLine: level.castInLine,
        castInDiagonal: level.castInDiagonal,
        castTestLos: level.castTestLos,
        criticalHitProbability: level.criticalHitProbability,
        criticalFailureProbability: level.criticalFailureProbability,
        needFreeCell: level.needFreeCell,
        needTakenCell: level.needTakenCell,
        minCastInterval: level.minCastInterval,
        initialCooldown: level.initialCooldown,
        maxCastPerTurn: level.maxCastPerTurn,
        maxCastPerTarget: level.maxCastPerTarget
      },
      { emitEvent: false }
    );

    this.setLegacyRestrictedControlsEnabled(!this.isLevelLegacy(level));
    this.levelForm.markAsPristine();
    this.levelForm.markAsUntouched();
  }

  private setLegacyRestrictedControlsEnabled(enabled: boolean): void {
    if (enabled) {
      this.levelForm.controls.castInDiagonal.enable({ emitEvent: false });
      this.levelForm.controls.needTakenCell.enable({ emitEvent: false });
      this.levelForm.controls.initialCooldown.enable({ emitEvent: false });
      return;
    }

    this.levelForm.controls.castInDiagonal.disable({ emitEvent: false });
    this.levelForm.controls.needTakenCell.disable({ emitEvent: false });
    this.levelForm.controls.initialCooldown.disable({ emitEvent: false });
  }

  private buildLevelUpdateRequest(
    baseline: SpellLevelDetailDto
  ): SpellLevelUpdateRequest | null {
    const formValue = this.levelForm.getRawValue();
    const request: SpellLevelUpdateRequest = {};

    if (formValue.apCost !== baseline.apCost) {
      request.apCost = formValue.apCost;
    }

    if (formValue.minRange !== baseline.minRange) {
      request.minRange = formValue.minRange;
    }

    if (formValue.maxRange !== baseline.maxRange) {
      request.maxRange = formValue.maxRange;
    }

    if (formValue.castInLine !== baseline.castInLine) {
      request.castInLine = formValue.castInLine;
    }

    if (formValue.castTestLos !== baseline.castTestLos) {
      request.castTestLos = formValue.castTestLos;
    }

    if (formValue.criticalHitProbability !== baseline.criticalHitProbability) {
      request.criticalHitProbability = formValue.criticalHitProbability;
    }

    if (formValue.criticalFailureProbability !== baseline.criticalFailureProbability) {
      request.criticalFailureProbability = formValue.criticalFailureProbability;
    }

    if (formValue.needFreeCell !== baseline.needFreeCell) {
      request.needFreeCell = formValue.needFreeCell;
    }

    if (formValue.minCastInterval !== baseline.minCastInterval) {
      request.minCastInterval = formValue.minCastInterval;
    }

    if (formValue.maxCastPerTurn !== baseline.maxCastPerTurn) {
      request.maxCastPerTurn = formValue.maxCastPerTurn;
    }

    if (formValue.maxCastPerTarget !== baseline.maxCastPerTarget) {
      request.maxCastPerTarget = formValue.maxCastPerTarget;
    }

    if (!this.isLevelLegacy(baseline)) {
      if (formValue.castInDiagonal !== baseline.castInDiagonal) {
        request.castInDiagonal = formValue.castInDiagonal;
      }

      if (formValue.needTakenCell !== baseline.needTakenCell) {
        request.needTakenCell = formValue.needTakenCell;
      }

      if (formValue.initialCooldown !== baseline.initialCooldown) {
        request.initialCooldown = formValue.initialCooldown;
      }
    }

    return Object.keys(request).length > 0 ? request : null;
  }

  private buildLevelSaveDetail(result: SpellLevelUpdateResultDto): string {
    const baseMessage =
      `PATCH /api/admin/v1/spells/${result.spellId}/levels/${result.levelNumber} aplicado ` +
      `con estrategia ${result.writeStrategy}.`;

    if (result.warnings.length === 0) {
      return baseMessage;
    }

    return `${baseMessage} Warnings: ${result.warnings.join(' | ')}`;
  }

  private refreshSelectedLevelFromDetail(detail: SpellDetailDto): void {
    if (this.selectedLevelNumber) {
      return;
    }

    const firstLevel = detail.levels[0];
    this.selectedLevelNumber = firstLevel?.levelNumber ?? null;
  }

  private ensureSelectedLevel(): void {
    if (this.levels.length === 0) {
      this.selectedLevelNumber = null;
      this.expandedEffectsLevelNumber = null;
      return;
    }

    const currentLevelNumber = this.selectedLevelNumber;
    if (
      currentLevelNumber !== null &&
      this.levels.some((level) => level.levelNumber === currentLevelNumber)
    ) {
      return;
    }

    this.selectedLevelNumber = this.levels[0]?.levelNumber ?? null;
  }

  private isLevelLegacy(level: SpellLevelDetailDto): boolean {
    return level.runtimeLevelId !== null && level.runtimeLevelId !== undefined;
  }

  private resetPageState(): void {
    this.ngZone.run(() => {
      this.spellId = null;
      this.detail = null;
      this.levels = [];
      this.selectedLevelNumber = null;
      this.expandedEffectsLevelNumber = null;
      this.detailProblem = null;
      this.levelsProblem = null;
      this.effectsProblems = {};
      this.levelSaveProblem = null;
      this.levelSaveFeedback = null;
      this.levelSaveWarnings = [];
      this.effectsLoading = {};
      this.levelEffectsCache = {};
      this.isLoadingDetail = false;
      this.isLoadingLevels = false;
      this.isEditingLevel = false;
      this.isSavingLevel = false;
      this.hasTriedLevelSubmit = false;
      this.editingLevelSnapshot = null;
      this.levelForm.reset(
        {
          apCost: 0,
          minRange: 0,
          maxRange: 0,
          castInLine: false,
          castInDiagonal: false,
          castTestLos: false,
          criticalHitProbability: 0,
          criticalFailureProbability: 0,
          needFreeCell: false,
          needTakenCell: false,
          minCastInterval: 0,
          initialCooldown: 0,
          maxCastPerTurn: 0,
          maxCastPerTarget: 0
        },
        { emitEvent: false }
      );
      this.setLegacyRestrictedControlsEnabled(true);
      this.levelForm.markAsPristine();
      this.levelForm.markAsUntouched();
      this.refreshView();
    });
  }

  private problemInvalidSpellId(): void {
    this.ngZone.run(() => {
      this.detailProblem = {
        title: 'SpellId invalido',
        detail: 'El identificador del spell debe ser un entero positivo.',
        status: 400
      };
      this.refreshView();
    });
  }

  private shouldShowLevelLocalError(control: AbstractControl): boolean {
    return control.invalid && (control.touched || this.hasTriedLevelSubmit);
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
