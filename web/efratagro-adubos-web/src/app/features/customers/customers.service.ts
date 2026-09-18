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

@Injectable({
  providedIn: 'root',
})
export class CustomersService {
  private readonly http =
    inject(HttpClient);

  getCustomers(
    search = '',
    take = 100,
  ): Observable<CustomerListItem[]> {
    let params =
      new HttpParams()
        .set('take', take);

    if (search.trim()) {
      params =
        params.set(
          'q',
          search.trim(),
        );
    }

    return this.http.get<CustomerListItem[]>(
      '/api/customers',
      { params },
    );
  }

  getCustomer(
    id: string,
  ): Observable<CustomerDetails> {
    return this.http.get<CustomerDetails>(
      `/api/customers/${id}`,
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
