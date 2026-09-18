import {
  Routes,
} from '@angular/router';

import {
  DeliveriesComponent,
} from './features/deliveries/deliveries.component';
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
    path: 'entregas',
    component: DeliveriesComponent,
    title: 'Entregas | EfratAgro',
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
