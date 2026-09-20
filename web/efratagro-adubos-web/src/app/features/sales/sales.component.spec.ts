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
  SalesComponent,
} from './sales.component';

describe(
  'SalesComponent financial integration',
  () => {
    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              SalesComponent,
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
      'uses exact cent arithmetic in the sale total and installment schedule',
      () => {
        const fixture =
          TestBed.createComponent(
            SalesComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        const initialRequests =
          http.match(() => true);

        for (
          const request
          of initialRequests
        ) {
          request.flush([]);
        }

        http.verify();

        component.items
          .at(0)
          .patchValue({
            quantity: 1.005,
            unitPrice: 1,
          });

        component.form.controls
          .paymentCondition
          .setValue(
            'installments',
          );

        component.form.controls
          .installmentCount
          .setValue(2);

        component.form.controls
          .firstDueDate
          .setValue(
            '2026-10-01',
          );

        expect(
          component.saleTotal(),
        ).toBe(1.01);

        const schedule =
          component
            .financialSchedule();

        expect(
          schedule.map(
            installment =>
              installment.amount,
          ),
        ).toEqual([
          0.51,
          0.50,
        ]);

        expect(
          schedule.reduce(
            (
              total,
              installment,
            ) =>
              total +
              installment.amount,
            0,
          ),
        ).toBeCloseTo(
          1.01,
          10,
        );
      },
    );
    it(
      'rejects quantities with more than three decimal places',
      () => {
        const fixture =
          TestBed.createComponent(
            SalesComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        const initialRequests =
          http.match(() => true);

        for (
          const request
          of initialRequests
        ) {
          request.flush([]);
        }

        http.verify();

        const quantity =
          component.items
            .at(0)
            .controls
            .quantity;

        quantity.setValue(
          1.001,
        );

        expect(
          quantity.valid,
        ).toBe(true);

        quantity.setValue(
          1.0001,
        );

        expect(
          quantity.hasError(
            'decimalPrecision',
          ),
        ).toBe(true);

        expect(
          quantity.invalid,
        ).toBe(true);
      },
    );

    it(
      'rejects unit prices with more than two decimal places',
      () => {
        const fixture =
          TestBed.createComponent(
            SalesComponent,
          );

        const component =
          fixture.componentInstance;

        const http =
          TestBed.inject(
            HttpTestingController,
          );

        const initialRequests =
          http.match(() => true);

        for (
          const request
          of initialRequests
        ) {
          request.flush([]);
        }

        http.verify();

        const unitPrice =
          component.items
            .at(0)
            .controls
            .unitPrice;

        unitPrice.setValue(
          10.01,
        );

        expect(
          unitPrice.valid,
        ).toBe(true);

        unitPrice.setValue(
          10.001,
        );

        expect(
          unitPrice.hasError(
            'decimalPrecision',
          ),
        ).toBe(true);

        expect(
          unitPrice.invalid,
        ).toBe(true);
      },
    );

  },
);
