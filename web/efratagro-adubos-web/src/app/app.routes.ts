import { Routes } from '@angular/router';

import {
  InventoryComponent,
} from './features/inventory/inventory.component';

export const routes: Routes = [
  {
    path: 'estoque',
    component: InventoryComponent,
    title: 'Estoque | EfratAgro',
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'estoque',
  },
  {
    path: '**',
    redirectTo: 'estoque',
  },
];
