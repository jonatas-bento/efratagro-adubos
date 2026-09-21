export interface InventoryItem {
  productId: string;
  productName: string;
  supplierName: string;
  isActive: boolean;
  quantity: number;
  reservedQuantity?: number;
  availableQuantity?: number;
}
