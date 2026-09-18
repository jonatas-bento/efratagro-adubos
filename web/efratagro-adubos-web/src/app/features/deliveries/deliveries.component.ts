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
  catchError,
  finalize,
  of,
} from 'rxjs';

import {
  DeliverySummary,
} from './delivery-models';
import {
  DeliveriesService,
} from './deliveries.service';

@Component({
  selector: 'app-deliveries',
  standalone: true,
  imports: [
    CommonModule,
  ],
  templateUrl: './deliveries.component.html',
  styleUrl: './deliveries.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeliveriesComponent {
  private readonly deliveriesService =
    inject(DeliveriesService);

  readonly deliveries =
    signal<DeliverySummary[]>([]);

  readonly loading =
    signal(true);

  readonly completingSaleId =
    signal<string | null>(null);

  readonly showCompleted =
    signal(false);

  readonly error =
    signal<string | null>(null);

  readonly success =
    signal<string | null>(null);

  readonly pendingCount =
    computed(
      () =>
        this.deliveries()
          .filter(
            delivery =>
              delivery.status === 'Pendente',
          )
          .length,
    );

  readonly deliveredCount =
    computed(
      () =>
        this.deliveries()
          .filter(
            delivery =>
              delivery.status === 'Entregue',
          )
          .length,
    );

  readonly totalQuantity =
    computed(
      () =>
        this.deliveries()
          .reduce(
            (total, delivery) =>
              total +
              Number(
                delivery.totalQuantity,
              ),
            0,
          ),
    );

  constructor() {
    this.load();
  }

  toggleCompleted(): void {
    this.showCompleted.update(
      value => !value,
    );

    this.load();
  }

  markDelivered(
    delivery: DeliverySummary,
  ): void {
    if (
      this.completingSaleId() !== null
    ) {
      return;
    }

    this.error.set(null);
    this.success.set(null);

    this.completingSaleId.set(
      delivery.saleId,
    );

    let failed = false;

    this.deliveriesService
      .markDelivered(
        delivery.saleId,
      )
      .pipe(
        catchError(response => {
          failed = true;

          this.error.set(
            response?.error?.error ??
            'Não foi possível concluir a entrega.',
          );

          return of(undefined);
        }),
        finalize(() =>
          this.completingSaleId.set(
            null,
          ),
        ),
      )
      .subscribe(() => {
        if (failed) {
          return;
        }

        this.success.set(
          `Entrega de ${delivery.customerName} concluída.`,
        );

        this.load();
      });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.deliveriesService
      .getDeliveries(
        !this.showCompleted(),
      )
      .pipe(
        catchError(() => {
          this.error.set(
            'Não foi possível carregar as entregas.',
          );

          return of(
            [] as DeliverySummary[],
          );
        }),
        finalize(() =>
          this.loading.set(false),
        ),
      )
      .subscribe(deliveries => {
        this.deliveries.set(
          deliveries,
        );
      });
  }
}
