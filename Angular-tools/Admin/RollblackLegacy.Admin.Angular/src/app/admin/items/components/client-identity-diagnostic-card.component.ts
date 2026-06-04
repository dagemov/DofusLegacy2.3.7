import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ClientItemIdentityCheckResultDto } from '../data-access/client-identity.models';
import {
  presentClientIdentityStatus,
  resolveCardAccentClass
} from '../data-access/client-identity.status';
import { ClientIdentityRecommendedActionComponent } from './client-identity-recommended-action.component';

@Component({
  selector: 'app-client-identity-diagnostic-card',
  imports: [CommonModule, RouterLink, ClientIdentityRecommendedActionComponent],
  templateUrl: './client-identity-diagnostic-card.component.html',
  styleUrl: './client-identity-diagnostic-card.component.scss'
})
export class ClientIdentityDiagnosticCardComponent {
  @Input() routeItemId: number | null = null;
  @Input() diagnostic: ClientItemIdentityCheckResultDto | null = null;
  @Input() isLoading = false;
  @Input() showPublicationLink = true;

  protected presentStatus(code: string) {
    return presentClientIdentityStatus(code);
  }

  protected get cardAccentClass(): string {
    return resolveCardAccentClass(this.diagnostic);
  }

  protected get primaryStatusLabel(): string {
    if (!this.diagnostic) {
      return 'Sin diagnóstico';
    }

    return presentClientIdentityStatus(this.diagnostic.status.primaryStatus).label;
  }

  protected get primaryBadgeClass(): string {
    if (!this.diagnostic) {
      return 'text-bg-secondary';
    }

    return presentClientIdentityStatus(this.diagnostic.status.primaryStatus).badgeClass;
  }

  protected i18nLabel(exists: boolean, language: string): string {
    return exists ? `i18n ${language} OK` : `i18n ${language} faltante`;
  }

  protected i18nBadgeClass(exists: boolean): string {
    return exists ? 'text-bg-success' : 'text-bg-info';
  }

  protected displayId(value: number | null | undefined, fallback = '—'): string {
    return value === null || value === undefined ? fallback : `${value}`;
  }
}
