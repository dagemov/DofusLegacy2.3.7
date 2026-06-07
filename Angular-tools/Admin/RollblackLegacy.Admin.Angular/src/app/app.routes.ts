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
    path: 'admin/item-sets',
    loadComponent: () =>
      import('./admin/item-sets/item-sets-page.component').then((m) => m.ItemSetsPageComponent)
  },
  {
    path: 'admin/item-sets/:setId',
    loadComponent: () =>
      import('./admin/item-sets/item-set-detail-page.component').then((m) => m.ItemSetDetailPageComponent)
  },
  {
    path: 'admin/spells',
    loadComponent: () =>
      import('./admin/spells/spells-page.component').then((m) => m.SpellsPageComponent)
  },
  {
    path: 'admin/spells/:spellId',
    loadComponent: () =>
      import('./admin/spells/spell-detail-page.component').then((m) => m.SpellDetailPageComponent)
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
