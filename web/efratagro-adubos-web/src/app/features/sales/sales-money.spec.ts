import {
  describe,
  expect,
  it,
} from 'vitest';

import {
  allocateInstallmentCents,
  calculateSaleTotalCents,
} from './sales-money';

describe(
  'calculateSaleTotalCents',
  () => {
    it(
      'rounds 1.005 to 1.01 like the backend decimal rule',
      () => {
        expect(
          calculateSaleTotalCents([
            {
              quantity: 1.005,
              unitPrice: 1,
            },
          ]),
        ).toBe(101);
      },
    );

    it(
      'rounds 1.015 to 1.02 like the backend decimal rule',
      () => {
        expect(
          calculateSaleTotalCents([
            {
              quantity: 1.015,
              unitPrice: 1,
            },
          ]),
        ).toBe(102);
      },
    );

    it(
      'sums the sale before rounding the final monetary total',
      () => {
        expect(
          calculateSaleTotalCents([
            {
              quantity: 0.335,
              unitPrice: 1,
            },
            {
              quantity: 0.335,
              unitPrice: 1,
            },
          ]),
        ).toBe(67);
      },
    );
  },
);

describe(
  'allocateInstallmentCents',
  () => {
    it(
      'distributes the remainder without losing a cent',
      () => {
        expect(
          allocateInstallmentCents(
            10_000,
            3,
          ),
        ).toEqual([
          3_334,
          3_333,
          3_333,
        ]);
      },
    );
  },
);
