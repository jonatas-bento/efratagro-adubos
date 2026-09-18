export interface DeliverySummary {
  saleId: string;
  customerName: string;
  occurredAtUtc: string;
  totalQuantity: number;
  method: string;
  status: string;
  deliveredAtUtc: string | null;
}
