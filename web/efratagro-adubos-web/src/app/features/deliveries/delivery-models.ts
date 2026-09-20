export enum DeliveryStatusCode {
  Unspecified = 0,
  Pending = 1,
  Delivered = 2,
  NotTracked = 3,
}

export interface DeliverySummary {
  saleId: string;
  customerName: string;
  occurredAtUtc: string;
  totalQuantity: number;
  method: string;
  statusCode: DeliveryStatusCode;
  status: string;
  deliveredAtUtc: string | null;
}
