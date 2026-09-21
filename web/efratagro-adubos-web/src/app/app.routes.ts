import {
  Routes,
} from '@angular/router';
import {
  authGuard,
} from './core/auth/auth.guard';
import {
  applicationRoles,
} from './core/auth/auth.models';
import {
  roleGuard,
} from './core/auth/role.guard';
import {
  CustomersComponent,
} from './features/customers/customers.component';
import {
  DeliveriesComponent,
} from './features/deliveries/deliveries.component';
import {
  FinanceComponent,
} from './features/finance/finance.component';
import {
  InventoryComponent,
} from './features/inventory/inventory.component';
import {
  LoginComponent,
} from './features/login/login.component';
import {
  PurchasesComponent,
} from './features/purchases/purchases.component';
import {
  SalesComponent,
} from './features/sales/sales.component';

import {
  UsersComponent,
} from './features/users/users.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    title: 'Entrar | EfratAgro',
  },
  {
    path: 'estoque',
    component: InventoryComponent,
    canActivate: [
      authGuard,
    ],
    title: 'Estoque | EfratAgro',
  },
  {
    path: 'vendas',
    component: SalesComponent,
    canActivate: [
      authGuard,
    ],
    title: 'Vendas | EfratAgro',
  },
  {
    path: 'compras',
    component: PurchasesComponent,
    canActivate: [
      authGuard,
      roleGuard,
    ],
    data: {
      roles: [
        applicationRoles.admin,
        applicationRoles.manager,
      ],
    },
    title: 'Compras | EfratAgro',
  },
  {
    path: 'clientes',
    component: CustomersComponent,
    canActivate: [
      authGuard,
    ],
    title: 'Clientes | EfratAgro',
  },
  {
    path: 'financeiro',
    component: FinanceComponent,
    canActivate: [
      authGuard,
      roleGuard,
    ],
    data: {
      roles: [
        applicationRoles.admin,
        applicationRoles.manager,
      ],
    },
    title: 'Financeiro | EfratAgro',
  },
  {
    path: 'entregas',
    component: DeliveriesComponent,
    canActivate: [
      authGuard,
    ],
    title: 'Entregas | EfratAgro',
  },
  {
    path: 'usuarios',
    component: UsersComponent,
    canActivate: [
      authGuard,
      roleGuard,
    ],
    data: {
      roles: [
        applicationRoles.admin,
      ],
    },
    title: 'Usuários | EfratAgro',
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
