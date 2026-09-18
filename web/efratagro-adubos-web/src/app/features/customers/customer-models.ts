export interface CustomerListItem {
  id: string;
  name: string;
  phone: string | null;
  salesCount: number;
  totalPurchased: number;
  totalQuantity: number;
  lastPurchaseAtUtc: string | null;
  totalReceived: number;
  outstandingAmount: number;
  overdueAmount: number;
  salesWithoutFinancialSchedule: number;
}

export interface CustomerFinancialSummary {
  totalReceivables: number;
  totalReceived: number;
  outstandingAmount: number;
  overdueAmount: number;
  openInstallments: number;
  overdueInstallments: number;
  salesWithoutFinancialSchedule: number;
}

export interface CustomerProductSummary {
  productId: string;
  productName: string;
  totalQuantity: number;
  totalValue: number;
}

export interface CustomerPurchaseProduct {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalValue: number;
}

export interface CustomerPurchase {
  saleId: string;
  occurredAtUtc: string;
  origin: string;
  items: number;
  totalQuantity: number;
  totalValue: number;
  products: CustomerPurchaseProduct[];
}

export interface CustomerDetails {
  id: string;
  name: string;
  phone: string | null;
  salesCount: number;
  totalPurchased: number;
  totalQuantity: number;
  lastPurchaseAtUtc: string | null;
  financial: CustomerFinancialSummary;
  products: CustomerProductSummary[];
  purchases: CustomerPurchase[];
}

export interface CreateCustomerRequest {
  name: string;
  phone: string | null;
}
