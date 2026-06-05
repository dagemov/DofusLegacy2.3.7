import { CommonModule, KeyValuePipe } from '@angular/common';
import { Component, Input } from '@angular/core';

import {
  AdminApiProblem,
  AdminFeedback
} from '../../admin/items/data-access/items.models';

@Component({
  selector: 'app-api-problem-panel',
  imports: [CommonModule, KeyValuePipe],
  template: `
    <div
      *ngIf="resolvedFeedback"
      class="alert border-0 shadow-sm mb-0"
      [class.alert-danger]="resolvedFeedback.kind === 'error'"
      [class.alert-success]="resolvedFeedback.kind === 'success'"
      role="alert">
      <div class="d-flex flex-column flex-lg-row gap-2 justify-content-between align-items-lg-start">
        <div>
          <p class="text-uppercase small fw-semibold mb-1" [class.text-danger-emphasis]="resolvedFeedback.kind === 'error'" [class.text-success-emphasis]="resolvedFeedback.kind === 'success'">
            {{ feedbackEyebrow }}
          </p>
          <h2 class="h6 mb-1">{{ resolvedFeedback.title }}</h2>
          <p *ngIf="displayDetail" class="mb-2">{{ displayDetail }}</p>
        </div>
        <span
          *ngIf="statusBadgeLabel"
          class="badge"
          [class.text-bg-danger]="resolvedFeedback.kind === 'error'"
          [class.text-bg-success]="resolvedFeedback.kind === 'success'">
          {{ statusBadgeLabel }}
        </span>
      </div>

      <p *ngIf="resolvedFeedback.traceId" class="mb-2 small text-break">
        <strong>traceId:</strong>
        <span class="font-monospace-soft">{{ resolvedFeedback.traceId }}</span>
      </p>

      <div *ngIf="resolvedFeedback.errors && hasErrors(resolvedFeedback.errors)" class="small">
        <p class="fw-semibold mb-2">Detalle de validacion</p>
        <ul class="mb-0 ps-3">
          <li *ngFor="let entry of resolvedFeedback.errors | keyvalue">
            <span class="font-monospace-soft">{{ entry.key }}</span>
            <span>: {{ entry.value.join(', ') }}</span>
          </li>
        </ul>
      </div>
    </div>
  `
})
export class ApiProblemPanelComponent {
  @Input() problem: AdminApiProblem | null = null;
  @Input() feedback: AdminFeedback | null = null;

  protected get resolvedFeedback(): AdminFeedback | null {
    if (this.feedback) {
      return this.feedback;
    }

    if (!this.problem) {
      return null;
    }

    return {
      kind: 'error',
      title: this.problem.title?.trim() || 'No se pudo completar la solicitud al Admin API.',
      detail: this.problem.detail?.trim() || null,
      status: this.problem.status ?? null,
      traceId: this.problem.traceId?.trim() || null,
      errors: this.problem.errors
    };
  }

  protected get displayDetail(): string | null {
    const title = this.resolvedFeedback?.title?.trim();
    const detail = this.resolvedFeedback?.detail?.trim();

    if (!detail) {
      return null;
    }

    if (title && detail === title) {
      return null;
    }

    return detail;
  }

  protected get feedbackEyebrow(): string {
    if (this.resolvedFeedback?.kind === 'success') {
      return 'Success';
    }

    const status = this.resolvedFeedback?.status ?? null;
    switch (status) {
      case 409:
        return '409 / Conflicto';
      case 422:
        return '422 / Validacion';
      case 500:
        return '500 / Error del servidor';
      default:
        return 'Error';
    }
  }

  protected get statusBadgeLabel(): string | null {
    if (!this.resolvedFeedback) {
      return null;
    }

    if (this.resolvedFeedback.kind === 'success') {
      return 'SUCCESS';
    }

    if (!this.resolvedFeedback.status) {
      return 'ERROR';
    }

    return `HTTP ${this.resolvedFeedback.status}`;
  }

  protected hasErrors(errors: Record<string, string[]> | null | undefined): boolean {
    return !!errors && Object.keys(errors).length > 0;
  }
}
