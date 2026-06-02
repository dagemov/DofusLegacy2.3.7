import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, finalize, forkJoin, of, switchMap } from 'rxjs';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { ItemDiagnosticPanelComponent } from './components/item-diagnostic-panel.component';
import { ItemPreviewCardComponent } from './components/item-preview-card.component';
import { ItemsFacade } from './data-access/items.facade';
import {
  AdminApiProblem,
  AdminOptionDto,
  AdminWarningLike,
  ItemDetailDto,
  ItemPreviewStateDto,
  ItemWriteBundle,
  ItemWriteMode,
  ItemWriteRequest,
  ItemWriteResultDto,
  createEmptyItemWriteRequest,
  createItemWriteRequestFromDetail,
  createUnknownPreviewState,
  normalizeItemWriteRequest,
  toAdminApiProblem
} from './data-access/items.models';

type ItemWriteFormControls = {
  resolvedName: FormControl<string>;
  description: FormControl<string>;
  typeId: FormControl<number | null>;
  level: FormControl<number | null>;
  weight: FormControl<number | null>;
  price: FormControl<number | null>;
  iconId: FormControl<number | null>;
  appearanceId: FormControl<number | null>;
  setId: FormControl<number | null>;
  conditions: FormControl<string>;
  isVisible: FormControl<boolean>;
  usable: FormControl<boolean>;
  targetable: FormControl<boolean>;
  twoHanded: FormControl<boolean>;
  etheral: FormControl<boolean>;
};

type ItemWriteFieldName = keyof ItemWriteFormControls;

