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
  },
);
