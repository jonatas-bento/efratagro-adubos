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
  PaymentHistoryItem,
  ReceivablesResult,
  RegisterPaymentRequest,
  ReversePaymentRequest,
  ReversePaymentResult,
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

  getPayments(
    receivableId: string,
  ): Observable<PaymentHistoryItem[]> {
    return this.http.get<
      PaymentHistoryItem[]
    >(
      `/api/receivables/${receivableId}/payments`,
    );
  }

  reversePayment(
    paymentId: string,
    request: ReversePaymentRequest,
  ): Observable<ReversePaymentResult> {
    return this.http.post<
      ReversePaymentResult
    >(
      `/api/payments/${paymentId}/reversal`,
      request,
    );
  }
}
