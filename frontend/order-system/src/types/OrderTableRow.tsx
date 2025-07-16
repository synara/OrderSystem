import { OrderInput } from './OrderInput';
import { Order } from './Order';

export type OrderTableRow = OrderInput & Partial<Order>;
