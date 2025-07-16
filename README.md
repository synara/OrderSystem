
# 📈 Order System - FIX 4.4 com .NET 6 e React

Usando a biblioteca [QuickFIX/n](https://quickfixn.org/), este projeto simula um sistema de envio e recebimento de ordens financeiras utilizando o protocolo **FIX 4.4**, com dois serviços principais desenvolvidos em **.NET 6**.

## 🧩 Componentes

### 🟦 OrderGenerator (Initiator)

- API Web (C# ASP.NET Core) que permite envio de ordens de compra/venda.
- Interface em React, usando Axios e Antd.
- Envia mensagens `NewOrderSingle` via FIX.
- Aguarda `ExecutionReport` de retorno (usando `TaskCompletionSource`).

### 🟨 OrderAccumulator (Acceptor)

- Servidor que processa as ordens recebidas.
- Verifica a exposição por símbolo e aceita/rejeita com base em limite de **R$ 100.000.000** por ativo.
- Retorna `ExecutionReport` de aceitação ou rejeição.

---

## ⚙️ Requisitos

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Node.js
- Visual Studio 2022 ou VS Code

---

## ▶️ Executando o Projeto

### 1. Clonar e navegar:

```bash
git clone https://github.com/synara/OrderSystem.git
cd OrderSystem
```

### 2. Restaurar e compilar backend

```bash
cd backend/OrderGenerator
dotnet restore
dotnet build

cd ../OrderAccumulator
dotnet restore
dotnet build
```

### 3. Rodar ambos os serviços

Use dois terminais separados:

```bash
# Acceptor
cd backend/OrderAccumulator
dotnet run

# Initiator (interface de envio)
cd backend/OrderGenerator
dotnet run
```

- A API estará disponível em: `https://localhost:5164/swagger`.
- **Observação importante:** O Accumulator (acceptor) **sempre** precisa rodar antes do Generator (initiator). Isso é uma obrigatoriedade do protocolo FIX, onde o acceptor inicia "escutando" conexões e o initiator já sobe buscando pela porta/IP do acceptor. Caso a ordem não seja obedecida, o initiator vai falhar por não encontrar essa configuração ativa.

### 4. Rodar front
```bash
cd frontend
npm install
npm start
```
- A interface estará disponível em `http://localhost:3000`.
- Requer que o backend (OrderGenerator) esteja rodando em `https://localhost:5164`.

---

## 📬 Enviando Ordens (POST /api/order)

### Exemplo de requisição JSON:

```json
{
  "symbol": "VALE3",
  "side": "Compra",
  "price": 100,
  "quantity": 200000
}
```

### Exemplo de resposta esperada:

```json
{
  "orderId": "123e4567-e89b-12d3-a456-426614174000",
  "message": "Ordem recebida e aceita.",
  "success": true
}
```

---

## 🔒 Regras de Negócio

- O limite de exposição por símbolo (ex: `VALE3`) é **R$ 100.000.000**.
- A exposição é calculada como: `preço × quantidade × (1 ou -1)` dependendo do lado (compra ou venda).
- Se a nova exposição ultrapassar o limite, a ordem é **rejeitada**.
- A resposta sempre chega via mensagem `ExecutionReport`.

---

## 🧪 Testes

- Projeto `OrderAccumulatorTest`: Testa regra de cálculo de exposição.
- Projeto `OrderGeneratorTest`: Mock de sessões FIX para simular retorno.

### Rodar os testes:

```bash
dotnet test
```

---

## 🗂 Estrutura do Projeto

```
OrderSystem/
├── backend/
│   ├── OrderGenerator/         # Envia ordens via FIX
│   ├── OrderAccumulator/       # Recebe e valida ordens
│   └── FIX/                    # Arquivos .cfg de sessão FIX
├── frontend/                   # React UI 
└── README.md                   
```

## 📌 Observações

- O `OrderGenerator` aguarda a resposta da ordem de forma assíncrona com `TaskCompletionSource`.
- Se a resposta não chegar em 10 segundos, um `TimeoutException` é lançado.
- Logs detalhados podem ser verificados nas pastas `logs/` e `store/`.

---

## 🛠 Tecnologias Utilizadas

- ASP.NET Core 6
- QuickFIX/n (1.13+)
- C#
- xUnit
- React.js + TypeScript
- Antd Design
- Axios   
