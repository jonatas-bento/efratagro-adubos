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
  CreateSaleRequest,
  CreateSaleResult,
  SaleSummary,
} from './sale-models';

@Injectable({
  providedIn: 'root',
})
export class SalesService {
  private readonly http =
    inject(HttpClient);

  getRecentSales(
    take = 20,
  ): Observable<SaleSummary[]> {
    const params =
      new HttpParams()
        .set(
          'take',
          take,
        );

    return this.http.get<SaleSummary[]>(
      '/api/sales',
      { params },
    );
  }

  createSale(
    request: CreateSaleRequest,
  ): Observable<CreateSaleResult> {
    return this.http.post<CreateSaleResult>(
      '/api/sales',
      request,
    );
  }
}
