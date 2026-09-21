import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  CommonModule,
} from '@angular/common';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  catchError,
  finalize,
  map,
  of,
} from 'rxjs';
import {
  applicationRoles,
} from '../../core/auth/auth.models';
import {
  AuthService,
} from '../../core/auth/auth.service';
import {
  SystemUser,
} from './user-models';
import {
  UsersService,
} from './users.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl:
    './users.component.html',
  styleUrl:
    './users.component.scss',
  changeDetection:
    ChangeDetectionStrategy.OnPush,
})
export class UsersComponent {
  private readonly fb =
    inject(FormBuilder).nonNullable;

  private readonly usersService =
    inject(UsersService);

  readonly auth =
    inject(AuthService);

  readonly roles = [
    applicationRoles.admin,
    applicationRoles.manager,
    applicationRoles.seller,
  ];

  readonly users =
    signal<SystemUser[]>([]);

  readonly loading =
    signal(true);

  readonly saving =
    signal(false);

  readonly error =
    signal<string | null>(null);

  readonly success =
    signal<string | null>(null);

  readonly editing =
    signal<SystemUser | null>(null);

  readonly editorOpen =
    signal(false);

  readonly resetTarget =
    signal<SystemUser | null>(null);

  readonly totalUsers =
    computed(
      () =>
        this.users().length,
    );

  readonly activeUsers =
    computed(
      () =>
        this.users()
          .filter(
            user =>
              user.isActive,
          )
          .length,
    );

  readonly lockedUsers =
    computed(
      () =>
        this.users()
          .filter(
            user =>
              user.isLockedOut,
          )
          .length,
    );

  readonly editorForm =
    this.fb.group({
      displayName: [
        '',
        [
          Validators.required,
          Validators.maxLength(120),
        ],
      ],
      email: [
        '',
        [
          Validators.required,
          Validators.email,
        ],
      ],
      role:
        this.fb.control<string>(
          applicationRoles.seller,
          {
            validators: [
              Validators.required,
            ],
          },
        ),
      password: [
        '',
        [
          Validators.minLength(8),
        ],
      ],
      isActive: [
        true,
      ],
    });

  readonly resetForm =
    this.fb.group({
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
        ],
      ],
      confirmPassword: [
        '',
        [
          Validators.required,
        ],
      ],
    });

  constructor() {
    this.load();
  }

  openCreate(): void {
    this.editing.set(null);

    this.editorForm.reset({
      displayName: '',
      email: '',
      role:
        applicationRoles.seller,
      password: '',
      isActive: true,
    });

    this.editorOpen.set(true);
  }

  openEdit(
    user: SystemUser,
  ): void {
    this.editing.set(user);

    this.editorForm.reset({
      displayName:
        user.displayName,
      email:
        user.email,
      role:
        user.role,
      password: '',
      isActive:
        user.isActive,
    });

    this.editorOpen.set(true);
  }

  closeEditor(): void {
    if (this.saving()) {
      return;
    }

    this.editorOpen.set(false);
    this.editing.set(null);
  }

  saveUser(): void {
    if (
      this.editorForm.invalid
    ) {
      this.editorForm
        .markAllAsTouched();

      return;
    }

    const raw =
      this.editorForm
        .getRawValue();

    const editing =
      this.editing();

    if (
      !editing &&
      !raw.password.trim()
    ) {
      this.editorForm
        .controls
        .password
        .setErrors({
          required: true,
        });

      return;
    }

    this.error.set(null);
    this.success.set(null);
    this.saving.set(true);

    const request$ =
      editing
        ? this.usersService
            .updateUser(
              editing.id,
              {
                displayName:
                  raw.displayName.trim(),
                email:
                  raw.email.trim(),
                role:
                  raw.role,
                isActive:
                  raw.isActive,
              },
            )
        : this.usersService
            .createUser({
              displayName:
                raw.displayName.trim(),
              email:
                raw.email.trim(),
              password:
                raw.password,
              role:
                raw.role,
            });

    request$
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível salvar o usuário.',
          );

          return of(null);
        }),
        finalize(() =>
          this.saving.set(
            false,
          ),
        ),
      )
      .subscribe(result => {
        if (!result) {
          return;
        }

        this.success.set(
          editing
            ? 'Usuário atualizado com sucesso.'
            : 'Usuário criado com sucesso.',
        );

        this.editorOpen.set(false);
        this.editing.set(null);

        this.load();
      });
  }

  openReset(
    user: SystemUser,
  ): void {
    this.resetTarget.set(user);

    this.resetForm.reset({
      newPassword: '',
      confirmPassword: '',
    });
  }

  closeReset(): void {
    if (this.saving()) {
      return;
    }

    this.resetTarget.set(null);
  }

  resetPassword(): void {
    const target =
      this.resetTarget();

    if (
      !target ||
      this.resetForm.invalid
    ) {
      this.resetForm
        .markAllAsTouched();

      return;
    }

    const raw =
      this.resetForm
        .getRawValue();

    if (
      raw.newPassword !==
      raw.confirmPassword
    ) {
      this.resetForm.controls
        .confirmPassword
        .setErrors({
          mismatch: true,
        });

      return;
    }

    this.error.set(null);
    this.success.set(null);
    this.saving.set(true);

    this.usersService
      .resetPassword(
        target.id,
        {
          newPassword:
            raw.newPassword,
        },
      )
      .pipe(
        map(() => true),
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível redefinir a senha.',
          );

          return of(false);
        }),
        finalize(() =>
          this.saving.set(
            false,
          ),
        ),
      )
      .subscribe(succeeded => {
        if (!succeeded) {
          return;
        }

        this.success.set(
          'Senha redefinida com sucesso.',
        );

        this.resetTarget.set(null);

        this.load();
      });
  }

  unlock(
    user: SystemUser,
  ): void {
    this.error.set(null);
    this.success.set(null);

    this.usersService
      .unlock(
        user.id,
      )
      .pipe(
        map(() => true),
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível desbloquear o usuário.',
          );

          return of(false);
        }),
      )
      .subscribe(succeeded => {
        if (!succeeded) {
          return;
        }

        this.success.set(
          'Usuário desbloqueado com sucesso.',
        );

        this.load();
      });
  }

  roleLabel(
    role: string,
  ): string {
    switch (role) {
      case applicationRoles.admin:
        return 'Administrador';

      case applicationRoles.manager:
        return 'Gerente';

      case applicationRoles.seller:
        return 'Vendedor';

      default:
        return role || 'Sem perfil';
    }
  }

  isCurrentUser(
    user: SystemUser,
  ): boolean {
    return (
      this.auth.currentUser()
        ?.id ===
      user.id
    );
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.usersService
      .getUsers()
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar os usuários.',
          );

          return of([]);
        }),
        finalize(() =>
          this.loading.set(
            false,
          ),
        ),
      )
      .subscribe(users => {
        this.users.set(
          users,
        );
      });
  }
}
