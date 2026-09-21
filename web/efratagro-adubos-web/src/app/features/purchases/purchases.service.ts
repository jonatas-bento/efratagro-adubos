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
  CreatePurchaseRequest,
  CreatePurchaseResult,
  PurchaseSummary,
  SupplierOption,
} from './purchase-models';

export interface PurchasesQueryOptions {
  page?: number;
  pageSize?: number;
  from?: string;
  to?: string;
}

@Injectable({
  providedIn: 'root',
})
export class PurchasesService {
  private readonly http =
    inject(HttpClient);

  getSuppliers():
      Observable<SupplierOption[]> {
    return this.http.get<
      SupplierOption[]
    >(
      '/api/suppliers',
    );
  }

  getPurchasesPage(
    options: PurchasesQueryOptions = {},
  ): Observable<
      PagedResult<PurchaseSummary>> {
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
      PagedResult<PurchaseSummary>
    >(
      '/api/purchases',
      { params },
    );
  }

  getRecentPurchases(
    take = 20,
  ): Observable<PurchaseSummary[]> {
    return this
      .getPurchasesPage({
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

  createPurchase(
    request: CreatePurchaseRequest,
  ): Observable<CreatePurchaseResult> {
    return this.http.post<
      CreatePurchaseResult
    >(
      '/api/purchases',
      request,
    );
  }
}
