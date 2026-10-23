export type OrderStatus = 'All' | 'Pending' | 'Shipped' | 'Delivered' | 'Cancelled';

export interface OrderSummary {
  orderId: number;
  orderDate: string;
  total: number;
  status: Exclude<OrderStatus, 'All'>;
}

export interface PagedOrders {
  items: OrderSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface OrderFilter {
  customerId: string;
  from?: string;
  to?: string;
  status: OrderStatus;
}


