export interface SupplierOption {
  id: string;
  name: string;
}

export interface CreatePurchaseItemRequest {
  productId: string;
  quantity: number;
  unitCost: number;
}

export interface CreatePurchaseRequest {
  supplierId: string;
  items: CreatePurchaseItemRequest[];
}

export interface CreatePurchaseResult {
  purchaseId: string;
  supplierId: string;
  supplierName: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  occurredAtUtc: string;
}

export interface PurchaseSummary {
  purchaseId: string;
  supplierName: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  occurredAtUtc: string;
}
