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
  InventoryItem,
} from '../inventory/inventory-item';
import {
  InventoryService,
} from '../inventory/inventory.service';
import {
  CreateSaleRequest,
  CreateSaleResult,
  DeliveryMethod,
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
  readonly deliveryMethods = DeliveryMethod;
  private readonly fb =
    inject(FormBuilder).nonNullable;

  private readonly inventoryService =
    inject(InventoryService);

  private readonly salesService =
    inject(SalesService);

  readonly inventory =
    signal<InventoryItem[]>([]);

  readonly recentSales =
    signal<SaleSummary[]>([]);

  readonly loading =
    signal(true);

  readonly saving =
    signal(false);

  readonly error =
    signal<string | null>(null);

  readonly success =
    signal<CreateSaleResult | null>(null);

  readonly availableProducts =
    computed(() =>
      this.inventory()
        .filter(item => item.quantity > 0),
    );

  readonly form =
    this.fb.group({
      customerName: [
        '',
        [
          Validators.required,
          Validators.maxLength(180),
        ],
      ],
      customerPhone: [''],
      deliveryMethod: [
        DeliveryMethod.Delivery,
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
            item.productId === productId,
        )
        ?.quantity ?? 0
    );
  }

  itemTotal(
    index: number,
  ): number {
    const item =
      this.items.at(index)
        .getRawValue();

    return (
      Number(item.quantity) *
      Number(item.unitPrice)
    );
  }

  saleTotal(): number {
    return this.items.controls.reduce(
      (total, _, index) =>
        total + this.itemTotal(index),
      0,
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

    const request: CreateSaleRequest = {
      customerName:
        raw.customerName.trim(),

      customerPhone:
        raw.customerPhone.trim() ||
        null,

      deliveryMethod:
        Number(raw.deliveryMethod) as DeliveryMethod,

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
    };

    this.saving.set(true);

    this.salesService
      .createSale(request)
      .pipe(
        catchError(response => {
          const message =
            response?.error?.error ??
            'Não foi possível registrar a venda.';

          this.error.set(message);

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
      });
  }

  private resetForm(): void {
    this.form.controls
      .customerName
      .setValue('');

    this.form.controls
      .customerPhone
      .setValue('');

    this.form.controls
      .deliveryMethod
      .setValue(
        DeliveryMethod.Delivery,
      );

    while (
      this.items.length > 0
    ) {
      this.items.removeAt(0);
    }

    this.items.push(
      this.createItemGroup(),
    );
  }
}
