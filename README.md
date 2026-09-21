# 🌱 EfratAgro Adubos

Sistema web para gestão comercial e operacional de uma distribuidora de insumos agrícolas.

O **EfratAgro Adubos** foi desenvolvido para centralizar processos de **vendas, estoque, reservas de safra, entregas, clientes, compras e controle financeiro**, substituindo fluxos manuais e bases legadas por uma aplicação moderna, rastreável e preparada para evolução.

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" />
  <img src="https://img.shields.io/badge/Angular-Web-DD0031?style=for-the-badge&logo=angular" />
  <img src="https://img.shields.io/badge/TypeScript-Frontend-3178C6?style=for-the-badge&logo=typescript" />
  <img src="https://img.shields.io/badge/EF_Core-Persistence-512BD4?style=for-the-badge&logo=dotnet" />
  <img src="https://img.shields.io/badge/xUnit-Tests-5C2D91?style=for-the-badge" />
</p>

---

## 🎯 Objetivo

O projeto busca digitalizar e organizar a operação da **EfratAgro**, oferecendo uma visão única dos principais processos do negócio.

Entre os objetivos estão:

* registrar vendas com segurança;
* controlar estoque físico e disponibilidade real;
* reservar mercadorias destinadas a vendas futuras;
* acompanhar entregas;
* gerenciar clientes e fornecedores;
* organizar recebimentos e informações financeiras;
* importar e tratar dados provenientes dos sistemas legados;
* criar uma base tecnológica sustentável para novas funcionalidades.

---

## 🚜 Trava / Safra

Um dos principais diferenciais do sistema é o suporte a dois comportamentos distintos de estoque durante uma venda.

### Venda normal

Na venda convencional, o produto é retirado imediatamente do estoque físico.

```text
Estoque físico:     100
Venda:               20
------------------------
Estoque físico:      80
Reservado:            0
Disponível:          80
```

### Trava / Safra

Uma venda de safra pode comprometer uma quantidade do produto para entrega futura sem realizar imediatamente a baixa física.

```text
Estoque físico:     100
Reserva Safra:       30
------------------------
Estoque físico:     100
Reservado:           30
Disponível:          70
```

O sistema passa a trabalhar com três conceitos independentes:

| Conceito       | Descrição                                            |
| -------------- | ---------------------------------------------------- |
| **Físico**     | Quantidade atualmente existente no armazém           |
| **Reservado**  | Quantidade comprometida por vendas futuras           |
| **Disponível** | Quantidade efetivamente disponível para novas vendas |

```text
Disponível = Físico - Reservado
```

Essa separação impede que uma mercadoria já comprometida com outro cliente seja comercializada novamente.

---

## 🔒 Consistência de estoque

As operações de venda e reserva utilizam uma camada de coordenação de estoque para reduzir riscos de inconsistência em operações concorrentes.

O fluxo considera:

```text
Venda
  │
  ├── valida dados comerciais e financeiros
  │
  ├── valida cliente
  │
  ├── bloqueia os produtos envolvidos
  │
  ├── calcula disponibilidade
  │
  ├── verifica estoque disponível
  │
  └── executa:
       │
       ├── Venda normal
       │     └── movimentação física de estoque
       │
       └── Trava / Safra
             └── criação de reserva de estoque
```

A leitura da disponibilidade e a alteração do estoque acontecem dentro da mesma fronteira transacional.

---

## 🧩 Arquitetura

O backend segue uma organização em camadas:

```text
src/
├── EfratAgro.Adubos.Api
├── EfratAgro.Adubos.Application
├── EfratAgro.Adubos.Domain
└── EfratAgro.Adubos.Infrastructure
```

### Domain

Contém as regras e entidades centrais do negócio.

Exemplos:

* vendas;
* estoque;
* reservas;
* financeiro;
* movimentações;
* status e enums do domínio.

### Application

Define contratos, DTOs e modelos utilizados pelos casos de uso da aplicação.

### Infrastructure

Responsável por:

* persistência;
* Entity Framework Core;
* consultas;
* serviços;
* coordenação de estoque;
* integrações com banco de dados.

### API

Expõe os recursos da aplicação para o frontend e demais consumidores.

---

## 🖥️ Frontend

O frontend é desenvolvido em Angular e está localizado em:

```text
web/
└── efratagro-adubos-web/
```

A aplicação possui interface responsiva e módulos organizados por funcionalidade.

Entre as áreas atualmente presentes estão:

* Estoque;
* Vendas;
* Clientes;
* Compras;
* Entregas;
* Financeiro;
* Usuários.

---

## 📦 Estoque

A interface de estoque apresenta uma visão operacional do inventário.

### Resumo

São apresentados indicadores como:

```text
Produtos
Com estoque
Estoque físico
Reservado
Disponível
```

Cada produto também apresenta individualmente:

```text
Físico
Reservado
Disponível
```

A disponibilidade comercial passa a ser considerada no lugar do simples saldo físico.

---

## 💰 Vendas

Durante o registro de uma venda, o operador pode escolher o comportamento de estoque:

