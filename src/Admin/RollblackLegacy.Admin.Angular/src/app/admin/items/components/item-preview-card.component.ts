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
        return 'Preview resolved from the current logical asset paths.';
      case 'MANUAL':
        return 'A manual asset is expected to represent this item preview.';
      case 'MISSING':
        return 'No preview asset was resolved for this item yet.';
      default:
        return 'Preview resolution is unavailable in the current environment.';
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
        return 'Manual asset';
      case 'BY_ITEM':
        return 'By item path';
      case 'BY_ICON':
        return 'By icon path';
      default:
        return 'Placeholder';
    }
  }

  protected get fallbackLabel(): string {
    switch ((this.previewState?.fallbackUsed || 'PLACEHOLDER').toUpperCase()) {
      case 'NONE':
        return 'None';
      case 'BY_ICON':
        return 'IconId fallback';
      default:
        return 'Placeholder fallback';
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
        return 'Copied';
      case 'manual':
      case 'unavailable':
        return 'Manual';
      default:
        return 'Copy';
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
    return value && value.trim().length > 0 ? value : 'Unavailable';
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
