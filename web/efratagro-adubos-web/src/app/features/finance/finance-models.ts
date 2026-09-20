export enum PaymentMethod {
  Pix = 1,
  Cash = 2,
  Card = 3,
  BankTransfer = 4,
  Boleto = 5,
  Check = 6,
  Other = 99,
}

export enum ReceivableStatusCode {
  Pending = 1,
  Partial = 2,
  Overdue = 3,
  PartialOverdue = 4,
  Paid = 5,
}

export interface Receivable {
  id: string;
  saleId: string;
  customerId: string;
  customerName: string;
  installmentNumber: number;
  dueDate: string;
  originalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  statusCode: ReceivableStatusCode;
  status: string;
}

export interface ReceivableSummary {
  totalScheduled: number;
  totalReceived: number;
  totalOutstanding: number;
  totalOverdue: number;
  openInstallments: number;
  overdueInstallments: number;
}

export interface ReceivablesResult {
  summary: ReceivableSummary;
  items: Receivable[];
}

export interface RegisterPaymentRequest {
  amount: number;
  method: PaymentMethod;
  reference: string | null;
  notes: string | null;
}