```text
Venda normal — baixar agora
Trava / Safra — reservar
```

Essa escolha é enviada para o backend através de `SaleStockMode`.

```text
Immediate
Reserved
```

O comportamento é processado de forma transacional pelo serviço de vendas.

---

## 🚚 Entregas

O módulo de entregas é responsável pela evolução do processo posterior à venda.

As reservas de estoque podem ser relacionadas ao fluxo de entrega, permitindo que a mercadoria previamente comprometida seja tratada de forma consistente quando ocorrer a saída física.

---

## 🗃️ Importação de dados legados

O projeto também possui uma ferramenta dedicada ao tratamento e importação de informações provenientes do ambiente anterior.

```text
tools/
└── EfratAgro.Adubos.LegacyImporter/
```

O importador permite que regras específicas de migração permaneçam separadas da aplicação operacional principal.

---

## 🧪 Testes

O projeto possui testes automatizados para domínio e API.

```text
tests/
├── EfratAgro.Adubos.Domain.Tests
└── EfratAgro.Adubos.Api.Tests
```

Entre os cenários cobertos estão:

* integridade financeira;
* regras de domínio;
* fluxo de reserva de estoque;
* venda com baixa imediata;
* venda com Trava/Safra;
* disponibilidade de estoque;
* comportamento de reservas.

### Estado atual

```text
.NET
91 testes executados
91 testes aprovados
0 falhas

Angular
27 testes executados
27 testes aprovados
0 falhas
```

---

## 🛠️ Tecnologias

### Backend

* .NET 10
* ASP.NET Core
* C#
* Entity Framework Core
* xUnit

### Frontend

* Angular
* TypeScript
* SCSS
* RxJS
* Angular Signals
* Reactive Forms

### Dados e infraestrutura

* banco relacional via Entity Framework Core;
* migrations;
* transações;
* controle concorrente de estoque;
* ferramenta própria de importação de legado.

---

## 📁 Estrutura do projeto

```text
efratagro-adubos/
│
├── src/
│   ├── EfratAgro.Adubos.Api/
│   ├── EfratAgro.Adubos.Application/
│   ├── EfratAgro.Adubos.Domain/
│   └── EfratAgro.Adubos.Infrastructure/
│
├── tests/
│   ├── EfratAgro.Adubos.Api.Tests/
│   └── EfratAgro.Adubos.Domain.Tests/
│
├── tools/
│   └── EfratAgro.Adubos.LegacyImporter/
│
├── web/
│   └── efratagro-adubos-web/
│
└── README.md
```

---

## ▶️ Executando o backend

Clone o repositório:

```bash
git clone https://github.com/jonatas-bento/efratagro-adubos.git

cd efratagro-adubos
```

Restaure as dependências:

```bash
dotnet restore
```

Compile:

```bash
dotnet build
```

Execute os testes:

```bash
dotnet test
```

Execute a API:

```bash
dotnet run \
  --project src/EfratAgro.Adubos.Api
```

---

## 🌐 Executando o frontend

Entre no projeto Angular:

```bash
cd web/efratagro-adubos-web
```

Instale as dependências:

```bash
npm install
```

Execute:

```bash
npm start
```

Para gerar o build:

```bash
npm run build
```

Para executar os testes:

```bash
npm test -- --watch=false
```

---

## 🌿 Branch principal

A branch principal do projeto é:

```text
main
```

Fluxo básico de desenvolvimento:

```bash
git checkout main

git pull

git checkout -b feature/minha-feature
```

Após o desenvolvimento:

```bash
git add .

git commit -m "feat: descrição da funcionalidade"

git push -u origin feature/minha-feature
```

---

## 🗺️ Evolução do projeto

O EfratAgro está em desenvolvimento contínuo.

Algumas frentes naturais de evolução são:

* consolidação completa do fluxo de Trava/Safra;
* acompanhamento operacional das reservas;
* expansão dos fluxos de entrega;
* relatórios gerenciais;
* histórico e auditoria de movimentações;
* indicadores comerciais e financeiros;
* melhorias de experiência do usuário;
* automação de deploy;
* observabilidade e monitoramento.

---

## 🧠 Princípios do projeto

O desenvolvimento busca preservar alguns princípios:

**Consistência antes de conveniência**

Operações financeiras e de estoque devem permanecer íntegras mesmo em cenários concorrentes.

**Domínio explícito**

Conceitos importantes do negócio devem existir claramente no código.

Exemplo:

```text
InventoryReservation
SaleStockMode
InventoryReservationStatus
```

**Evolução incremental**

Funcionalidades são adicionadas em pequenos incrementos testáveis e verificáveis.

**Legado tratado conscientemente**

Dados antigos são migrados através de ferramentas específicas, evitando contaminar o domínio principal com regras temporárias de importação.

---

## 👨‍💻 Desenvolvimento

Projeto desenvolvido e mantido por **Jonatas Bento**.

GitHub:

[@jonatas-bento](https://github.com/jonatas-bento)

---

<p align="center">
  <strong>EfratAgro</strong><br>
  Tecnologia aplicada à gestão do agronegócio. 🌱
</p>
