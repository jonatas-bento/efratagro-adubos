import {
  Routes,
} from '@angular/router';

import {
  InventoryComponent,
} from './features/inventory/inventory.component';
import {
  PurchasesComponent,
} from './features/purchases/purchases.component';
import {
  SalesComponent,
} from './features/sales/sales.component';

export const routes: Routes = [
  {
    path: 'estoque',
    component: InventoryComponent,
    title: 'Estoque | EfratAgro',
  },
  {
    path: 'vendas',
    component: SalesComponent,
    title: 'Vendas | EfratAgro',
  },
  {
    path: 'compras',
    component: PurchasesComponent,
    title: 'Compras | EfratAgro',
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
