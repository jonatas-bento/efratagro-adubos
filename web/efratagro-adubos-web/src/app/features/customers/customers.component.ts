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
  debounceTime,
  finalize,
  of,
} from 'rxjs';

import {
  CustomerDetails,
  CustomerListItem,
} from './customer-models';
import {
  CustomersService,
} from './customers.service';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomersComponent {
  private readonly fb =
    inject(FormBuilder).nonNullable;

  private readonly customersService =
    inject(CustomersService);

  readonly customers =
    signal<CustomerListItem[]>([]);

  readonly selectedCustomer =
    signal<CustomerDetails | null>(null);

  readonly loading =
    signal(true);

  readonly saving =
    signal(false);

  readonly detailsLoading =
    signal(false);

  readonly error =
    signal<string | null>(null);

  readonly showForm =
    signal(false);

  readonly search =
    this.fb.control('');

  readonly customerForm =
    this.fb.group({
      name: [
        '',
        [
          Validators.required,
          Validators.maxLength(180),
        ],
      ],
      phone: [''],
    });

  readonly totalCustomers =
    computed(
      () => this.customers().length,
    );

  readonly totalPurchased =
    computed(
      () =>
        this.customers().reduce(
          (total, customer) =>
            total +
            Number(customer.totalPurchased),
          0,
        ),
    );

  readonly totalOutstanding =
    computed(
      () =>
        this.customers().reduce(
          (total, customer) =>
            total +
            Number(customer.outstandingAmount),
          0,
        ),
    );

  readonly financialPending =
    computed(
      () =>
        this.customers().reduce(
          (total, customer) =>
            total +
            customer.salesWithoutFinancialSchedule,
          0,
        ),
    );

  constructor() {
    this.load();

    this.search.valueChanges
      .pipe(
        debounceTime(250),
      )
      .subscribe(() =>
        this.load(),
      );
  }

  toggleForm(): void {
    this.showForm.update(
      value => !value,
    );
  }

  createCustomer(): void {
    this.error.set(null);

    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    const value =
      this.customerForm.getRawValue();

    this.saving.set(true);

    this.customersService
      .createCustomer({
        name: value.name.trim(),
        phone:
          value.phone.trim() || null,
      })
      .pipe(
        catchError(response => {
          this.error.set(
            response?.error?.error ??
            'Não foi possível cadastrar o cliente.',
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

        this.customerForm.reset({
          name: '',
          phone: '',
        });

        this.showForm.set(false);
        this.load();

        this.openCustomer(
          result.id,
        );
      });
  }

  openCustomer(
    customerId: string,
  ): void {
    this.detailsLoading.set(true);
    this.error.set(null);

    this.customersService
      .getCustomer(customerId)
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar o histórico do cliente.',
          );

          return of(null);
        }),
        finalize(() =>
          this.detailsLoading.set(false),
        ),
      )
      .subscribe(customer => {
        if (customer) {
          this.selectedCustomer.set(
            customer,
          );
        }
      });
  }

  closeCustomer(): void {
    this.selectedCustomer.set(null);
  }

  private load(): void {
    this.loading.set(true);

    this.customersService
      .getCustomers(
        this.search.value,
      )
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar os clientes.',
          );

          return of(
            [] as CustomerListItem[],
          );
        }),
        finalize(() =>
          this.loading.set(false),
        ),
      )
      .subscribe(customers => {
        this.customers.set(
          customers,
        );
      });
  }
}
