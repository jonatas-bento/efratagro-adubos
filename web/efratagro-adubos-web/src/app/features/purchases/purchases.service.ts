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
  CreatePurchaseRequest,
  CreatePurchaseResult,
  PurchaseSummary,
  SupplierOption,
} from './purchase-models';

@Injectable({
  providedIn: 'root',
})
export class PurchasesService {
  private readonly http =
    inject(HttpClient);

  getSuppliers():
      Observable<SupplierOption[]> {
    return this.http.get<SupplierOption[]>(
      '/api/suppliers',
    );
  }

  getRecentPurchases(
    take = 20,
  ): Observable<PurchaseSummary[]> {
    const params =
      new HttpParams()
        .set(
          'take',
          take,
        );

    return this.http.get<PurchaseSummary[]>(
      '/api/purchases',
      { params },
    );
  }

  createPurchase(
    request: CreatePurchaseRequest,
  ): Observable<CreatePurchaseResult> {
    return this.http.post<CreatePurchaseResult>(
      '/api/purchases',
      request,
    );
  }
}
