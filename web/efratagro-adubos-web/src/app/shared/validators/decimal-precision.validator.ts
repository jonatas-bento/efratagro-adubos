import {
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';

export function decimalPrecisionValidator(
  maxDecimals: number,
): ValidatorFn {
  if (
    !Number.isInteger(maxDecimals) ||
    maxDecimals < 0
  ) {
    throw new ArgumentOutOfRangeError(
      maxDecimals,
    );
  }

  return (
    control: AbstractControl,
  ): ValidationErrors | null => {
    const value = control.value;

    if (
      value === null ||
      value === undefined ||
      value === ''
    ) {
      return null;
    }

    const numericValue =
      Number(value);

    if (
      !Number.isFinite(
        numericValue,
      )
    ) {
      return null;
    }

    const actualDecimals =
      decimalPlaces(
        numericValue,
      );

    if (
      actualDecimals <=
      maxDecimals
    ) {
      return null;
    }

    return {
      decimalPrecision: {
        maxDecimals,
        actualDecimals,
      },
    };
  };
}

function decimalPlaces(
  value: number,
): number {
  const normalized =
    value
      .toString()
      .toLowerCase();

  const [
    coefficient,
    exponentText,
  ] =
    normalized.split('e');

  const exponent =
    exponentText
      ? Number(exponentText)
      : 0;

  const fractionLength =
    coefficient
      .split('.')[1]
      ?.length ?? 0;

  return Math.max(
    0,
    fractionLength -
      exponent,
  );
}

class ArgumentOutOfRangeError
  extends Error {
  constructor(
    value: number,
  ) {
    super(
      `maxDecimals must be a non-negative integer. Received: ${value}.`,
    );

    this.name =
      'ArgumentOutOfRangeError';
  }
}
