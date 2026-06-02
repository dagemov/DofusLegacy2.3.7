import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'admin/items'
  },
  {
    path: 'admin/items',
    loadComponent: () =>
      import('./admin/items/items-page.component').then((m) => m.ItemsPageComponent)
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
    path: 'admin/items/:itemId',
    loadComponent: () =>
      import('./admin/items/item-detail-page.component').then((m) => m.ItemDetailPageComponent)
  },
  {
    path: '**',
    redirectTo: 'admin/items'
  }
];
