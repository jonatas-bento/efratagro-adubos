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
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  Subject,
  switchMap,
  tap,
} from 'rxjs';

import {
  TemporalAsOfFilterComponent,
} from '../../shared/temporal/temporal-as-of-filter.component';

import { InventoryItem } from './inventory-item';
import { InventoryService } from './inventory.service';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    CommonModule,
    DataQualityBannerComponent,
    FormsModule,
    TemporalAsOfFilterComponent,
  ],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryComponent {
  private readonly inventoryService =
    inject(InventoryService);

  private readonly searchChanges =
    new Subject<string>();

  readonly search = signal('');

  readonly asOf = signal('');

  readonly items = signal<InventoryItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly totalProducts =
    computed(() => this.items().length);

  readonly productsWithStock =
    computed(() =>
      this.items().filter(
        item =>
          item.isActive &&
          (
            item.availableQuantity ??
            item.quantity
          ) > 0,
      ).length,
    );

  readonly totalQuantity =
    computed(() =>
      this.items().reduce(
        (total, item) =>
          total + item.quantity,
        0,
      ),
    );

  readonly totalReserved =
    computed(() =>
      this.items().reduce(
        (total, item) =>
          total +
          (
            item.reservedQuantity ??
            0
          ),
        0,
      ),
    );

  readonly totalAvailable =
    computed(() =>
      this.items().reduce(
        (total, item) =>
          total +
          (
            item.availableQuantity ??
            item.quantity
          ),
        0,
      ),
    );

  constructor() {
    this.load();

    this.searchChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(search =>
          this.inventoryService
            .getInventory(
              search,
              this.asOf() ||
              undefined,
            )
            .pipe(
              catchError(() => {
                this.error.set(
                  'Não foi possível carregar o estoque.',
                );

                return of([]);
              }),
            ),
        ),
      )
      .subscribe(items => {
        this.items.set(items);
        this.loading.set(false);
      });
  }

  onAsOfChange(
    value: string,
  ): void {
    this.asOf.set(
      value,
    );

    this.load();
  }

  onSearchChange(value: string): void {
    this.search.set(value);
    this.searchChanges.next(value);
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.inventoryService
      .getInventory(
        undefined,
        this.asOf() ||
        undefined,
      )
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar o estoque.',
          );

          return of([]);
        }),
      )
      .subscribe(items => {
        this.items.set(items);
        this.loading.set(false);
      });
  }
}
