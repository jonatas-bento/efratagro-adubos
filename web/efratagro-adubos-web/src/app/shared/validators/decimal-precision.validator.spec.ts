import {
  FormControl,
} from '@angular/forms';

import {
  describe,
  expect,
  it,
} from 'vitest';

import {
  decimalPrecisionValidator,
} from './decimal-precision.validator';

describe(
  'decimalPrecisionValidator',
  () => {
    it(
      'accepts values within the configured precision',
      () => {
        const quantity =
          new FormControl(
            1.001,
            decimalPrecisionValidator(3),
          );

        const money =
          new FormControl(
            10.01,
            decimalPrecisionValidator(2),
          );

        expect(
          quantity.valid,
        ).toBe(true);

        expect(
          money.valid,
        ).toBe(true);
      },
    );

    it(
      'rejects values beyond the configured precision',
      () => {
        const quantity =
          new FormControl(
            1.0001,
            decimalPrecisionValidator(3),
          );

        const money =
          new FormControl(
            10.001,
            decimalPrecisionValidator(2),
          );

        expect(
          quantity.hasError(
            'decimalPrecision',
          ),
        ).toBe(true);

        expect(
          money.hasError(
            'decimalPrecision',
          ),
        ).toBe(true);
      },
    );

    it(
      'handles scientific notation consistently',
      () => {
        const valid =
          new FormControl(
            1e-3,
            decimalPrecisionValidator(3),
          );

        const invalid =
          new FormControl(
            1e-4,
            decimalPrecisionValidator(3),
          );

        expect(
          valid.valid,
        ).toBe(true);

        expect(
          invalid.hasError(
            'decimalPrecision',
          ),
        ).toBe(true);
      },
    );

    it(
      'leaves empty values to required validation',
      () => {
        const validator =
          decimalPrecisionValidator(2);

        expect(
          validator(
            new FormControl(null),
          ),
        ).toBeNull();

        expect(
          validator(
            new FormControl(''),
          ),
        ).toBeNull();
      },
    );

    it(
      'rejects an invalid precision configuration',
      () => {
        expect(
          () =>
            decimalPrecisionValidator(
              -1,
            ),
        ).toThrow();

        expect(
          () =>
            decimalPrecisionValidator(
              1.5,
            ),
        ).toThrow();
      },
    );
  },
);
