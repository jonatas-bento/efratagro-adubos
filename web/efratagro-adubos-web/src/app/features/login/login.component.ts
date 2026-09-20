import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  ActivatedRoute,
  Router,
} from '@angular/router';
import {
  finalize,
} from 'rxjs';
import {
  AuthService,
} from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
  ],
  templateUrl:
    './login.component.html',
  styleUrl:
    './login.component.scss',
  changeDetection:
    ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly auth =
    inject(AuthService);

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  readonly submitting =
    signal(false);

  readonly errorMessage =
    signal('');

  readonly form =
    new FormGroup({
      email:
        new FormControl(
          '',
          {
            nonNullable: true,
            validators: [
              Validators.required,
              Validators.email,
            ],
          },
        ),

      password:
        new FormControl(
          '',
          {
            nonNullable: true,
            validators: [
              Validators.required,
            ],
          },
        ),
    });

  submit(): void {
    if (
      this.form.invalid ||
      this.submitting()
    ) {
      this.form.markAllAsTouched();

      return;
    }

    this.errorMessage.set('');
    this.submitting.set(true);

    this.auth.login(
      this.form.getRawValue(),
    )
      .pipe(
        finalize(
          () =>
            this.submitting.set(
              false,
            ),
        ),
      )
      .subscribe({
        next: () => {
          const returnUrl =
            this.route.snapshot
              .queryParamMap
              .get('returnUrl');

          const destination =
            returnUrl?.startsWith('/') &&
            !returnUrl.startsWith(
              '/login',
            )
              ? returnUrl
              : '/estoque';

          void this.router
            .navigateByUrl(
              destination,
            );
        },

        error: error => {
          if (error.status === 401) {
            this.errorMessage.set(
              'Email ou senha inválidos.',
            );

            return;
          }

          this.errorMessage.set(
            'Não foi possível entrar agora. Tente novamente.',
          );
        },
      });
  }
}
