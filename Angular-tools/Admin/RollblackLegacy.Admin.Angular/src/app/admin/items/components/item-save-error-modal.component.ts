import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { AdminApiProblem } from '../data-access/items.models';

@Component({
  selector: 'app-item-save-error-modal',
  imports: [CommonModule],
  templateUrl: './item-save-error-modal.component.html',
  styleUrl: './item-save-error-modal.component.scss'
})
export class ItemSaveErrorModalComponent {
  @Input({ required: true }) problem!: AdminApiProblem;
  @Input() isOpen = false;
  @Output() readonly closed = new EventEmitter<void>();

  protected showTechnicalDetails = false;

  protected get whatHappened(): string {
    return this.problem.title || 'No se pudo guardar el item';
  }

  protected get whatToFix(): string[] {
    const fixes: string[] = [];
    if (this.problem.detail) {
      fixes.push(this.problem.detail);
    }

    if (this.problem.errors) {
      for (const [field, messages] of Object.entries(this.problem.errors)) {
        for (const message of messages) {
          fixes.push(`${field}: ${message}`);
        }
      }
    }

    if (fixes.length === 0) {
      fixes.push('Revisa nombre, tipo, nivel e IconId. Si el error persiste, copia el TraceId para soporte.');
    }

    return fixes;
  }

  protected get affectedFields(): string[] {
    if (!this.problem.errors) {
      return [];
    }

    return Object.keys(this.problem.errors);
  }

  protected close(): void {
    this.closed.emit();
  }

  protected toggleTechnical(): void {
    this.showTechnicalDetails = !this.showTechnicalDetails;
  }
}
