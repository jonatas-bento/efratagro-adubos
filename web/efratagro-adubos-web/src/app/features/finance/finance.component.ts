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
  PaymentHistoryItem,
  PaymentMethod,
  Receivable,
  ReceivableStatusCode,
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

  readonly receivableStatuses =
    ReceivableStatusCode;

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

  readonly historyReceivable =
    signal<Receivable | null>(null);

  readonly paymentHistory =
    signal<PaymentHistoryItem[]>([]);

  readonly historyLoading =
    signal(false);

  readonly reversalTarget =
    signal<PaymentHistoryItem | null>(
      null,
    );

  readonly reversing =
    signal(false);

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

  readonly reversalForm =
    this.fb.group({
      reason: [
        '',
        [
          Validators.required,
          Validators.maxLength(500),
        ],
      ],
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

  openHistory(
    receivable: Receivable,
  ): void {
    this.historyReceivable.set(
      receivable,
    );

    this.paymentHistory.set([]);

    this.loadPaymentHistory();
  }

  closeHistory(): void {
    if (this.reversalTarget()) {
      return;
    }

    this.historyReceivable.set(null);

    this.paymentHistory.set([]);
  }

  openReversal(
    payment: PaymentHistoryItem,
  ): void {
    if (payment.reversal) {
      return;
    }

    this.reversalTarget.set(
      payment,
    );

    this.reversalForm.reset({
      reason: '',
    });
  }

  closeReversal(): void {
    if (this.reversing()) {
      return;
    }

    this.reversalTarget.set(null);

    this.reversalForm.reset({
      reason: '',
    });
  }

  reversePayment(): void {
    const payment =
      this.reversalTarget();

    if (
      !payment ||
      this.reversalForm.invalid
    ) {
      this.reversalForm.markAllAsTouched();

      return;
    }

    const reason =
      this.reversalForm
        .getRawValue()
        .reason
        .trim();

    if (!reason) {
      this.reversalForm
        .controls
        .reason
        .setErrors({
          required: true,
        });

      return;
    }

    this.error.set(null);
    this.success.set(null);
    this.reversing.set(true);

    this.financeService
      .reversePayment(
        payment.id,
        {
          reason,
        },
      )
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível estornar o recebimento.',
          );

          return of(null);
        }),
        finalize(() =>
          this.reversing.set(
            false,
          ),
        ),
      )
      .subscribe(result => {
        if (!result) {
          return;
        }

        this.success.set(
          'Recebimento estornado com sucesso.',
        );

        this.reversalTarget.set(null);

        this.reversalForm.reset({
          reason: '',
        });

        this.loadPaymentHistory();

        this.load();
      });
  }

  paymentMethodLabel(
    method: PaymentMethod,
  ): string {
    switch (method) {
      case PaymentMethod.Pix:
        return 'Pix';

      case PaymentMethod.Cash:
        return 'Dinheiro';

      case PaymentMethod.Card:
        return 'Cartão';

      case PaymentMethod.BankTransfer:
        return 'Transferência';

      case PaymentMethod.Boleto:
        return 'Boleto';

      case PaymentMethod.Check:
        return 'Cheque';

      case PaymentMethod.Other:
        return 'Outro';

      default:
        return 'Não informado';
    }
  }

  private loadPaymentHistory(): void {
    const receivable =
      this.historyReceivable();

    if (!receivable) {
      return;
    }

    this.historyLoading.set(true);

    this.financeService
      .getPayments(
        receivable.id,
      )
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar o histórico de recebimentos.',
          );

          return of([]);
        }),
        finalize(() =>
          this.historyLoading.set(
            false,
          ),
        ),
      )
      .subscribe(payments => {
        this.paymentHistory.set(
          payments,
        );
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
