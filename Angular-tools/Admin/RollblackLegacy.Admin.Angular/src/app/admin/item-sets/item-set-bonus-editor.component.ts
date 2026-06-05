import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  AdminEffectOptionDto,
  ItemSetBonusEffectWriteDto,
  ItemSetBonusTierWriteDto
} from '../items/data-access/items.models';

const DEFAULT_PIECE_COUNTS = [2, 3, 4, 5];

@Component({
  selector: 'app-item-set-bonus-editor',
  imports: [CommonModule, FormsModule],
  templateUrl: './item-set-bonus-editor.component.html',
  styleUrl: './item-set-bonus-editor.component.scss'
})
export class ItemSetBonusEditorComponent implements OnChanges {
  @Input({ required: true }) effectOptions: AdminEffectOptionDto[] = [];
  @Input({ required: true }) tiers: ItemSetBonusTierWriteDto[] = [];
  @Input() memberCount = 0;

  protected readonly defaultPieceCounts = DEFAULT_PIECE_COUNTS;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['tiers'] && this.tiers.length === 0) {
      for (const pieceCount of DEFAULT_PIECE_COUNTS) {
        this.tiers.push({ pieceCount, effects: [] });
      }
    }
  }

  protected tierLabel(pieceCount: number, index: number): string {
    if (this.memberCount > 0 && pieceCount >= this.memberCount && index === this.tiers.length - 1) {
      return 'Set completo';
    }

    return `${pieceCount} piezas`;
  }

  protected addTier(): void {
    const nextPieceCount = (this.tiers[this.tiers.length - 1]?.pieceCount ?? 1) + 1;
    this.tiers.push({ pieceCount: nextPieceCount, effects: [] });
  }

  protected removeTier(index: number): void {
    this.tiers.splice(index, 1);
  }

  protected addEffect(tier: ItemSetBonusTierWriteDto): void {
    tier.effects.push({
      effectId: this.effectOptions[0]?.effectId ?? 0,
      value: 10,
      format: 'Integer'
    });
  }

  protected removeEffect(tier: ItemSetBonusTierWriteDto, index: number): void {
    tier.effects.splice(index, 1);
  }

  protected onEffectChanged(effect: ItemSetBonusEffectWriteDto): void {
    const option = this.effectOptions.find((entry) => entry.effectId === effect.effectId);
    if (!option) {
      return;
    }

    if (option.defaultSerializationTypeId === 73) {
      effect.format = 'Dice';
      effect.diceNum ??= 1;
      effect.diceSide ??= effect.value;
    } else {
      effect.format = 'Integer';
      effect.diceNum = null;
      effect.diceSide = null;
    }
  }
}
