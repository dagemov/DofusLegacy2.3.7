import { CommonModule, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';

import { ItemClientIdentityDto } from '../data-access/items.models';
import { ClipboardCopyStatus, copyTextToClipboard, getClipboardSupportInfo } from '../../../shared/utils/copy-text';

@Component({
  selector: 'app-item-client-identity-card',
  imports: [CommonModule, PercentPipe],
  templateUrl: './item-client-identity-card.component.html',
  styleUrl: './item-client-identity-card.component.scss'
})
export class ItemClientIdentityCardComponent {
  @Input() routeItemId: number | null = null;
  @Input() identity: ItemClientIdentityDto | null = null;

  protected readonly clipboardSupport = getClipboardSupportInfo();
  protected readonly copyState = new Map<string, ClipboardCopyStatus>();

  protected async copyField(field: string, value: number | string | null | undefined): Promise<void> {
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

  protected displayValue(value: number | string | null | undefined, fallback = 'Unavailable'): string {
    return value === null || value === undefined || value === '' ? fallback : `${value}`;
  }

  protected isCopyDisabled(value: number | string | null | undefined): boolean {
    return !this.asCopyValue(value);
  }

  protected get clipboardHint(): string | null {
    return this.clipboardSupport.helpText;
  }

  private asCopyValue(value: number | string | null | undefined): string | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    return `${value}`;
  }
}
