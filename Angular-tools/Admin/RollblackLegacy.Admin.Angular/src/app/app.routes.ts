import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'admin/items'
  },
  {
    path: 'admin/publication',
    loadComponent: () =>
      import('./admin/publication/publication-dashboard-page.component').then(
        (m) => m.PublicationDashboardPageComponent
      )
  },
  {
    path: 'admin/items',
    loadComponent: () =>
      import('./admin/items/items-page.component').then((m) => m.ItemsPageComponent)
  },
  {
    path: 'admin/items/icon-selector',
    loadComponent: () =>
      import('./admin/items/item-icon-selector.component').then(
        (m) => m.ItemIconSelectorComponent
      )
  },
  {
    path: 'admin/items/new',
    data: {
      writeMode: 'create'
    },
    loadComponent: () =>
      import('./admin/items/item-write-page.component').then((m) => m.ItemWritePageComponent)
  },
  {
    path: 'admin/items/:itemId/edit',
    data: {
      writeMode: 'edit'
    },
    loadComponent: () =>
      import('./admin/items/item-write-page.component').then((m) => m.ItemWritePageComponent)
  },
  {
    path: 'admin/items/:itemId/duplicate',
    data: {
      writeMode: 'duplicate'
    },
    loadComponent: () =>
      import('./admin/items/item-write-page.component').then((m) => m.ItemWritePageComponent)
  },
  {
    path: 'admin/items/:itemId/publication-status',
    loadComponent: () =>
      import('./admin/items/item-publication-status-page.component').then(
        (m) => m.ItemPublicationStatusPageComponent
      )
  },
  {
    path: 'admin/items/:itemId',
    loadComponent: () =>
      import('./admin/items/item-detail-page.component').then((m) => m.ItemDetailPageComponent)
  },
  {
    path: '**',
    redirectTo: 'admin/items'
  }
];
