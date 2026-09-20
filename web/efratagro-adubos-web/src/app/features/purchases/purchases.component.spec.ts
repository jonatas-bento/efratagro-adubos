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
  PurchasesComponent,
} from './purchases.component';

describe(
  'PurchasesComponent input precision',
  () => {
    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              PurchasesComponent,
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
      'rejects quantities with more than three decimal places',
      () => {
        const fixture =
          TestBed.createComponent(
            PurchasesComponent,
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
      'rejects unit costs with more than two decimal places',
      () => {
        const fixture =
          TestBed.createComponent(
            PurchasesComponent,
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

        const unitCost =
          component.items
            .at(0)
            .controls
            .unitCost;

        unitCost.setValue(
          10.01,
        );

        expect(
          unitCost.valid,
        ).toBe(true);

        unitCost.setValue(
          10.001,
        );

        expect(
          unitCost.hasError(
            'decimalPrecision',
          ),
        ).toBe(true);

        expect(
          unitCost.invalid,
        ).toBe(true);
      },
    );
  },
);