@Component({
  selector: 'app-item-write-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ApiProblemPanelComponent,
    ItemPreviewCardComponent,
    ItemDiagnosticPanelComponent
  ],
  templateUrl: './item-write-page.component.html',
  styleUrl: './item-write-page.component.scss'
})
export class ItemWritePageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly itemsFacade = inject(ItemsFacade);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  private previewRequestVersion = 0;
  private hasTriedSubmit = false;

  protected readonly form = new FormGroup<ItemWriteFormControls>({
    resolvedName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    typeId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    level: new FormControl<number | null>(1, {
      validators: [Validators.required, Validators.min(1)]
    }),
    weight: new FormControl<number | null>(0, { validators: [Validators.min(0)] }),
    price: new FormControl<number | null>(0, { validators: [Validators.min(0)] }),
    iconId: new FormControl<number | null>(0, { validators: [Validators.min(0)] }),
    appearanceId: new FormControl<number | null>(0, { validators: [Validators.min(0)] }),
    setId: new FormControl<number | null>(null),
    conditions: new FormControl('', { nonNullable: true }),
    isVisible: new FormControl(true, { nonNullable: true }),
    usable: new FormControl(false, { nonNullable: true }),
    targetable: new FormControl(false, { nonNullable: true }),
    twoHanded: new FormControl(false, { nonNullable: true }),
    etheral: new FormControl(false, { nonNullable: true })
  });

  protected mode: ItemWriteMode = 'create';
  protected sourceItemId: number | null = null;
  protected sourceDetail: ItemDetailDto | null = null;
  protected typeOptions: AdminOptionDto[] = [];
  protected itemSetOptions: AdminOptionDto[] = [];
  protected previewState: ItemPreviewStateDto = createUnknownPreviewState();
  protected advisoryWarnings: AdminWarningLike[] = [];
  protected loadProblem: AdminApiProblem | null = null;
  protected saveProblem: AdminApiProblem | null = null;
  protected saveResult: ItemWriteResultDto | null = null;
  protected isLoading = false;
  protected isSaving = false;
  protected isLoadingPreview = false;

  ngOnInit(): void {
    this.form.controls.iconId.valueChanges
      .pipe(debounceTime(180), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.clearWriteFeedback();
        void this.refreshPreviewState();
      });

    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.clearWriteFeedback();
      this.advisoryWarnings = this.buildAdvisoryWarnings();
      this.refreshView();
    });

    this.activatedRoute.paramMap
      .pipe(
        switchMap((paramMap) => {
          const nextMode = this.readRouteMode();
          const nextSourceItemId = this.resolveSourceItemId(nextMode, paramMap.get('itemId'));

          this.ngZone.run(() => {
            this.mode = nextMode;
            this.sourceItemId = nextSourceItemId;
            this.sourceDetail = null;
            this.typeOptions = [];
            this.itemSetOptions = [];
            this.previewState = createUnknownPreviewState();
            this.advisoryWarnings = [];
            this.loadProblem = null;
            this.saveProblem = null;
            this.saveResult = null;
            this.isLoading = true;
            this.isLoadingPreview = false;
            this.previewRequestVersion += 1;
            this.refreshView();
          });

          if ((nextMode === 'edit' || nextMode === 'duplicate') && !nextSourceItemId) {
            this.ngZone.run(() => {
              this.loadProblem = {
                title: 'Invalid item id',
                detail: 'Edit and duplicate routes require a positive item id.',
                status: 400
              };
              this.isLoading = false;
              this.refreshView();
            });

            return of(null);
          }

          return this.loadBundle(nextSourceItemId).pipe(
            catchError((error: unknown) => {
              this.ngZone.run(() => {
                this.loadProblem = toAdminApiProblem(error);
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
      .subscribe((bundle) => {
        if (!bundle) {
          return;
        }

        this.ngZone.run(() => {
          this.typeOptions = bundle.typeOptions;
          this.itemSetOptions = bundle.itemSetOptions;
          this.sourceDetail = bundle.sourceDetail;
          this.applyFormState(bundle.sourceDetail);
        });

        void this.refreshPreviewState();
      });
  }

  protected get pageEyebrow(): string {
    switch (this.mode) {
      case 'edit':
        return 'Items Builder / Edit';
      case 'duplicate':
        return 'Items Builder / Duplicate';
      default:
        return 'Items Builder / Create';
    }
  }

  protected get pageTitle(): string {
    switch (this.mode) {
      case 'edit':
        return 'Edit item';
      case 'duplicate':
        return 'Duplicate item';
      default:
        return 'Create item';
    }
  }

  protected get pageDescription(): string {
    switch (this.mode) {
      case 'edit':
        return 'Update the supported sunshine.items fields while keeping ItemId fixed and previewing by IconId.';
      case 'duplicate':
        return 'Create a new item from an existing template without reusing the source ItemId or DescriptionId.';
      default:
        return 'Create a new non-weapon item row in sunshine.items with explicit identity fields and preview by IconId.';
    }
  }

  protected get submitLabel(): string {
    switch (this.mode) {
      case 'edit':
        return 'Save changes';
      case 'duplicate':
        return 'Duplicate item';
      default:
        return 'Create item';
    }
  }

  protected get resetLabel(): string {
    return this.mode === 'create' ? 'Clear form' : 'Reset to source';
  }

  protected get previewContextItemId(): number | null {
    return this.mode === 'edit' ? this.sourceItemId : null;
  }

  protected get previewIconId(): number | null {
    return this.form.controls.iconId.value ?? null;
  }

  protected get displayWarnings(): AdminWarningLike[] {
    return this.saveResult?.warnings?.length ? this.saveResult.warnings : this.advisoryWarnings;
  }

  protected get submitDisabled(): boolean {
    return this.isLoading || this.isSaving || this.typeOptions.length === 0;
  }

  protected submit(): void {
    this.hasTriedSubmit = true;
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      this.refreshView();
      return;
    }

    const request = this.toWriteRequest();
    const operation = this.createWriteOperation(request);
    if (!operation) {
      return;
    }

    this.isSaving = true;
    this.saveProblem = null;
    this.saveResult = null;
    this.refreshView();

    operation
      .pipe(
        takeUntilDestroyed(this.destroyRef),
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
        })
      )
      .subscribe((result) => {
        if (!result) {
          return;
        }

        this.ngZone.run(() => {
          this.saveResult = result;
          this.previewState = result.previewState;
          this.advisoryWarnings = result.warnings;
          this.refreshView();
        });
      });
  }

  protected resetForm(): void {
    this.clearWriteFeedback();
    this.applyFormState(this.sourceDetail);
    void this.refreshPreviewState();
  }

  protected resolveSetName(setId: number | null | undefined): string {
    if (!setId) {
      return 'No set';
    }

    const option = this.itemSetOptions.find((entry) => entry.value === setId);
    return option?.label || `Set #${setId}`;
  }

  protected getFieldErrors(fieldName: string): string[] {
    return this.saveProblem?.errors?.[fieldName] ?? [];
  }

  protected hasFieldIssue(fieldName: ItemWriteFieldName): boolean {
    return this.getFieldErrors(fieldName).length > 0 || !!this.getLocalError(fieldName);
  }

  protected getLocalError(fieldName: ItemWriteFieldName): string | null {
    const control = this.form.controls[fieldName];
    if (!this.shouldShowLocalError(control)) {
      return null;
    }

    if (control.hasError('required')) {
      switch (fieldName) {
        case 'resolvedName':
          return 'ResolvedName is required.';
        case 'typeId':
          return 'TypeId is required.';
        case 'level':
          return 'Level is required.';
        default:
          return 'This field is required.';
      }
    }

    if (control.hasError('min')) {
      switch (fieldName) {
        case 'level':
          return 'Level must be greater than or equal to 1.';
        case 'weight':
          return 'Weight must be greater than or equal to 0.';
        case 'price':
          return 'Price must be greater than or equal to 0.';
        case 'iconId':
          return 'IconId must be greater than or equal to 0.';
        case 'appearanceId':
          return 'AppearanceId must be greater than or equal to 0.';
        default:
          return 'This value is below the supported minimum.';
      }
    }

    return null;
  }

  private loadBundle(sourceItemId: number | null) {
    return forkJoin({
      sourceDetail: sourceItemId ? this.itemsFacade.getItem(sourceItemId) : of(null),
      typeOptions: this.itemsFacade.ensureTypeOptions(),
      itemSetOptions: this.itemsFacade.ensureItemSetOptions()
    });
  }

  private readRouteMode(): ItemWriteMode {
    const routeMode = this.activatedRoute.snapshot.data['writeMode'];

    if (routeMode === 'edit' || routeMode === 'duplicate') {
      return routeMode;
    }

    return 'create';
  }

  private resolveSourceItemId(mode: ItemWriteMode, itemIdValue: string | null): number | null {
    if (mode === 'create' || !itemIdValue) {
      return null;
    }

    const parsed = Number(itemIdValue);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }

  private applyFormState(sourceDetail: ItemDetailDto | null): void {
    const request = sourceDetail
      ? createItemWriteRequestFromDetail(sourceDetail)
      : createEmptyItemWriteRequest();

    this.form.reset(
      {
        resolvedName: request.resolvedName,
        description: request.description ?? '',
        typeId: request.typeId > 0 ? request.typeId : null,
        level: request.level,
        weight: request.weight,
        price: request.price,
        iconId: request.iconId,
        appearanceId: request.appearanceId,
        setId: request.setId ?? null,
        conditions: request.conditions ?? '',
        isVisible: request.isVisible ?? true,
        usable: request.usable,
        targetable: request.targetable,
        twoHanded: request.twoHanded,
        etheral: request.etheral
      },
      { emitEvent: false }
    );

    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.hasTriedSubmit = false;
    this.advisoryWarnings = this.buildAdvisoryWarnings();
    this.refreshView();
  }

  private toWriteRequest(): ItemWriteRequest {
    return normalizeItemWriteRequest({
      resolvedName: this.form.controls.resolvedName.value,
      description: this.form.controls.description.value,
      typeId: this.form.controls.typeId.value ?? 0,
      level: this.form.controls.level.value ?? 0,
      weight: this.form.controls.weight.value ?? 0,
      price: this.form.controls.price.value ?? 0,
      iconId: this.form.controls.iconId.value ?? 0,
      appearanceId: this.form.controls.appearanceId.value ?? 0,
      setId: this.form.controls.setId.value,
      conditions: this.form.controls.conditions.value,
      isVisible: this.form.controls.isVisible.value,
      usable: this.form.controls.usable.value,
      targetable: this.form.controls.targetable.value,
      twoHanded: this.form.controls.twoHanded.value,
      etheral: this.form.controls.etheral.value
    });
  }

  private createWriteOperation(request: ItemWriteRequest) {
    switch (this.mode) {
      case 'edit':
        return this.sourceItemId
          ? this.itemsFacade.updateItem(this.sourceItemId, request)
          : null;
      case 'duplicate':
        return this.sourceItemId
          ? this.itemsFacade.duplicateItem(this.sourceItemId, request)
          : null;
      default:
        return this.itemsFacade.createItem(request);
    }
  }

  private async refreshPreviewState(): Promise<void> {
    const iconId = this.previewIconId;
    const currentRequestVersion = ++this.previewRequestVersion;

    if (iconId === null || iconId === undefined || iconId < 0) {
      this.previewState = createUnknownPreviewState();
      this.advisoryWarnings = this.buildAdvisoryWarnings();
      this.refreshView();
      return;
    }

    if (iconId === 0) {
      this.previewState = createUnknownPreviewState();
      this.advisoryWarnings = this.buildAdvisoryWarnings();
      this.refreshView();
      return;
    }

    this.isLoadingPreview = true;
    this.refreshView();

    this.itemsFacade
      .getPreviewState(null, iconId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError((error: unknown) => {
          this.ngZone.run(() => {
            if (currentRequestVersion !== this.previewRequestVersion) {
              return;
            }

            const problem = toAdminApiProblem(error);
            this.previewState = createUnknownPreviewState();
            this.saveProblem = this.saveProblem ?? (problem.status && problem.status >= 500 ? problem : null);
            this.refreshView();
          });

          return of(createUnknownPreviewState());
        }),
        finalize(() => {
          this.ngZone.run(() => {
            if (currentRequestVersion !== this.previewRequestVersion) {
              return;
            }

            this.isLoadingPreview = false;
            this.refreshView();
          });
        })
      )
      .subscribe((previewState) => {
        this.ngZone.run(() => {
          if (currentRequestVersion !== this.previewRequestVersion) {
            return;
          }

          this.previewState = previewState;
          this.advisoryWarnings = this.buildAdvisoryWarnings();
          this.refreshView();
        });
      });
  }

  private buildAdvisoryWarnings(): AdminWarningLike[] {
    const request = this.toWriteRequest();
    const warnings: AdminWarningLike[] = [
      {
        code: 'IDENTITY_RULE_REMINDER',
        severity: 'info',
        message: 'ItemId, IconId, and AppearanceId stay separate. Preview is resolved by IconId in the write form.',
        field: null
      },
      {
        code: 'DESCRIPTION_NOT_PERSISTED',
        severity: 'warning',
        message: 'Description is accepted in the contract for future publish flow, but Phase 7 does not persist client text yet.',
        field: 'description'
      },
      {
        code: 'IS_VISIBLE_NOT_PERSISTED',
        severity: 'info',
        message: 'IsVisible is tracked in the form for future workflow alignment, but sunshine.items has no direct column for it yet.',
        field: 'isVisible'
      }
    ];

    if (!request.setId) {
      warnings.push({
        code: 'NO_ITEM_SET',
        severity: 'info',
        message: 'This item will be saved without an item set link.',
        field: 'setId'
      });
    }

    if (request.iconId <= 0) {
      warnings.push({
        code: 'ICON_ID_ZERO',
        severity: 'warning',
        message: 'IconId <= 0 means preview and client identity will remain weak until a valid client icon is assigned.',
        field: 'iconId'
      });
    }

    if (request.appearanceId <= 0) {
      warnings.push({
        code: 'APPEARANCE_ID_ZERO',
        severity: 'info',
        message: 'AppearanceId <= 0 is allowed, but equipped look identity will stay unresolved.',
        field: 'appearanceId'
      });
    }

    if (this.previewState.state === 'MISSING' || this.previewState.state === 'UNKNOWN') {
      warnings.push({
        code: 'PREVIEW_NOT_RESOLVED',
        severity: 'warning',
        message: 'No preview PNG is currently resolved for this IconId. Save is still allowed.',
        field: 'iconId'
      });
    }

    if (this.mode === 'duplicate' && this.sourceItemId) {
      warnings.push({
        code: 'DUPLICATE_ALLOCATES_NEW_IDENTITIES',
        severity: 'info',
        message: `Duplicating item #${this.sourceItemId} will allocate a new ItemId and a new DescriptionId on save.`,
        field: null
      });
    }

    if (this.mode === 'edit' && this.sourceItemId) {
      warnings.push({
        code: 'EDIT_KEEPS_ITEM_ID',
        severity: 'info',
        message: `Editing item #${this.sourceItemId} keeps the same ItemId. Preview is still evaluated by the current IconId in the form.`,
        field: null
      });
    }

    return warnings;
  }

  private clearWriteFeedback(): void {
    if (!this.saveProblem && !this.saveResult) {
      return;
    }

    this.saveProblem = null;
    this.saveResult = null;
  }

  private shouldShowLocalError(control: AbstractControl): boolean {
    return control.invalid && (control.touched || this.hasTriedSubmit);
  }

  private refreshView(): void {
    this.changeDetectorRef.detectChanges();
  }
}
