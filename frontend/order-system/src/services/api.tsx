import axios from 'axios';
import { OrderInput } from '../types/OrderInput';
import { Order } from '../types/Order';

const api = axios.create({
  baseURL: 'http://localhost:5164/api',
  timeout: 10000,
});

export const sendOrder = async (data: OrderInput): Promise<Order> => {
  const response = await api.post<Order>('/order', data);
  return response.data;
};
