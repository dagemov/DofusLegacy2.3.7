import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-client-identity-recommended-action',
  imports: [CommonModule],
  templateUrl: './client-identity-recommended-action.component.html',
  styleUrl: './client-identity-recommended-action.component.scss'
})
export class ClientIdentityRecommendedActionComponent {
  @Input() action: string | null = null;
  @Input() needsClientPatch = false;

  protected get alertClass(): string {
    return this.needsClientPatch ? 'alert-warning' : 'alert-success';
  }
}
