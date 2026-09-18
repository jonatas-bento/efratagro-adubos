import {
  ChangeDetectionStrategy,
  Component,
  computed,
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
  forkJoin,
  of,
} from 'rxjs';

import {
  CustomerListItem,
} from '../customers/customer-models';
import {
  CustomersService,
} from '../customers/customers.service';
import {
  InventoryItem,
} from '../inventory/inventory-item';
import {
  InventoryService,
} from '../inventory/inventory.service';
import {
  CreateSaleRequest,
  CreateSaleResult,
  DeliveryMethod,
  FinancialInstallmentPreview,
  PaymentCondition,
  SaleSummary,
} from './sale-models';
import {
  SalesService,
} from './sales.service';

@Component({
  selector: 'app-sales',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './sales.component.html',
  styleUrl: './sales.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SalesComponent {
  private readonly fb =
    inject(FormBuilder).nonNullable;

  private readonly inventoryService =
    inject(InventoryService);

  private readonly salesService =
    inject(SalesService);

  private readonly customersService =
    inject(CustomersService);

  readonly deliveryMethods =
    DeliveryMethod;

  readonly inventory =
    signal<InventoryItem[]>([]);

  readonly customers =
    signal<CustomerListItem[]>([]);

  readonly recentSales =
    signal<SaleSummary[]>([]);

  readonly loading = signal(true);
  readonly saving = signal(false);

  readonly error =
    signal<string | null>(null);

  readonly success =
    signal<CreateSaleResult | null>(null);

  readonly availableProducts =
    computed(() =>
      this.inventory()
        .filter(
          item =>
            item.quantity > 0,
        ),
    );

  readonly form =
    this.fb.group({
      customerId: [
        '',
        Validators.required,
      ],

      deliveryMethod: [
        DeliveryMethod.Delivery,
        Validators.required,
      ],

      paymentCondition:
        this.fb.control<PaymentCondition>(
          'cash',
          {
            validators: [
              Validators.required,
            ],
          },
        ),

      installmentCount: [
        2,
        [
          Validators.required,
          Validators.min(2),
          Validators.max(24),
        ],
      ],

      firstDueDate: [
        this.todayString(),
        Validators.required,
      ],

      items: this.fb.array([
        this.createItemGroup(),
      ]),
    });

  constructor() {
    this.loadData();
  }

  get items() {
    return this.form.controls.items;
  }

  isInstallmentSale(): boolean {
    return (
      this.form.controls
        .paymentCondition
        .value ===
      'installments'
    );
  }

  addItem(): void {
    this.items.push(
      this.createItemGroup(),
    );
  }

  removeItem(
    index: number,
  ): void {
    if (this.items.length === 1) {
      return;
    }

    this.items.removeAt(index);
  }

  availableFor(
    productId: string,
  ): number {
    return (
      this.inventory()
        .find(
          item =>
            item.productId ===
            productId,
        )
        ?.quantity ?? 0
    );
  }

  itemTotal(
    index: number,
  ): number {
    const item =
      this.items
        .at(index)
        .getRawValue();

    return (
      Number(item.quantity) *
      Number(item.unitPrice)
    );
  }

  saleTotal(): number {
    return this.items.controls.reduce(
      (total, _, index) =>
        total +
        this.itemTotal(index),
      0,
    );
  }

  financialSchedule():
      FinancialInstallmentPreview[] {
    const total =
      this.saleTotal();

    if (total <= 0) {
      return [];
    }

    const condition =
      this.form.controls
        .paymentCondition.value;

    const count =
      condition === 'cash'
        ? 1
        : Math.max(
            2,
            Number(
              this.form.controls
                .installmentCount.value,
            ),
          );

    const firstDueDateValue =
      this.form.controls
        .firstDueDate.value;

    if (!firstDueDateValue) {
      return [];
    }

    const firstDueDate =
      this.parseDateOnly(
        firstDueDateValue,
      );

    const totalCents =
      Math.round(total * 100);

    const baseCents =
      Math.floor(
        totalCents / count,
      );

    const remainder =
      totalCents -
      baseCents * count;

    return Array.from(
      { length: count },
      (_, index) => {
        const amountCents =
          baseCents +
          (index < remainder ? 1 : 0);

        return {
          installmentNumber:
            index + 1,

          dueDate:
            this.formatDateOnly(
              this.addMonths(
                firstDueDate,
                index,
              ),
            ),

          amount:
            amountCents / 100,
        };
      },
    );
  }

  submit(): void {
    this.error.set(null);
    this.success.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw =
      this.form.getRawValue();

    const schedule =
      this.financialSchedule();

    if (schedule.length === 0) {
      this.error.set(
        'Informe a programação financeira.',
      );

      return;
    }

    const request: CreateSaleRequest = {
      customerId:
        raw.customerId,

      deliveryMethod:
        Number(
          raw.deliveryMethod,
        ) as DeliveryMethod,

      items:
        raw.items.map(
          item => ({
            productId:
              item.productId,

            quantity:
              Number(item.quantity),

            unitPrice:
              Number(item.unitPrice),
          }),
        ),

      receivables:
        schedule.map(
          installment => ({
            installmentNumber:
              installment.installmentNumber,

            dueDate:
              installment.dueDate,

            amount:
              installment.amount,
          }),
        ),
    };

    this.saving.set(true);

    this.salesService
      .createSale(request)
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível registrar a venda.',
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

        this.success.set(result);

        this.resetForm();
        this.loadData();
      });
  }

  private createItemGroup() {
    return this.fb.group({
      productId: [
        '',
        Validators.required,
      ],

      quantity: [
        1,
        [
          Validators.required,
          Validators.min(0.001),
        ],
      ],

      unitPrice: [
        0,
        [
          Validators.required,
          Validators.min(0),
        ],
      ],
    });
  }

  private loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      inventory:
        this.inventoryService
          .getInventory(),

      sales:
        this.salesService
          .getRecentSales(20),

      customers:
        this.customersService
          .getCustomers('', 1000),
    })
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar os dados de vendas.',
          );

          return of(null);
        }),
        finalize(() =>
          this.loading.set(false),
        ),
      )
      .subscribe(data => {
        if (!data) {
          return;
        }

        this.inventory.set(
          data.inventory,
        );

        this.recentSales.set(
          data.sales,
        );

        this.customers.set(
          data.customers,
        );
      });
  }

  private resetForm(): void {
    this.form.controls
      .customerId
      .setValue('');

    this.form.controls
      .deliveryMethod
      .setValue(
        DeliveryMethod.Delivery,
      );

    this.form.controls
      .paymentCondition
      .setValue('cash');

    this.form.controls
      .installmentCount
      .setValue(2);

    this.form.controls
      .firstDueDate
      .setValue(
        this.todayString(),
      );

    this.items.clear();

    this.items.push(
      this.createItemGroup(),
    );

    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private todayString(): string {
    return this.formatDateOnly(
      new Date(),
    );
  }

  private parseDateOnly(
    value: string,
  ): Date {
    const [
      year,
      month,
      day,
    ] =
      value
        .split('-')
        .map(Number);

    return new Date(
      year,
      month - 1,
      day,
      12,
      0,
      0,
    );
  }

  private formatDateOnly(
    value: Date,
  ): string {
    const year =
      value.getFullYear();

    const month =
      String(
        value.getMonth() + 1,
      ).padStart(2, '0');

    const day =
      String(
        value.getDate(),
      ).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private addMonths(
    value: Date,
    months: number,
  ): Date {
    const originalDay =
      value.getDate();

    const result =
      new Date(value);

    result.setDate(1);

    result.setMonth(
      result.getMonth() +
      months,
    );

    const lastDay =
      new Date(
        result.getFullYear(),
        result.getMonth() + 1,
        0,
      ).getDate();

    result.setDate(
      Math.min(
        originalDay,
        lastDay,
      ),
    );

    return result;
  }
}
