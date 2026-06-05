import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

import { AdminWarningLike } from '../data-access/items.models';
import { ItemWarningBadgeComponent } from './item-warning-badge.component';

@Component({
  selector: 'app-item-diagnostic-panel',
  imports: [CommonModule, ItemWarningBadgeComponent],
  templateUrl: './item-diagnostic-panel.component.html',
  styleUrl: './item-diagnostic-panel.component.scss'
})
export class ItemDiagnosticPanelComponent {
  @Input() warnings: AdminWarningLike[] | null = [];

  protected get resolvedWarnings(): AdminWarningLike[] {
    return this.warnings ?? [];
  }

  protected get hasWarnings(): boolean {
    return this.resolvedWarnings.length > 0;
  }

  protected trackByWarningCode(_index: number, warning: AdminWarningLike): string {
    return `${warning.code}-${warning.field ?? 'none'}`;
  }

  protected warningCardClass(severity: string | undefined): string {
    switch ((severity || 'info').toLowerCase()) {
      case 'error':
        return 'diagnostic-entry diagnostic-entry--error';
      case 'warning':
        return 'diagnostic-entry diagnostic-entry--warning';
      default:
        return 'diagnostic-entry diagnostic-entry--info';
    }
  }
}
