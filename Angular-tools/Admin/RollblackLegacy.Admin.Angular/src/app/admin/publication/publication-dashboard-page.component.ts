import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ApiProblemPanelComponent } from '../../shared/components/api-problem-panel.component';
import { AdminApiProblem } from '../items/data-access/items.models';
import { PublicationApi } from './data-access/publication.api';
import { PublicationBackupStatusDto } from './data-access/publication.models';

@Component({
  selector: 'app-publication-dashboard-page',
  imports: [CommonModule, RouterLink, ApiProblemPanelComponent],
  templateUrl: './publication-dashboard-page.component.html',
  styleUrl: './publication-dashboard-page.component.scss'
})
export class PublicationDashboardPageComponent implements OnInit {
  private readonly publicationApi = inject(PublicationApi);

  protected status: PublicationBackupStatusDto | null = null;
  protected isLoading = true;
  protected problem: AdminApiProblem | null = null;

  ngOnInit(): void {
    this.loadStatus();
  }

  protected loadStatus(): void {
    this.isLoading = true;
    this.problem = null;
    this.publicationApi.getBackupStatus().subscribe({
      next: (status) => {
        this.status = status;
        this.isLoading = false;
      },
      error: (error) => {
        this.problem = error?.error ?? { title: 'Error al cargar backup-status', status: error?.status };
        this.isLoading = false;
      }
    });
  }

  protected get laneBadgeClass(): string {
    switch ((this.status?.publishLaneStatus || '').toUpperCase()) {
      case 'READY':
        return 'text-bg-success';
      case 'NEEDS_BACKUP':
      case 'NEEDS_VALIDATION':
        return 'text-bg-warning';
      default:
        return 'text-bg-danger';
    }
  }
}
