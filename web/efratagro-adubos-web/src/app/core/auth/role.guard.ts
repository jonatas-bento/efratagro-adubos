import {
  inject,
} from '@angular/core';
import {
  CanActivateFn,
  Router,
} from '@angular/router';
import {
  map,
} from 'rxjs';
import {
  AuthService,
} from './auth.service';

export const roleGuard:
  CanActivateFn =
    route => {
      const auth =
        inject(AuthService);

      const router =
        inject(Router);

      const configuredRoles =
        route.data['roles'] as
          string[] | undefined;

      const allowedRoles =
        configuredRoles ?? [];

      return auth.ensureSession()
        .pipe(
          map(authenticated => {
            if (!authenticated) {
              return router
                .createUrlTree(
                  ['/login'],
                );
            }

            if (
              allowedRoles.length === 0 ||
              auth.hasRole(
                ...allowedRoles,
              )
            ) {
              return true;
            }

            return router
              .createUrlTree(
                ['/estoque'],
              );
          }),
        );
    };
