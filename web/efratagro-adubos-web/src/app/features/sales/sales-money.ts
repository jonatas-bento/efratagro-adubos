export interface SaleMoneyItem {
  quantity: number;
  unitPrice: number;
}

const MILLICENTS_PER_CENT = 1000n;

function toScaledInteger(
  value: number,
  decimalPlaces: number,
): bigint {
  if (!Number.isFinite(value)) {
    throw new RangeError(
      'Value must be finite.',
    );
  }

  const negative =
    value < 0;

  const fixed =
    Math.abs(value)
      .toFixed(decimalPlaces);

  const [
    whole,
    fraction = '',
  ] = fixed.split('.');

  const scale =
    10n ** BigInt(decimalPlaces);

  const fractionalPart =
    fraction
      .padEnd(
        decimalPlaces,
        '0',
      )
      .slice(
        0,
        decimalPlaces,
      );

  const scaled =
    BigInt(whole) *
      scale +
    BigInt(
      fractionalPart || '0',
    );

  return negative
    ? -scaled
    : scaled;
}

function roundMilliCentsToCents(
  milliCents: bigint,
): bigint {
  const half =
    MILLICENTS_PER_CENT /
    2n;

  if (milliCents >= 0n) {
    return (
      milliCents +
      half
    ) /
      MILLICENTS_PER_CENT;
  }

  return (
    milliCents -
    half
  ) /
    MILLICENTS_PER_CENT;
}

export function calculateSaleTotalCents(
  items: readonly SaleMoneyItem[],
): number {
  const totalMilliCents =
    items.reduce(
      (
        total,
        item,
      ) => {
        const quantityThousandths =
          toScaledInteger(
            item.quantity,
            3,
          );

        const unitPriceCents =
          toScaledInteger(
            item.unitPrice,
            2,
          );

        return (
          total +
          quantityThousandths *
            unitPriceCents
        );
      },
      0n,
    );

  const totalCents =
    roundMilliCentsToCents(
      totalMilliCents,
    );

  const numericTotal =
    Number(totalCents);

  if (
    !Number.isSafeInteger(
      numericTotal,
    )
  ) {
    throw new RangeError(
      'Sale total exceeds the safe integer range.',
    );
  }

  return numericTotal;
}

export function allocateInstallmentCents(
  totalCents: number,
  installmentCount: number,
): number[] {
  if (
    !Number.isInteger(totalCents) ||
    totalCents <= 0
  ) {
    throw new RangeError(
      'Total cents must be a positive integer.',
    );
  }

  if (
    !Number.isInteger(
      installmentCount,
    ) ||
    installmentCount <= 0
  ) {
    throw new RangeError(
      'Installment count must be a positive integer.',
    );
  }

  const baseCents =
    Math.floor(
      totalCents /
        installmentCount,
    );

  const remainder =
    totalCents -
    baseCents *
      installmentCount;

  return Array.from(
    {
      length:
        installmentCount,
    },
    (_, index) =>
      baseCents +
      (
        index < remainder
          ? 1
          : 0
      ),
  );
}
