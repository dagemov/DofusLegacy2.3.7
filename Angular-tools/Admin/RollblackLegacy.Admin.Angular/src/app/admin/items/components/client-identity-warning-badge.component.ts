import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

import { presentClientIdentityStatus } from '../data-access/client-identity.status';

@Component({
  selector: 'app-client-identity-warning-badge',
  imports: [CommonModule],
  template: `
    <span class="badge" [class]="badgeClass">{{ label }}</span>
  `
})
export class ClientIdentityWarningBadgeComponent {
  @Input({ required: true }) statusCode!: string;

  protected get label(): string {
    return presentClientIdentityStatus(this.statusCode).label;
  }

  protected get badgeClass(): string {
    return presentClientIdentityStatus(this.statusCode).badgeClass;
  }
}
