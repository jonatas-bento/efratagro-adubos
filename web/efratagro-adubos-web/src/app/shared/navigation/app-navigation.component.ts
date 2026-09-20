import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import {
  Router,
  RouterLink,
  RouterLinkActive,
} from '@angular/router';
import {
  applicationRoles,
} from '../../core/auth/auth.models';
import {
  AuthService,
} from '../../core/auth/auth.service';

interface NavigationItem {
  label: string;
  route: string;
  roles?: string[];
}

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl:
    './app-navigation.component.html',
  styleUrl:
    './app-navigation.component.scss',
  changeDetection:
    ChangeDetectionStrategy.OnPush,
})
export class AppNavigationComponent {
  readonly auth =
    inject(AuthService);

  private readonly router =
    inject(Router);

  private readonly items:
    NavigationItem[] = [
      {
        label: 'Estoque',
        route: '/estoque',
      },
      {
        label: 'Vendas',
        route: '/vendas',
      },
      {
        label: 'Compras',
        route: '/compras',
        roles: [
          applicationRoles.admin,
          applicationRoles.manager,
        ],
      },
      {
        label: 'Clientes',
        route: '/clientes',
      },
      {
        label: 'Financeiro',
        route: '/financeiro',
        roles: [
          applicationRoles.admin,
          applicationRoles.manager,
        ],
      },
      {
        label: 'Entregas',
        route: '/entregas',
      },
    ];

  readonly visibleItems =
    computed(
      () =>
        this.items.filter(
          item =>
            !item.roles ||
            this.auth.hasRole(
              ...item.roles,
            ),
        ),
    );

  readonly roleLabel =
    computed(() => {
      if (
        this.auth.hasRole(
          applicationRoles.admin,
        )
      ) {
        return 'Administrador';
      }

      if (
        this.auth.hasRole(
          applicationRoles.manager,
        )
      ) {
        return 'Gerente';
      }

      if (
        this.auth.hasRole(
          applicationRoles.seller,
        )
      ) {
        return 'Vendedor';
      }

      return '';
    });

  logout(): void {
    this.auth.logout();

    void this.router.navigateByUrl(
      '/login',
    );
  }
}
