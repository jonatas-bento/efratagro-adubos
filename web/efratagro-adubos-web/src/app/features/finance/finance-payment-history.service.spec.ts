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
  FinanceService,
} from './finance.service';

describe(
  'FinanceService payment history',
  () => {
    let service: FinanceService;
    let http: HttpTestingController;

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          FinanceService,
          provideHttpClient(),
          provideHttpClientTesting(),
        ],
      });

      service =
        TestBed.inject(
          FinanceService,
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
      'loads payment history for a receivable',
      () => {
        service
          .getPayments(
            'receivable-1',
          )
          .subscribe();

        const request =
          http.expectOne(
            '/api/receivables/receivable-1/payments',
          );

        expect(
          request.request.method,
        ).toBe('GET');

        request.flush([]);
      },
    );

    it(
      'sends a payment reversal with its reason',
      () => {
        service
          .reversePayment(
            'payment-1',
            {
              reason:
                'Lançamento duplicado.',
            },
          )
          .subscribe();

        const request =
          http.expectOne(
            '/api/payments/payment-1/reversal',
          );

        expect(
          request.request.method,
        ).toBe('POST');

        expect(
          request.request.body,
        ).toEqual({
          reason:
            'Lançamento duplicado.',
        });

        request.flush({
          reversalId: 'reversal-1',
          paymentId: 'payment-1',
          receivableId: 'receivable-1',
          reversedAmount: 100,
          totalPaid: 0,
          outstandingAmount: 100,
          reversedAtUtc:
            '2026-09-20T19:00:00Z',
          reversedByUserId:
            'user-1',
        });
      },
    );
  },
);
