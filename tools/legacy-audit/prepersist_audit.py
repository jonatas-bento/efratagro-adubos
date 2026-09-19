import csv
import re
import unicodedata

from collections import defaultdict
from difflib import get_close_matches
from pathlib import Path


ROWS_PATH = Path(
    "artifacts/legacy-commercial-rows.csv"
)

CATALOG_PATH = Path(
    "artifacts/current-product-catalog.tsv"
)

UNKEYED_PATH = Path(
    "artifacts/legacy-commercial-unkeyed.csv"
)

RESOLUTION_PATH = Path(
    "artifacts/legacy-product-resolution.csv"
)


def normalize(value: str) -> str:
    value = (value or "").strip().upper()

    value = unicodedata.normalize(
        "NFKD",
        value,
    )

    value = "".join(
        ch
        for ch in value
        if not unicodedata.combining(ch)
    )

    value = re.sub(
        r"[^A-Z0-9%+]+",
        " ",
        value,
    )

    return re.sub(
        r"\s+",
        " ",
        value,
    ).strip()


def original_issues(row):
    issues = {
        value
        for value
        in row["Issues"].split("|")
        if value
    }

    # Esses conflitos foram originalmente calculados
    # usando apenas NOTA2. Agora a identidade candidata
    # da transação é NOTA2 + data da venda.
    issues.discard(
        "DOCUMENT_CUSTOMER_CONFLICT"
    )

    issues.discard(
        "DOCUMENT_DATE_CONFLICT"
    )

    return issues


if not ROWS_PATH.exists():
    raise SystemExit(
        f"Arquivo não encontrado: {ROWS_PATH}"
    )

if not CATALOG_PATH.exists():
    raise SystemExit(
        f"Arquivo não encontrado: {CATALOG_PATH}"
    )


with ROWS_PATH.open(
    encoding="utf-8-sig",
    newline="",
) as file:
    rows = list(
        csv.DictReader(
            file,
            delimiter=";",
        )
    )


with CATALOG_PATH.open(
    encoding="utf-8",
    newline="",
) as file:
    catalog_rows = list(
        csv.DictReader(
            file,
            delimiter="\t",
        )
    )


# =========================================================
# CATÁLOGO ATUAL
# =========================================================

catalog = defaultdict(list)

supplier_products = defaultdict(set)

for item in catalog_rows:
    supplier = normalize(
        item["Supplier"]
    )

    names = {
        normalize(
            item["ProductName"]
        )
    }

    legacy_name = (
        item.get("LegacyName") or ""
    ).strip()

    if legacy_name:
        names.add(
            normalize(
                legacy_name
            )
        )

    for name in names:
        catalog[
            (
                supplier,
                name,
            )
        ].append(item)

        supplier_products[
            supplier
        ].add(name)


# =========================================================
# RESOLUÇÃO DOS PRODUTOS LEGADOS
# =========================================================

product_resolution = {}

unresolved_pairs = defaultdict(int)

ambiguous_pairs = defaultdict(int)

for row in rows:
    supplier = normalize(
        row["Sheet"]
    )

    product = normalize(
        row["Product"]
    )

    key = (
        supplier,
        product,
    )

    candidates = catalog.get(
        key,
        [],
    )

    original_pair = (
        row["Sheet"],
        row["Product"],
    )

    if len(candidates) == 1:
        candidate = candidates[0]

        product_resolution[
            original_pair
        ] = (
            "RESOLVED",
            candidate["ProductId"],
            candidate["ProductName"],
            "",
        )

    elif len(candidates) > 1:
        ambiguous_pairs[
            original_pair
        ] += 1

        product_resolution[
            original_pair
        ] = (
            "AMBIGUOUS",
            "",
            "",
            "",
        )

    else:
        unresolved_pairs[
            original_pair
        ] += 1

        suggestions = get_close_matches(
            product,
            sorted(
                supplier_products.get(
                    supplier,
                    set(),
                )
            ),
            n=3,
            cutoff=0.55,
        )

        product_resolution[
            original_pair
        ] = (
            "UNRESOLVED",
            "",
            "",
            " | ".join(
                suggestions
            ),
        )


