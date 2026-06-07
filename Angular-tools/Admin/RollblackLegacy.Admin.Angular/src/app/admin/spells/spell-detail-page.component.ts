import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { AdminApiProblem } from './data-access/spells.models';

@Component({
  selector: 'app-spell-detail-page',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './spell-detail-page.component.html',
  styleUrl: './spell-detail-page.component.scss'
})
export class SpellDetailPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly activatedRoute = inject(ActivatedRoute);

  protected spellId: number | null = null;
  protected problem: AdminApiProblem | null = null;

  ngOnInit(): void {
    this.activatedRoute.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((paramMap) => {
      const spellId = Number(paramMap.get('spellId'));
      if (!Number.isInteger(spellId) || spellId <= 0) {
        this.problem = {
          title: 'SpellId inválido',
          detail: 'El identificador del spell no es válido.',
          status: 400
        };
        this.spellId = null;
        return;
      }

      this.problem = null;
      this.spellId = spellId;
    });
  }
}
