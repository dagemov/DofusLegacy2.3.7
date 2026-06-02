import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-item-warning-badge',
  imports: [CommonModule],
  templateUrl: './item-warning-badge.component.html',
  styleUrl: './item-warning-badge.component.scss'
})
export class ItemWarningBadgeComponent {
  @Input() severity: string | null | undefined = 'info';

  protected get badgeClass(): string {
    switch ((this.severity || 'info').toLowerCase()) {
      case 'error':
        return 'warning-badge--error';
      case 'warning':
        return 'warning-badge--warning';
      default:
        return 'warning-badge--info';
    }
  }

  protected get label(): string {
    switch ((this.severity || 'info').toLowerCase()) {
      case 'warning':
        return 'ALERTA';
      case 'error':
        return 'ERROR';
      default:
        return 'INFO';
    }
  }
}
