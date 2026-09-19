import {
  ChangeDetectionStrategy,
  Component,
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
  of,
} from 'rxjs';

import {
  PaymentMethod,
  Receivable,
  ReceivablesResult,
} from './finance-models';
import {
  FinanceService,
} from './finance.service';

@Component({
  selector: 'app-finance',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './finance.component.html',
  styleUrl: './finance.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinanceComponent {
  private readonly fb =
    inject(FormBuilder).nonNullable;

  private readonly financeService =
    inject(FinanceService);

  readonly paymentMethods =
    PaymentMethod;

  readonly data =
    signal<ReceivablesResult | null>(
      null,
    );

  readonly loading =
    signal(true);

  readonly saving =
    signal(false);

  readonly showPaid =
    signal(false);

  readonly selected =
    signal<Receivable | null>(null);

  readonly error =
    signal<string | null>(null);

  readonly success =
    signal<string | null>(null);

  readonly paymentForm =
    this.fb.group({
      amount: [
        0,
        [
          Validators.required,
          Validators.min(0.01),
        ],
      ],

      method: [
        PaymentMethod.Pix,
        Validators.required,
      ],

      reference: [''],
      notes: [''],
    });

  constructor() {
    this.load();
  }

  togglePaid(): void {
    this.showPaid.update(
      value => !value,
    );

    this.load();
  }

  openPayment(
    receivable: Receivable,
  ): void {
    this.selected.set(
      receivable,
    );

    this.paymentForm.reset({
      amount:
        receivable.outstandingAmount,

      method:
        PaymentMethod.Pix,

      reference: '',
      notes: '',
    });
  }

  closePayment(): void {
    this.selected.set(null);
  }

  registerPayment(): void {
    const receivable =
      this.selected();

    if (
      !receivable ||
      this.paymentForm.invalid
    ) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const raw =
      this.paymentForm.getRawValue();

    this.error.set(null);
    this.success.set(null);
    this.saving.set(true);

    this.financeService
      .registerPayment(
        receivable.id,
        {
          amount:
            Number(raw.amount),

          method:
            Number(
              raw.method,
            ) as PaymentMethod,

          reference:
            raw.reference.trim() ||
            null,

          notes:
            raw.notes.trim() ||
            null,
        },
      )
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível registrar o recebimento.',
          );

          return of(null);
        }),
        finalize(() =>
          this.saving.set(false),
        ),
      )
      .subscribe(result => {
        if (!result) {
          return;
        }

        this.success.set(
          'Recebimento registrado com sucesso.',
        );

        this.closePayment();
        this.load();
      });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.financeService
      .getReceivables(
        !this.showPaid(),
      )
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar o financeiro.',
          );

          return of(null);
        }),
        finalize(() =>
          this.loading.set(false),
        ),
      )
      .subscribe(data => {
        if (data) {
          this.data.set(data);
        }
      });
  }
}
