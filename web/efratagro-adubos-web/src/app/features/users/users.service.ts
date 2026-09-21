import {
  HttpClient,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import {
  Observable,
} from 'rxjs';
import {
  CreateSystemUserRequest,
  ResetUserPasswordRequest,
  SystemUser,
  UpdateSystemUserRequest,
} from './user-models';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private readonly http =
    inject(HttpClient);

  getUsers():
    Observable<SystemUser[]> {
    return this.http.get<SystemUser[]>(
      '/api/users',
    );
  }

  createUser(
    request: CreateSystemUserRequest,
  ): Observable<SystemUser> {
    return this.http.post<SystemUser>(
      '/api/users',
      request,
    );
  }

  updateUser(
    userId: string,
    request: UpdateSystemUserRequest,
  ): Observable<SystemUser> {
    return this.http.put<SystemUser>(
      `/api/users/${userId}`,
      request,
    );
  }

  resetPassword(
    userId: string,
    request: ResetUserPasswordRequest,
  ): Observable<void> {
    return this.http.post<void>(
      `/api/users/${userId}/reset-password`,
      request,
    );
  }

  unlock(
    userId: string,
  ): Observable<void> {
    return this.http.post<void>(
      `/api/users/${userId}/unlock`,
      {},
    );
  }
}
