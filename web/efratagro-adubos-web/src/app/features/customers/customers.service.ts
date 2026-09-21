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
  CreateCustomerRequest,
  CustomerDetails,
  CustomerListItem,
} from './customer-models';

export interface CustomerTemporalQuery {
  from?: string;
  to?: string;
}

@Injectable({
  providedIn: 'root',
})
export class CustomersService {
  private readonly http =
    inject(HttpClient);

  getCustomers(
    search = '',
    take = 100,
    period: CustomerTemporalQuery = {},
  ): Observable<CustomerListItem[]> {
    let params =
      new HttpParams()
        .set(
          'take',
          take,
        );

    if (search.trim()) {
      params =
        params.set(
          'q',
          search.trim(),
        );
    }

    if (period.from) {
      params =
        params.set(
          'from',
          period.from,
        );
    }

    if (period.to) {
      params =
        params.set(
          'to',
          period.to,
        );
    }

    return this.http.get<CustomerListItem[]>(
      '/api/customers',
      { params },
    );
  }

  getCustomer(
    id: string,
    period: CustomerTemporalQuery = {},
  ): Observable<CustomerDetails> {
    let params =
      new HttpParams();

    if (period.from) {
      params =
        params.set(
          'from',
          period.from,
        );
    }

    if (period.to) {
      params =
        params.set(
          'to',
          period.to,
        );
    }

    return this.http.get<CustomerDetails>(
      `/api/customers/${id}`,
      { params },
    );
  }

  createCustomer(
    request: CreateCustomerRequest,
  ): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      '/api/customers',
      request,
    );
  }
}
