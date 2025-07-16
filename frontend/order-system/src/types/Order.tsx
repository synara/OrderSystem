import { OrderInput } from "./OrderInput";

export interface Order extends OrderInput {
  message: string;
  success: boolean;
  orderId: string;
}

