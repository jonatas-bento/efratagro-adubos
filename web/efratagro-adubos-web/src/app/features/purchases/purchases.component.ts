import {
  ChangeDetectionStrategy,
  Component,
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
  InventoryItem,
} from '../inventory/inventory-item';
import {
  InventoryService,
} from '../inventory/inventory.service';
import {
  CreatePurchaseRequest,
  CreatePurchaseResult,
  PurchaseSummary,
  SupplierOption,
} from './purchase-models';
import {
  PurchasesService,
} from './purchases.service';

@Component({
  selector: 'app-purchases',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './purchases.component.html',
  styleUrl: './purchases.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchasesComponent {
  private readonly fb =
    inject(FormBuilder).nonNullable;

  private readonly inventoryService =
    inject(InventoryService);

  private readonly purchasesService =
    inject(PurchasesService);

  readonly inventory =
    signal<InventoryItem[]>([]);

  readonly suppliers =
    signal<SupplierOption[]>([]);

  readonly recentPurchases =
    signal<PurchaseSummary[]>([]);

  readonly loading =
    signal(true);

  readonly saving =
    signal(false);

  readonly error =
    signal<string | null>(null);

  readonly success =
    signal<CreatePurchaseResult | null>(null);

  readonly form =
    this.fb.group({
      supplierId: [
        '',
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

  productsForSupplier():
      InventoryItem[] {
    const supplierId =
      this.form.controls
        .supplierId
        .value;

    const supplier =
      this.suppliers()
        .find(
          item =>
            item.id === supplierId,
        );

    if (!supplier) {
      return [];
    }

    return this.inventory()
      .filter(
        product =>
          product.isActive &&
          product.supplierName ===
          supplier.name,
      );
  }

  supplierChanged(): void {
    for (
      const item
      of this.items.controls
    ) {
      item.controls
        .productId
        .setValue('');
    }
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

  itemTotal(
    index: number,
  ): number {
    const item =
      this.items
        .at(index)
        .getRawValue();

    return (
      Number(item.quantity) *
      Number(item.unitCost)
    );
  }

  purchaseTotal(): number {
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

    const request: CreatePurchaseRequest = {
      supplierId:
        raw.supplierId,

      items:
        raw.items.map(
          item => ({
            productId:
              item.productId,

            quantity:
              Number(item.quantity),

            unitCost:
              Number(item.unitCost),
          }),
        ),
    };

    this.saving.set(true);

    this.purchasesService
      .createPurchase(request)
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível registrar a compra.',
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
          decimalPrecisionValidator(3),
        ],
      ],

      unitCost: [
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

      suppliers:
        this.purchasesService
          .getSuppliers(),

      purchases:
        this.purchasesService
          .getRecentPurchases(),
    })
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar os dados de compras.',
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

        this.suppliers.set(
          data.suppliers,
        );

        this.recentPurchases.set(
          data.purchases,
        );
      });
  }

  private resetForm(): void {
    this.form.controls
      .supplierId
      .reset('');

    this.items.clear();

    this.items.push(
      this.createItemGroup(),
    );

    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.form.updateValueAndValidity();
  }
}
