import {
  DataQualityBannerComponent,
} from '../../shared/data-quality/data-quality-banner.component';

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
  decimalPrecisionValidator,
} from '../../shared/validators/decimal-precision.validator';
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
  PagedResult,
} from '../../core/models/paged-result';
import {
  TemporalPeriod,
  TemporalPeriodFilterComponent,
} from '../../shared/temporal/temporal-period-filter.component';

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
  SaleStockMode,
  SaleSummary,
} from './sale-models';
import {
  SalesService,
} from './sales.service';

import {
  allocateInstallmentCents,
  calculateSaleTotalCents,
} from './sales-money';

@Component({
  selector: 'app-sales',
  standalone: true,
  imports: [
    CommonModule,
    DataQualityBannerComponent,
    ReactiveFormsModule,
    TemporalPeriodFilterComponent,
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

  readonly stockModes =
    SaleStockMode;

  readonly inventory =
    signal<InventoryItem[]>([]);

  readonly customers =
    signal<CustomerListItem[]>([]);

  readonly recentSales =
    signal<SaleSummary[]>([]);

  readonly historyFrom =
    signal('');

  readonly historyTo =
    signal('');

  readonly historyPage =
    signal(1);

  readonly historyTotalPages =
    signal(0);

  readonly historyTotalItems =
    signal(0);

  readonly historyLoading =
    signal(false);

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
            item.isActive &&
            (
              item.availableQuantity ??
              item.quantity
            ) > 0,
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

      stockMode:
        this.fb.control<SaleStockMode>(
          SaleStockMode.Immediate,
          {
            validators: [
              Validators.required,
            ],
          },
        ),
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
    const item =
      this.inventory()
        .find(
          inventoryItem =>
            inventoryItem.productId ===
            productId,
        );

    return (
      item?.availableQuantity ??
      item?.quantity ??
      0
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
    return (
      this.saleTotalCents() /
      100
    );
  }

  private saleTotalCents(): number {
    return calculateSaleTotalCents(
      this.items.controls.map(
        control => {
          const item =
            control.getRawValue();

          return {
            quantity:
              Number(
                item.quantity,
              ),
            unitPrice:
              Number(
                item.unitPrice,
              ),
          };
        },
      ),
    );
  }

  financialSchedule():
      FinancialInstallmentPreview[] {
    const totalCents =
      this.saleTotalCents();

    if (totalCents <= 0) {
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

    const installmentCents =
      allocateInstallmentCents(
        totalCents,
        count,
      );

    return installmentCents.map(
      (amountCents, index) => {

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

      stockMode:
        Number(
          raw.stockMode,
        ) as SaleStockMode,

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

  onHistoryPeriodChange(
    period: TemporalPeriod,
  ): void {
    this.historyFrom.set(
      period.from,
    );

    this.historyTo.set(
      period.to,
    );

    this.loadSalesHistory(1);
  }

  hasHistoryPeriod(): boolean {
    return Boolean(
      this.historyFrom() ||
      this.historyTo(),
    );
  }

  previousHistoryPage(): void {
    if (
      this.historyPage() <= 1
    ) {
      return;
    }

    this.loadSalesHistory(
      this.historyPage() - 1,
    );
  }

  nextHistoryPage(): void {
    if (
      this.historyPage() >=
      this.historyTotalPages()
    ) {
      return;
    }

    this.loadSalesHistory(
      this.historyPage() + 1,
    );
  }

  private loadSalesHistory(
    page: number,
  ): void {
    this.historyLoading.set(true);
    this.error.set(null);

    this.salesService
      .getSalesPage({
        page,
        pageSize: 20,
        from:
          this.historyFrom() ||
          undefined,
        to:
          this.historyTo() ||
          undefined,
      })
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível carregar o histórico de vendas.',
          );

          return of(null);
        }),
        finalize(() =>
          this.historyLoading.set(false),
        ),
      )
      .subscribe(result => {
        if (!result) {
          return;
        }

        this.applySalesPage(
          result,
        );
      });
  }

  private applySalesPage(
    result: PagedResult<SaleSummary>,
  ): void {
    this.recentSales.set(
      result.items,
    );

    this.historyPage.set(
      result.page,
    );

    this.historyTotalPages.set(
      result.totalPages,
    );

    this.historyTotalItems.set(
      result.totalItems,
    );
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
          decimalPrecisionValidator(3),
        ],
      ],

      unitPrice: [
        0,
        [
          Validators.required,
          Validators.min(0),
          decimalPrecisionValidator(2),
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
          .getSalesPage({
            page: 1,
            pageSize: 20,
            from:
              this.historyFrom() ||
              undefined,
            to:
              this.historyTo() ||
              undefined,
          }),

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

        this.applySalesPage(
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
      .stockMode
      .setValue(
        SaleStockMode.Immediate,
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
