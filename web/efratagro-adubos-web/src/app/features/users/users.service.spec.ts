import {
  provideHttpClient,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import {
  TestBed,
} from '@angular/core/testing';
import {
  UsersService,
} from './users.service';

describe(
  'UsersService',
  () => {
    let service: UsersService;
    let http:
      HttpTestingController;

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          UsersService,
          provideHttpClient(),
          provideHttpClientTesting(),
        ],
      });

      service =
        TestBed.inject(
          UsersService,
        );

      http =
        TestBed.inject(
          HttpTestingController,
        );
    });

    afterEach(() => {
      http.verify();
    });

    it(
      'loads system users',
      () => {
        service
          .getUsers()
          .subscribe();

        const request =
          http.expectOne(
            '/api/users',
          );

        expect(
          request.request.method,
        ).toBe('GET');

        request.flush([]);
      },
    );

    it(
      'creates a user',
      () => {
        service
          .createUser({
            displayName: 'Maria',
            email:
              'maria@efratagro.local',
            password:
              'ValidPass1',
            role: 'Seller',
          })
          .subscribe();

        const request =
          http.expectOne(
            '/api/users',
          );

        expect(
          request.request.method,
        ).toBe('POST');

        request.flush({});
      },
    );

    it(
      'updates a user',
      () => {
        service
          .updateUser(
            'user-1',
            {
              displayName: 'Maria',
              email:
                'maria@efratagro.local',
              role: 'Manager',
              isActive: true,
            },
          )
          .subscribe();

        const request =
          http.expectOne(
            '/api/users/user-1',
          );

        expect(
          request.request.method,
        ).toBe('PUT');

        request.flush({});
      },
    );

    it(
      'resets password',
      () => {
        service
          .resetPassword(
            'user-1',
            {
              newPassword:
                'NewPass123',
            },
          )
          .subscribe();

        const request =
          http.expectOne(
            '/api/users/user-1/reset-password',
          );

        expect(
          request.request.method,
        ).toBe('POST');

        request.flush(null);
      },
    );

    it(
      'unlocks a user',
      () => {
        service
          .unlock(
            'user-1',
          )
          .subscribe();

        const request =
          http.expectOne(
            '/api/users/user-1/unlock',
          );

        expect(
          request.request.method,
        ).toBe('POST');

        request.flush(null);
      },
    );
  },
);
