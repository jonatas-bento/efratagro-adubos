import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import {
  Observable,
} from 'rxjs';

import {
  DeliverySummary,
} from './delivery-models';

@Injectable({
  providedIn: 'root',
})
export class DeliveriesService {
  private readonly http =
    inject(HttpClient);

  getDeliveries(
    pendingOnly: boolean,
    take = 100,
  ): Observable<DeliverySummary[]> {
    const params =
      new HttpParams()
        .set(
          'pendingOnly',
          pendingOnly,
        )
        .set(
          'take',
          take,
        );

    return this.http.get<DeliverySummary[]>(
      '/api/deliveries',
      { params },
    );
  }

  markDelivered(
    saleId: string,
  ): Observable<void> {
    return this.http.patch<void>(
      `/api/deliveries/${saleId}/complete`,
      {},
    );
  }
}
