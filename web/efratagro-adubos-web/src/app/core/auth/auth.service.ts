import {
  computed,
  inject,
  Injectable,
  signal,
} from '@angular/core';
import {
  HttpClient,
} from '@angular/common/http';
import {
  catchError,
  map,
  Observable,
  of,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import {
  CurrentUser,
  LoginRequest,
  LoginResponse,
} from './auth.models';
import {
  clearAccessToken,
  readAccessToken,
  writeAccessToken,
} from './auth-token.storage';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http =
    inject(HttpClient);

  private readonly currentUserState =
    signal<CurrentUser | null>(
      null,
    );

  readonly currentUser =
    this.currentUserState.asReadonly();

  readonly isAuthenticated =
    computed(
      () =>
        this.currentUserState()
        !== null,
    );

  readonly roles =
    computed(
      () =>
        this.currentUserState()
          ?.roles ?? [],
    );

  login(
    request: LoginRequest,
  ): Observable<CurrentUser> {
    return this.http
      .post<LoginResponse>(
        '/api/auth/login',
        request,
      )
      .pipe(
        tap(
          response =>
            writeAccessToken(
              response.accessToken,
            ),
        ),
        switchMap(
          () =>
            this.loadCurrentUser(),
        ),
        catchError(error => {
          this.clearSession();

          return throwError(
            () => error,
          );
        }),
      );
  }

  ensureSession():
    Observable<boolean> {
    if (this.currentUserState()) {
      return of(true);
    }

    if (!readAccessToken()) {
      return of(false);
    }

    return this.loadCurrentUser()
      .pipe(
        map(() => true),
        catchError(() => {
          this.clearSession();

          return of(false);
        }),
      );
  }

  hasRole(
    ...roles: string[]
  ): boolean {
    const currentRoles =
      this.currentUserState()
        ?.roles ?? [];

    return roles.some(
      role =>
        currentRoles.includes(
          role,
        ),
    );
  }

  logout(): void {
    this.clearSession();
  }

  private loadCurrentUser():
    Observable<CurrentUser> {
    return this.http
      .get<CurrentUser>(
        '/api/auth/me',
      )
      .pipe(
        tap(user =>
          this.currentUserState.set(
            user,
          ),
        ),
      );
  }

  private clearSession(): void {
    clearAccessToken();

    this.currentUserState.set(
      null,
    );
  }
}
