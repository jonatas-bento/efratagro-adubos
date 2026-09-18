export enum DeliveryMethod {
  Delivery = 1,
  StorePickup = 2,
  WarehousePickup = 3,
}

export interface CreateSaleItemRequest {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateSaleReceivableRequest {
  installmentNumber: number;
  dueDate: string;
  amount: number;
}

export interface CreateSaleRequest {
  customerId: string;
  deliveryMethod: DeliveryMethod;
  items: CreateSaleItemRequest[];
  receivables: CreateSaleReceivableRequest[];
}

export interface CreateSaleResult {
  saleId: string;
  customerId: string;
  customerName: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  occurredAtUtc: string;
  deliveryMethod: DeliveryMethod;
  deliveryStatus: number;
  receivables: number;
  scheduledAmount: number;
}

export interface SaleSummary {
  saleId: string;
  customerName: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  occurredAtUtc: string;
}

export type PaymentCondition =
  | 'cash'
  | 'installments';

export interface FinancialInstallmentPreview {
  installmentNumber: number;
  dueDate: string;
  amount: number;
}
