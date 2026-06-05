import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

import { ItemPreviewStateDto } from '../data-access/items.models';
import { ClipboardCopyStatus, copyTextToClipboard, getClipboardSupportInfo } from '../../../shared/utils/copy-text';

@Component({
  selector: 'app-item-preview-card',
  imports: [CommonModule],
  templateUrl: './item-preview-card.component.html',
  styleUrl: './item-preview-card.component.scss'
})
export class ItemPreviewCardComponent implements OnChanges {
  @Input() previewState: ItemPreviewStateDto | null = null;
  @Input() routeItemId: number | null = null;
  @Input() iconId: number | null = null;

  protected readonly clipboardSupport = getClipboardSupportInfo();
  protected readonly copyState = new Map<string, ClipboardCopyStatus>();
  protected imageLoadFailed = false;

  ngOnChanges(_changes: SimpleChanges): void {
    this.imageLoadFailed = false;
  }

  protected get resolvedState(): string {
    return (this.previewState?.state || 'UNKNOWN').toUpperCase();
  }

  protected get badgeClass(): string {
    switch (this.resolvedState) {
      case 'FOUND':
        return 'state-badge--found';
      case 'MANUAL':
        return 'state-badge--manual';
      case 'MISSING':
        return 'state-badge--missing';
      default:
        return 'state-badge--unknown';
    }
  }

  protected get previewMessage(): string {
    switch (this.resolvedState) {
      case 'FOUND':
        return 'El preview se resolvió desde las rutas lógicas actuales.';
      case 'MANUAL':
        return 'Se espera un asset manual para representar este preview.';
      case 'MISSING':
        return 'Todavía no se resolvió ningún preview para este item.';
      default:
        return 'La resolución del preview no está disponible en el entorno actual.';
    }
  }

  protected get canRenderImage(): boolean {
    return !!this.previewImagePath && (this.resolvedState === 'FOUND' || this.resolvedState === 'MANUAL');
  }

  protected get previewImagePath(): string | null {
    return this.previewState?.resolvedPath?.trim() ? this.previewState.resolvedPath : null;
  }

  protected get previewSourceLabel(): string {
    switch ((this.previewState?.previewSource || 'PLACEHOLDER').toUpperCase()) {
      case 'MANUAL':
        return 'Asset manual';
      case 'BY_ITEM':
        return 'Ruta por item';
      case 'BY_ICON':
        return 'Ruta por icono';
      case 'BY_CATEGORY':
        return 'Catálogo por categoría';
      default:
        return 'Placeholder';
    }
  }

  protected get fallbackLabel(): string {
    switch ((this.previewState?.fallbackUsed || 'PLACEHOLDER').toUpperCase()) {
      case 'NONE':
        return 'Ninguno';
      case 'BY_ICON':
        return 'Fallback por IconId';
      case 'BY_CATEGORY':
        return 'Fallback por categoría';
      default:
        return 'Fallback placeholder';
    }
  }

  protected async copyField(field: string, value: string | null | undefined): Promise<void> {
    const normalizedValue = this.asCopyValue(value);
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

  protected isCopyDisabled(value: string | null | undefined): boolean {
    return !this.asCopyValue(value);
  }

  protected displayPath(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'No disponible';
  }

  protected onImageError(): void {
    this.imageLoadFailed = true;
  }

  protected get clipboardHint(): string | null {
    return this.clipboardSupport.helpText;
  }

  private asCopyValue(value: string | null | undefined): string | null {
    if (!value || value.trim().length === 0) {
      return null;
    }

    return value;
  }
}
