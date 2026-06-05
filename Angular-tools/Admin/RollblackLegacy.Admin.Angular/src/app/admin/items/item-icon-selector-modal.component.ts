import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { ItemIconSelectorComponent } from './item-icon-selector.component';
import { ItemIconSelection } from './data-access/items.models';

@Component({
  selector: 'app-item-icon-selector-modal',
  imports: [CommonModule, ItemIconSelectorComponent],
  templateUrl: './item-icon-selector-modal.component.html',
  styleUrl: './item-icon-selector-modal.component.scss'
})
export class ItemIconSelectorModalComponent {
  @Input() isOpen = false;
  @Input() initialIconId: number | null = null;

  @Output() readonly cancelled = new EventEmitter<void>();
  @Output() readonly selected = new EventEmitter<ItemIconSelection>();

  protected closeModal(): void {
    this.cancelled.emit();
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeModal();
    }
  }

  protected applySelection(selection: ItemIconSelection): void {
    this.selected.emit(selection);
  }
}
