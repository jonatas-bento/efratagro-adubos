export interface CreateSaleItemRequest {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateSaleRequest {
  customerName: string;
  customerPhone: string | null;
  items: CreateSaleItemRequest[];
}

export interface CreateSaleResult {
  saleId: string;
  customerId: string;
  customerName: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  occurredAtUtc: string;
}

export interface SaleSummary {
  saleId: string;
  customerName: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  occurredAtUtc: string;
}