# =========================================================
# LINHAS QUE NÃO POSSUEM CHAVE DE TRANSAÇÃO
# =========================================================

unkeyed = [
    row
    for row in rows
    if (
        not row["Document"].strip()
        or
        not row["SaleDate"].strip()
    )
]


with UNKEYED_PATH.open(
    "w",
    encoding="utf-8-sig",
    newline="",
) as file:
    fields = [
        "Sheet",
        "Row",
        "Product",
        "Document",
        "SaleDateRaw",
        "SaleDate",
        "Customer",
        "Quantity",
        "UnitPrice",
        "FinancialRaw",
        "Issues",
    ]

    writer = csv.DictWriter(
        file,
        fieldnames=fields,
        delimiter=";",
    )

    writer.writeheader()

    for row in unkeyed:
        writer.writerow({
            field: row.get(
                field,
                "",
            )
            for field in fields
        })


# =========================================================
# RELATÓRIO DE RESOLUÇÃO DE PRODUTO
# =========================================================

with RESOLUTION_PATH.open(
    "w",
    encoding="utf-8-sig",
    newline="",
) as file:
    writer = csv.writer(
        file,
        delimiter=";",
    )

    writer.writerow([
        "Supplier",
        "LegacyProduct",
        "Status",
        "ProductId",
        "CatalogProduct",
        "Occurrences",
        "Suggestions",
    ])

    pairs = sorted({
        (
            row["Sheet"],
            row["Product"],
        )
        for row in rows
    })

    for pair in pairs:
        (
            status,
            product_id,
            catalog_name,
            suggestions,
        ) = product_resolution[
            pair
        ]

        occurrences = sum(
            1
            for row in rows
            if (
                row["Sheet"],
                row["Product"],
            ) == pair
        )

        writer.writerow([
            pair[0],
            pair[1],
            status,
            product_id,
            catalog_name,
            occurrences,
            suggestions,
        ])


# =========================================================
# TRANSAÇÕES CANDIDATAS
#
# A NOTA2 isoladamente NÃO é chave.
#
# LegacyTransactionKey =
#     Document + SaleDate
# =========================================================

transactions = defaultdict(list)

for row in rows:
    document = (
        row["Document"]
        .strip()
        .upper()
    )

    sale_date = (
        row["SaleDate"]
        .strip()
    )

    if (
        not document
        or
        not sale_date
    ):
        continue

    transactions[
        (
            document,
            sale_date,
        )
    ].append(row)


safe_transactions = []

review_transactions = []

for key, items in transactions.items():
    issues = set()

    for item in items:
        issues.update(
            original_issues(
                item
            )
        )

        resolution = (
            product_resolution[
                (
                    item["Sheet"],
                    item["Product"],
                )
            ][0]
        )

        if resolution == "UNRESOLVED":
            issues.add(
                "PRODUCT_UNRESOLVED"
            )

        elif resolution == "AMBIGUOUS":
            issues.add(
                "PRODUCT_AMBIGUOUS"
            )

    customers = {
        item[
            "CanonicalCustomer"
        ].strip()
        for item in items
        if item[
            "CanonicalCustomer"
        ].strip()
    }

    if len(customers) == 0:
        issues.add(
            "TRANSACTION_CUSTOMER_MISSING"
        )

    elif len(customers) > 1:
        issues.add(
            "TRANSACTION_CUSTOMER_CONFLICT"
        )

    transaction = {
        "key": key,
        "items": items,
        "issues": issues,
    }

    if issues:
        review_transactions.append(
            transaction
        )
    else:
        safe_transactions.append(
            transaction
        )


safe_items = sum(
    len(tx["items"])
    for tx in safe_transactions
)

review_items = sum(
    len(tx["items"])
    for tx in review_transactions
)


# =========================================================
# RELATÓRIO
# =========================================================

print(
    "===== FINAL PRE-PERSIST AUDIT ====="
)

print(
    f"Linhas comerciais ............. {len(rows)}"
)

