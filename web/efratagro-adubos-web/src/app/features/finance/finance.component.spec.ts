import {
  registerLocaleData,
} from '@angular/common';

import localePt from '@angular/common/locales/pt';

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
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';

import {
  FinanceComponent,
} from './finance.component';

registerLocaleData(
  localePt,
  'pt-BR',
);

import {
  PaymentMethod,
  Receivable,
  ReceivableStatusCode,
} from './finance-models';

describe(
  'FinanceComponent payment validation',
  () => {
    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              FinanceComponent,
            ],
            providers: [
              provideHttpClient(),
              provideHttpClientTesting(),
            ],
          })
          .compileComponents();
      },
    );

    it(
      'rejects a payment above the receivable outstanding amount',
      () => {
        const fixture =
          TestBed.createComponent(
            FinanceComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        const initialRequest =
          http.expectOne(
            request =>
              request.url ===
                '/api/receivables' &&
              request.params.get(
                'openOnly',
              ) === 'true',
          );

        initialRequest.flush({
          summary: {
            totalScheduled: 0,
            totalReceived: 0,
            totalOutstanding: 0,
            totalOverdue: 0,
            openInstallments: 0,
            overdueInstallments: 0,
          },
          items: [],
        });

        fixture.detectChanges();

        const receivable: Receivable = {
          id: 'receivable-1',
          saleId: 'sale-1',
          customerId: 'customer-1',
          customerName: 'CLIENTE TESTE',
          installmentNumber: 1,
          dueDate: '2026-10-01T00:00:00Z',
          originalAmount: 100,
          paidAmount: 0,
          outstandingAmount: 100,
          statusCode:
            ReceivableStatusCode.Pending,
          status: 'Pendente',
        };

        component.openPayment(
          receivable,
        );

        fixture.detectChanges();

        const amountControl =
          component.paymentForm
            .controls
            .amount;

        expect(
          amountControl.value,
        ).toBe(100);

        expect(
          amountControl.valid,
        ).toBe(true);

        amountControl.setValue(
          100.01,
        );

        expect(
          amountControl.hasError(
            'max',
          ),
        ).toBe(true);

        expect(
          component.paymentForm
            .invalid,
        ).toBe(true);

        component.registerPayment();

        http.expectNone(
          '/api/receivables/receivable-1/payments',
        );

        http.verify();
      },
    );

    it(
      'uses a stable status code for visual state instead of the display label',
      () => {
        const fixture =
          TestBed.createComponent(
            FinanceComponent,
          );

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        const request =
          http.expectOne(
            candidate =>
              candidate.url ===
                '/api/receivables' &&
              candidate.params.get(
                'openOnly',
              ) === 'true',
          );

        request.flush({
          summary: {
            totalScheduled: 100,
            totalReceived: 0,
            totalOutstanding: 100,
            totalOverdue: 100,
            openInstallments: 1,
            overdueInstallments: 1,
          },
          items: [
            {
              id: 'receivable-overdue',
              saleId: 'sale-overdue',
              customerId: 'customer-overdue',
              customerName: 'CLIENTE TESTE',
              installmentNumber: 1,
              dueDate:
                '2026-09-01T00:00:00Z',
              originalAmount: 100,
              paidAmount: 0,
              outstandingAmount: 100,

              // Contrato novo que queremos:
              // 3 = Overdue.
              statusCode:
                ReceivableStatusCode.Overdue,

              // Texto propositalmente diferente.
              // A aparência NÃO deve depender dele.
              status: 'ATRASADO',
            },
          ],
        });

        fixture.detectChanges();

        const status =
          fixture.nativeElement
            .querySelector(
              '.status',
            ) as HTMLElement;

        expect(
          status.textContent?.trim(),
        ).toBe(
          'ATRASADO',
        );

        expect(
          status.classList.contains(
            'overdue',
          ),
        ).toBe(true);

        expect(
          status.classList.contains(
            'paid',
          ),
        ).toBe(false);

        http.verify();
      },
    );
  },
);
