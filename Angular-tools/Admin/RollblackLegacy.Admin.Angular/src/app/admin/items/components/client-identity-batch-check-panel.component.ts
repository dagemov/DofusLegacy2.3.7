import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Input, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { ApiProblemPanelComponent } from '../../../shared/components/api-problem-panel.component';
import { ClientIdentityApi } from '../data-access/client-identity.api';
import { ClientItemIdentityCheckResultDto } from '../data-access/client-identity.models';
import { presentClientIdentityStatus } from '../data-access/client-identity.status';
import { AdminApiProblem, toAdminApiProblem } from '../data-access/items.models';

const QA_SAMPLE_IDS = [7754, 12616, 12617, 39] as const;

@Component({
  selector: 'app-client-identity-batch-check-panel',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './client-identity-batch-check-panel.component.html',
  styleUrl: './client-identity-batch-check-panel.component.scss'
})
export class ClientIdentityBatchCheckPanelComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly clientIdentityApi = inject(ClientIdentityApi);

  @Input() collapsedByDefault = true;

  protected expanded = false;
  protected isLoading = false;
  protected results: ClientItemIdentityCheckResultDto[] = [];
  protected problem: AdminApiProblem | null = null;

  protected readonly sampleIds = [...QA_SAMPLE_IDS];

  ngOnInit(): void {
    this.expanded = !this.collapsedByDefault;
  }

  protected toggle(): void {
    this.expanded = !this.expanded;
  }

  protected loadSample(): void {
    this.isLoading = true;
    this.problem = null;
    this.results = [];

    this.clientIdentityApi
      .checkItems(QA_SAMPLE_IDS)
      .pipe(
        catchError((error: unknown) => {
          this.problem = toAdminApiProblem(error);
          return of(null);
        }),
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((results) => {
        if (!results) {
          return;
        }

        this.results = results;
      });
  }

  protected statusLabel(code: string): string {
    return presentClientIdentityStatus(code).label;
  }

  protected statusBadgeClass(code: string): string {
    return presentClientIdentityStatus(code).badgeClass;
  }
}