print(
    f"Linhas sem chave .............. {len(unkeyed)}"
)

print(
    f"Transações candidatas ......... {len(transactions)}"
)

print(
    f"Transações prontas ............ {len(safe_transactions)}"
)

print(
    f"Transações retidas ............ {len(review_transactions)}"
)

print(
    f"Itens prontos ................. {safe_items}"
)

print(
    f"Itens retidos ................. {review_items}"
)

print(
    f"Produtos legado distintos ..... {len(product_resolution)}"
)

print(
    f"Produtos não resolvidos ....... {len(unresolved_pairs)}"
)

print(
    f"Produtos ambíguos ............. {len(ambiguous_pairs)}"
)

print()
print(
    "===== PRODUTOS NÃO RESOLVIDOS ====="
)

if not unresolved_pairs:
    print(
        "Nenhum ✅"
    )

for pair, count in sorted(
    unresolved_pairs.items(),
    key=lambda item: (
        item[0][0],
        item[0][1],
    ),
):
    (
        _,
        _,
        _,
        suggestions,
    ) = product_resolution[
        pair
    ]

    print()
    print(
        f"{pair[0]} :: {pair[1]}"
    )

    print(
        f"  ocorrências: {count}"
    )

    print(
        f"  sugestões: "
        f"{suggestions or '-'}"
    )


print()
print(
    "===== PRODUTOS AMBÍGUOS ====="
)

if not ambiguous_pairs:
    print(
        "Nenhum ✅"
    )

for pair, count in sorted(
    ambiguous_pairs.items()
):
    print(
        f"{pair[0]} :: {pair[1]} "
        f"({count} ocorrência(s))"
    )


print()
print(
    "===== LINHAS SEM CHAVE ====="
)

if not unkeyed:
    print(
        "Nenhuma ✅"
    )

for row in unkeyed:
    print()

    print(
        f"{row['Sheet']} "
        f"row {row['Row']}"
    )

    print(
        f"  produto: "
        f"{row['Product']!r}"
    )

    print(
        f"  doc: "
        f"{row['Document']!r}"
    )

    print(
        f"  data raw: "
        f"{row['SaleDateRaw']!r}"
    )

    print(
        f"  cliente: "
        f"{row['Customer']!r}"
    )

    print(
        f"  quantidade: "
        f"{row['Quantity']!r}"
    )

    print(
        f"  unit price: "
        f"{row['UnitPrice']!r}"
    )

    print(
        f"  issues: "
        f"{row['Issues']}"
    )


print()
print(
    "===== TRANSAÇÕES RETIDAS ====="
)

for tx in review_transactions:
    document, sale_date = (
        tx["key"]
    )

    customers = sorted({
        item["Customer"]
        for item in tx["items"]
        if item["Customer"]
    })

    products = sorted({
        (
            item["Sheet"],
            item["Product"],
        )
        for item in tx["items"]
    })

    print()
    print(
        f"{document} @ {sale_date}"
    )

    print(
        f"  itens: "
        f"{len(tx['items'])}"
    )

    print(
        "  clientes: "
        + " | ".join(
            customers
        )
    )

    print(
        "  produtos:"
    )

    for supplier, product in products:
        status = (
            product_resolution[
                (
                    supplier,
                    product,
                )
            ][0]
        )

        print(
            f"    "
            f"{supplier} :: "
            f"{product} "
            f"[{status}]"
        )

    print(
        "  issues: "
        + " | ".join(
            sorted(
                tx["issues"]
            )
        )
    )


print()
print(
    "===== GATE DE ESCRITA ====="
)

print(
    "Customer ............... 0"
)

print(
    "Sale ................... 0"
)

print(
    "SaleItem ............... 0"
)

print(
    "Receivable ............. 0"
)

print(
    "Payment ................ 0"
)

print(
    "InventoryMovement ...... 0"
)

print()
print(
    "Relatórios:"
)

print(
    f"  {UNKEYED_PATH}"
)

print(
    f"  {RESOLUTION_PATH}"
)
