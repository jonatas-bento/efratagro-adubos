# Database checkpoint — 2026-09-19

## Status

Checkpoint validado após:

- importação do estoque inicial;
- importação segura do histórico comercial;
- resolução auditada de 2 linhas históricas;
- isolamento das vendas legadas do fluxo de entregas;
- validação do fluxo operacional de entregas;
- correção do contrato UTC;
- prova de idempotência do importador comercial;
- auditoria relacional;
- backup e restauração completos.

## Banco validado

Produtos: 106

Clientes: 459

Vendas: 524

Itens de venda: 748

Quantidade total dos itens históricos/operacionais:
58183.000

Valor bruto dos itens:
8009119.20

Movimentos de estoque: 67

Saldo físico no ledger:
19183.000

Recebíveis: 3

Valor programado:
300.00

Pagamentos: 1

Valor recebido:
100.00

Lotes de importação legada: 2

Linhas de importação legada: 889

Metadados de vendas legadas: 522

Linhas comerciais ainda em revisão: 42

Linhas comerciais vinculadas a SaleItem: 746

## Entregas

Venda operacional pendente: 1

Venda operacional entregue: 1

Vendas legadas não rastreadas: 522

Nenhuma venda legada está vinculada ao workflow
operacional de entregas.

## Auditoria relacional

OrphanMetadata: 0

OrphanLinkedRows: 0

LegacyMetadataWrongOrigin: 0

LegacySalesTrackedDelivery: 0

## Importação comercial

Batch:

8391ecaf-52c4-4bad-8b7c-0baeff1fc98e

ImportedRows: 788

ReviewRows no momento da importação: 44

Reviews ainda abertas: 42

A reexecução do importador comercial com o mesmo
arquivo e escopo não alterou o estado persistido.

Idempotency state gate: PASSED.

## Migrations

Quantidade aplicada: 10

Última migration:

20260919145202_AddLegacyReviewResolutionAudit

## Backup validado

Arquivo:

artifacts/backups/efratagro-adubos-20260919-132009.sql.gz

SHA-256:

6a9425d6433df4796ac876dbf4ea0522b66c505e1c0f9ea37d53e1165aa8723f

O arquivo passou por:

- gzip integrity check;
- restauração em banco temporário;
- comparação lógica entre origem e restauração.

Backup restore gate: PASSED.

O banco temporário utilizado para validação foi removido
após o teste.

## Invariantes

O histórico comercial legado não gera movimentos de estoque.

O histórico comercial legado não gera recebíveis ou pagamentos.

Vendas legadas usam DeliveryStatus.NotTracked.

Dados brutos de importação permanecem preservados.

Correções históricas são auditáveis.

Datas UTC são expostas pela API com semântica UTC explícita.

