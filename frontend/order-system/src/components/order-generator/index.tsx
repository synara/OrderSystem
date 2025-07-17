import React, { useState, useEffect } from 'react';
import {
  Layout,
  Typography,
  InputNumber,
  Button,
  Select,
  Table,
  message,
  Row,
  Col,
  Tag,
} from 'antd';
import { sendOrder } from '../../services/api';
import { OrderTableRow } from '../../types/OrderTableRow';
import { OrderInput } from '../../types/OrderInput';

import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';

const { Header, Content } = Layout;
const { Title } = Typography;
const { Option } = Select;

export default function OrderGenerator() {
  const [loading, setLoading] = useState(false);
  const [orders, setOrders] = useState<OrderTableRow[]>([]);
  const [symbol, setSymbol] = useState('');
  const [side, setSide] = useState('');
  const [price, setPrice] = useState<number | undefined>(undefined);
  const [quantity, setQuantity] = useState<number | undefined>(undefined);

  useEffect(() => {
    const storedOrders = localStorage.getItem('orders');
    if (storedOrders) {
      try {
        setOrders(JSON.parse(storedOrders));
      } catch {
        localStorage.removeItem('orders');
      }
    }
  }, []);
  
  const updateOrders = (newOrders: OrderTableRow[] | ((prev: OrderTableRow[]) => OrderTableRow[])) => {
    setOrders((prev) => {
      const updatedOrders = typeof newOrders === 'function' ? newOrders(prev) : newOrders;
      localStorage.setItem('orders', JSON.stringify(updatedOrders));
      return updatedOrders;
    });
  };

  const newOrder = async () => {
    if (!symbol || !side || !price || !quantity) {
      message.error('Preencha todos os campos');
      return;
    }
    const input: OrderInput = { symbol, side, price, quantity };

    updateOrders((prev) => [{ ...input, message: 'Enviando...' }, ...prev]);

    setLoading(true);
    try {
      const result = await sendOrder(input);
      updateOrders((prev) => {
        const updated = [...prev];
        updated[0] = { ...input, ...result };
        return updated;
      });

    } catch (error: any) {
      const serverError =
        error?.response?.data?.message ||
        error?.message ||
        'Erro ao enviar ordem';

      updateOrders((prev) => {
        const updated = [...prev];
        updated[0] = {
          ...input,
          message: serverError,
          success: false,
        };
        return updated;
      });

    } finally {
      setLoading(false);
      setSymbol('');
      setSide('');
      setPrice(undefined);
      setQuantity(undefined);
    }
  };

  const columns = [
    { title: 'Símbolo', dataIndex: 'symbol', key: 'symbol' },
    { title: 'Lado', dataIndex: 'side', key: 'side' },
    { title: 'Preço', dataIndex: 'price', key: 'price',  render: (price: number) => `R$${price.toFixed(2)}` },
    { title: 'Quantidade', dataIndex: 'quantity', key: 'quantity' },
    {
      title: 'ID da ordem',
      dataIndex: 'orderId',
      key: 'orderId',
      render: (orderId: string | undefined) => orderId ?? 'S/N',
    },
    {
      title: 'Mensagem',
      dataIndex: 'message',
      key: 'message',
      render: (_: string, record: OrderTableRow) => {
        if (record.success === true) {
          return (
            <Tag icon={<CheckCircleOutlined />} color="success">
              {record.message}
            </Tag>
          );
        }
        if (record.success === false) {
          return (
            <Tag icon={<CloseCircleOutlined />} color="error">
              {record.message}
            </Tag>
          );
        }
        return (
          <Tag icon={<ClockCircleOutlined />} color="processing">
            {record.message || 'Enviando...'}
          </Tag>
        );
      },
    },
  ];

  const simbolos = ['VALE3', 'VIIA4', 'PETR4'];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ background: '#a7a6a6', padding: '0 24px' }}>
        <Title level={3} style={{ color: '#000', lineHeight: '64px', margin: 0 }}>
          Gerador de Ordens
        </Title>
      </Header>

      <Content style={{ padding: '24px 48px' }}>
        <Row gutter={16} align="bottom">
          <Col span={5}>
            <label style={{ display: 'block', marginBottom: 4 }}>Símbolo</label>
            <Select
              placeholder="Selecione"
              style={{ width: '100%' }}
              value={symbol || undefined}
              onChange={setSymbol}
            >
              {simbolos.map((s) => (
                <Option key={s} value={s}>
                  {s}
                </Option>
              ))}
            </Select>
          </Col>

          <Col span={5}>
            <label style={{ display: 'block', marginBottom: 4 }}>Lado</label>
            <Select
              placeholder="Compra ou Venda"
              style={{ width: '100%' }}
              value={side || undefined}
              onChange={setSide}
            >
              <Option value="Compra">Compra</Option>
              <Option value="Venda">Venda</Option>
            </Select>
          </Col>

          <Col span={5}>
            <label style={{ display: 'block', marginBottom: 4 }}>Preço</label>
            <InputNumber
              placeholder="Ex.: R$13.55"
              style={{ width: '100%' }}
              value={price}
              step={0.1}
              onChange={(v) => setPrice(v === null ? undefined : v)}
            />
          </Col>

          <Col span={5}>
            <label style={{ display: 'block', marginBottom: 4 }}>Quantidade</label>
            <InputNumber
              placeholder="Ex: 100"
              style={{ width: '100%' }}
              value={quantity}
              onChange={(v) => setQuantity(v === null ? undefined : v)}
            />
          </Col>

          <Col span={4}>
            <label style={{ display: 'block', visibility: 'hidden' }}>Enviar</label>
            <Button
              type="primary"
              block
              onClick={newOrder}
              disabled={!symbol || !side || !price || !quantity}
              loading={loading}
            >
              Enviar
            </Button>
          </Col>
        </Row>

        <Table<OrderTableRow>
          dataSource={orders}
          columns={columns}
          rowKey={(record, index) =>
            record.orderId ?? (index !== undefined ? index.toString() : Math.random().toString())
          }
          pagination={{ pageSize: 10 }}
          bordered
          style={{ marginTop: 32 }}
        />
      </Content>
    </Layout>
  );
}
