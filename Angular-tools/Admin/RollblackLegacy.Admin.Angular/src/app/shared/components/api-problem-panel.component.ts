import { CommonModule, KeyValuePipe } from '@angular/common';
import { Component, Input } from '@angular/core';

import { AdminApiProblem } from '../../admin/items/data-access/items.models';

@Component({
  selector: 'app-api-problem-panel',
  imports: [CommonModule, KeyValuePipe],
  template: `
    <div *ngIf="problem" class="alert alert-danger border-0 shadow-sm" role="alert">
      <div class="d-flex flex-column flex-lg-row gap-2 justify-content-between align-items-lg-start">
        <div>
          <h2 class="h6 mb-1">{{ problem.title || 'No se pudo completar la solicitud al Admin API.' }}</h2>
          <p *ngIf="displayDetail" class="mb-2">{{ displayDetail }}</p>
        </div>
        <span *ngIf="problem.status" class="badge text-bg-danger">HTTP {{ problem.status }}</span>
      </div>

      <p *ngIf="problem.traceId" class="mb-2 small text-break">
        <strong>traceId:</strong>
        <span class="font-monospace-soft">{{ problem.traceId }}</span>
      </p>

      <div *ngIf="problem.errors && hasErrors(problem.errors)" class="small">
        <p class="fw-semibold mb-2">Detalle de validación</p>
        <ul class="mb-0 ps-3">
          <li *ngFor="let entry of problem.errors | keyvalue">
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

  protected get displayDetail(): string | null {
    const title = this.problem?.title?.trim();
    const detail = this.problem?.detail?.trim();

    if (!detail) {
      return null;
    }

    if (title && detail === title) {
      return null;
    }

    return detail;
  }

  protected hasErrors(errors: Record<string, string[]> | null | undefined): boolean {
    return !!errors && Object.keys(errors).length > 0;
  }
}
