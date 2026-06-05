import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

import { ItemAppearancePreviewStateDto } from '../data-access/items.models';
import { ClipboardCopyStatus, copyTextToClipboard, getClipboardSupportInfo } from '../../../shared/utils/copy-text';

@Component({
  selector: 'app-item-appearance-preview-card',
  imports: [CommonModule],
  templateUrl: './item-appearance-preview-card.component.html',
  styleUrl: './item-appearance-preview-card.component.scss'
})
export class ItemAppearancePreviewCardComponent implements OnChanges {
  @Input() appearancePreviewState: ItemAppearancePreviewStateDto | null = null;
  @Input() routeItemId: number | null = null;

  protected readonly clipboardSupport = getClipboardSupportInfo();
  protected readonly copyState = new Map<string, ClipboardCopyStatus>();
  protected imageLoadFailed = false;

  ngOnChanges(_changes: SimpleChanges): void {
    this.imageLoadFailed = false;
  }

  protected get resolvedState(): string {
    return (this.appearancePreviewState?.state || 'UNKNOWN').toUpperCase();
  }

  protected get badgeClass(): string {
    switch (this.resolvedState) {
      case 'CURATED_BY_APPEARANCE':
        return 'state-badge--curated';
      case 'MISSING':
        return 'state-badge--missing';
      case 'NOT_APPLICABLE':
        return 'state-badge--na';
      default:
        return 'state-badge--unknown';
    }
  }

  protected get previewMessage(): string {
    switch (this.resolvedState) {
      case 'CURATED_BY_APPEARANCE':
        return 'Preview curado de equipamiento disponible para este AppearanceId.';
      case 'MISSING':
        return 'El cliente reconoce o espera esta apariencia, pero falta el PNG curado by-appearance.';
      case 'NOT_APPLICABLE':
        return 'AppearanceId en cero: no aplica preview de equipamiento.';
      case 'UNKNOWN':
        return 'Appearance desconocido para el cliente actual o sin validacion Appearances.d2o.';
      default:
        return 'Estado de appearance preview no disponible.';
    }
  }

  protected get canRenderImage(): boolean {
    return this.resolvedState === 'CURATED_BY_APPEARANCE' && !!this.previewImagePath && !this.imageLoadFailed;
  }

  protected get previewImagePath(): string | null {
    return this.appearancePreviewState?.resolvedPath?.trim() ? this.appearancePreviewState.resolvedPath : null;
  }

  protected get appearanceKnownLabel(): string {
    const known = this.appearancePreviewState?.appearanceKnown;
    if (known === true) {
      return 'Si (Appearances.d2o)';
    }

    if (known === false) {
      return 'No (APPEARANCE_UNKNOWN)';
    }

    return 'N/A';
  }

  protected displayPath(path?: string | null): string {
    return path?.trim() ? path : '(sin ruta)';
  }

  protected onImageError(): void {
    this.imageLoadFailed = true;
  }

  protected isCopyDisabled(path?: string | null): boolean {
    return !path?.trim();
  }

  protected async copyField(field: string, path?: string | null): Promise<void> {
    const normalizedValue = path?.trim();
    if (!normalizedValue) {
      return;
    }

    const status = await copyTextToClipboard(normalizedValue);
    this.copyState.set(field, status);

    if (status === 'copied') {
      window.setTimeout(() => {
        if (this.copyState.get(field) === 'copied') {
          this.copyState.delete(field);
        }
      }, 1800);
    }
  }

  protected copyLabel(field: string): string {
    switch (this.copyState.get(field)) {
      case 'copied':
        return 'Copiado';
      case 'manual':
      case 'unavailable':
        return 'Manual';
      default:
        return 'Copiar';
    }
  }

  protected shouldShowManualValue(field: string): boolean {
    const status = this.copyState.get(field);
    return status === 'manual' || status === 'unavailable';
  }

  protected get clipboardHint(): string | null {
    return this.clipboardSupport.helpText;
  }
}
