import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import {
  inject,
  Injectable,
} from '@angular/core';
import {
  map,
  Observable,
} from 'rxjs';

import {
  PagedResult,
} from '../../core/models/paged-result';

import {
  CreateSaleRequest,
  CreateSaleResult,
  SaleSummary,
} from './sale-models';

export interface SalesQueryOptions {
  page?: number;
  pageSize?: number;
  from?: string;
  to?: string;
}

@Injectable({
  providedIn: 'root',
})
export class SalesService {
  private readonly http =
    inject(HttpClient);

  getSalesPage(
    options: SalesQueryOptions = {},
  ): Observable<PagedResult<SaleSummary>> {
    let params =
      new HttpParams()
        .set(
          'page',
          options.page ?? 1,
        )
        .set(
          'pageSize',
          options.pageSize ?? 20,
        );

    if (options.from) {
      params =
        params.set(
          'from',
          options.from,
        );
    }

    if (options.to) {
      params =
        params.set(
          'to',
          options.to,
        );
    }

    return this.http.get<
      PagedResult<SaleSummary>
    >(
      '/api/sales',
      { params },
    );
  }

  getRecentSales(
    take = 20,
  ): Observable<SaleSummary[]> {
    return this
      .getSalesPage({
        page: 1,
        pageSize: take,
      })
      .pipe(
        map(
          result =>
            result.items,
        ),
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
