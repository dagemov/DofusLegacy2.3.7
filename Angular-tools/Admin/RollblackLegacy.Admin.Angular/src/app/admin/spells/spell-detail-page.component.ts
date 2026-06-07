import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { SpellsFacade } from './data-access/spells.facade';
import {
  AdminApiProblem,
  SpellBreedSummaryDto,
  SpellDetailDto,
  SpellEffectRowDto,
  SpellLevelDetailDto,
  SpellLevelEffectsDto,
  toAdminApiProblem
} from './data-access/spells.models';

@Component({
  selector: 'app-spell-detail-page',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './spell-detail-page.component.html',
  styleUrl: './spell-detail-page.component.scss'
})
export class SpellDetailPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly spellsFacade = inject(SpellsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected spellId: number | null = null;
  protected detail: SpellDetailDto | null = null;
  protected levels: SpellLevelDetailDto[] = [];
  protected selectedLevelNumber: number | null = null;
  protected expandedEffectsLevelNumber: number | null = null;

  protected detailProblem: AdminApiProblem | null = null;
  protected levelsProblem: AdminApiProblem | null = null;
  protected effectsProblems: Record<number, AdminApiProblem | null> = {};

  protected isLoadingDetail = false;
  protected isLoadingLevels = false;
  protected effectsLoading: Record<number, boolean> = {};

  protected levelEffectsCache: Record<number, SpellLevelEffectsDto | null> = {};

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
    this.selectedLevelNumber = levelNumber;
  }

  protected toggleEffects(levelNumber: number): void {
    if (!this.spellId) {
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
    if (!this.spellId) {
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
        this.levels.find((level) => level.levelNumber === this.selectedLevelNumber) ?? this.levels[0] ?? null
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
      this.effectsLoading = {};
      this.levelEffectsCache = {};
      this.isLoadingDetail = false;
      this.isLoadingLevels = false;
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

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
