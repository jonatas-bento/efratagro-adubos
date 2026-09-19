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
  ReceivablesResult,
  RegisterPaymentRequest,
} from './finance-models';

@Injectable({
  providedIn: 'root',
})
export class FinanceService {
  private readonly http =
    inject(HttpClient);

  getReceivables(
    openOnly: boolean,
  ): Observable<ReceivablesResult> {
    const params =
      new HttpParams()
        .set(
          'openOnly',
          openOnly,
        );

    return this.http.get<ReceivablesResult>(
      '/api/receivables',
      { params },
    );
  }

  registerPayment(
    receivableId: string,
    request: RegisterPaymentRequest,
  ): Observable<unknown> {
    return this.http.post(
      `/api/receivables/${receivableId}/payments`,
      request,
    );
  }
}
