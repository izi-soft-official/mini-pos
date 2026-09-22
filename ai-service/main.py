from dotenv import load_dotenv

load_dotenv()

import json
import os

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from groq import Groq


app = FastAPI(title="mini-pos AI service")


# ============================================================
# GROQ
# ============================================================

api_key = os.environ.get("GROQ_API_KEY")

if not api_key:
    raise RuntimeError("GROQ_API_KEY is not configured.")

client = Groq(api_key=api_key)

MODEL = os.environ.get(
    "AI_MODEL",
)


# ============================================================
# HEALTH
# ============================================================

@app.get("/health")
def health():
    return {
        "status": "ok",
        "model": MODEL
    }


# ============================================================
# EXISTING PARSE SALE MODELS
# ============================================================

class CatalogItem(BaseModel):
    id: int
    sku: str
    name: str


class ParseSaleRequest(BaseModel):
    text: str
    catalog: list[CatalogItem]


class ParsedItem(BaseModel):
    productId: int
    quantity: int


class ParseSaleResponse(BaseModel):
    items: list[ParsedItem]
    unmatched: list[str]


# ============================================================
# EXISTING PARSE SALE
# ============================================================

@app.post("/parse-sale", response_model=ParseSaleResponse)
def parse_sale(request: ParseSaleRequest):

    catalog_text = "\n".join(
        f"{p.id}: {p.name} ({p.sku})"
        for p in request.catalog
    )

    prompt = f"""
You are a POS product matching assistant.

Match the cashier request to products from the catalog.

CATALOG:
{catalog_text}

REQUEST:
"{request.text}"

Rules:

1. Only use product IDs that exist in the catalog.
2. Never invent a product ID.
3. If a product cannot be confidently matched, put its name in unmatched.
4. Extract quantities.
5. If no quantity is specified, use quantity 1.
6. Return ONLY valid JSON.

Format:

{{
  "items": [
    {{
      "productId": 123,
      "quantity": 2
    }}
  ],
  "unmatched": []
}}
"""

    try:

        response = client.chat.completions.create(
            model=MODEL,
            max_tokens=1000,
            response_format={"type": "json_object"},
            messages=[
                {
                    "role": "user",
                    "content": prompt
                }
            ],
        )

        content = response.choices[0].message.content

        if not content:
            raise ValueError("Empty AI response.")

        parsed = json.loads(content)

        return ParseSaleResponse(**parsed)

    except Exception as exc:

        raise HTTPException(
            status_code=502,
            detail=f"AI parsing failed: {str(exc)}"
        )


# ============================================================
# AI ASSISTANT
# ============================================================

class AssistantProduct(BaseModel):
    id: int
    sku: str
    name: str
    category: str = "Uncategorized"
    isActive: bool = True


class AssistantCustomer(BaseModel):
    id: int
    name: str


class PendingItem(BaseModel):
    productId: int
    quantity: int


class AssistantRequest(BaseModel):
    text: str

    catalog: list[AssistantProduct]

    customers: list[AssistantCustomer]

    pendingItems: list[PendingItem] = []

    pendingCustomerId: int | None = None

    pendingPaymentMethod: str | None = None

    pendingDiscount: float = 0

    pendingPaidAmount: float | None = None


class AssistantItem(BaseModel):
    productId: int
    quantity: int


class AssistantResponse(BaseModel):
    intent: str

    items: list[AssistantItem] = []

    customerId: int | None = None

    paymentMethod: str | None = None

    discount: float = 0

    paidAmount: float | None = None

    unmatched: list[str] = []

    message: str = ""

    # ========================================================
    # PRODUCT QUERY PARAMETERS
    # ========================================================

    productId: int | None = None

    productIds: list[int] = []

    queryType: str | None = None
    operation: str | None = None
    field: str | None = None
    operator: str | None = None
    value: float | None = None
    category: str | None = None
    status: str | None = None


# ============================================================
# AI ASSISTANT
# ============================================================

@app.post("/assistant", response_model=AssistantResponse)
def assistant(request: AssistantRequest):

    catalog_text = "\n".join(
        f"{p.id} | {p.name} | SKU: {p.sku} | "
        f"Category: {p.category} | Active: {p.isActive}"
        for p in request.catalog
    )

    customers_text = "\n".join(
        f"{c.id} | {c.name}"
        for c in request.customers
    )

    pending_text = json.dumps(
        {
            "items": [
                {
                    "productId": item.productId,
                    "quantity": item.quantity
                }
                for item in request.pendingItems
            ],
            "customerId": request.pendingCustomerId,
            "paymentMethod": request.pendingPaymentMethod,
            "discount": request.pendingDiscount,
            "paidAmount": request.pendingPaidAmount
        },
        ensure_ascii=False
    )

    prompt = f"""
You are the AI assistant of a retail POS system.

Your job is to understand the cashier's natural-language request
and convert it into structured data.

IMPORTANT:

You NEVER create a sale.

You NEVER modify stock.

You NEVER modify products.

You NEVER invent product IDs.

You NEVER invent customer IDs.

The backend will validate everything and retrieve the real
product information from the database.

============================================================
PRODUCT CATALOG
============================================================

{catalog_text}

============================================================
CUSTOMERS
============================================================

{customers_text}

============================================================
CURRENT PENDING SALE
============================================================

{pending_text}

============================================================
CURRENT USER MESSAGE
============================================================

"{request.text}"

============================================================
INTENTS
============================================================

Return exactly ONE of:

create_sale
modify_sale
confirm_sale
cancel_sale
product_query
unknown

Do NOT create separate intents for every wording variation.
Use product_query for read-only product and inventory questions.

============================================================
PRODUCT QUERY
============================================================

Use product_query whenever the user asks for information about
products, inventory, prices, categories, stock, active/inactive
status, or comparisons between products.

The backend will execute the actual database query.

You must understand natural language and convert it to the
appropriate structured query parameters.

Allowed queryType values:

single
search
filter
aggregate
low_stock
out_of_stock

------------------------------------------------------------
SINGLE PRODUCT
------------------------------------------------------------

Use:

queryType = "single"

for questions about one specific product.

Examples:

"How much Coca Cola do we have?"
"How many Coca Cola are left?"
"Do we have Pepsi?"
"Is Pepsi in stock?"
"Show me Coca Cola"
"What is the stock of Milk?"
"What is the price of Chips?"
"Tell me about Coca Cola"

Return the matching catalog ID in productId.

If the product cannot be confidently matched:

productId = null
unmatched = ["human readable product name"]

------------------------------------------------------------
SEARCH / CATEGORY
------------------------------------------------------------

Use:

queryType = "search"

for finding products.

Examples:

"Find Coca Cola"
"Show products containing Coke"
"Find drinks"
"Search for chips"
"Show me snacks"
"Which products are in the drinks category?"

For a name/SKU search, return matching catalog IDs in productIds.

For a category request, return the category name in category.
Use the category exactly as it appears in the catalog when possible.

Do not invent product IDs.

------------------------------------------------------------
FILTERS
------------------------------------------------------------

Use:

queryType = "filter"

for questions involving conditions.

Examples:

"Which products have less than 5 in stock?"
"Show products with more than 20 units."
"Which snacks have 10 units?"
"Show inactive products."

For stock conditions:

field = "stock"
operator = one of:
">", ">=", "<", "<=", "=", "=="
value = the numeric value

For inactive products:

status = "inactive"

For active products:

status = "active"

Category can also be provided when relevant.

------------------------------------------------------------
LOW STOCK
------------------------------------------------------------

Use:

queryType = "low_stock"

for:

"Which products are low in stock?"
"Show low stock products"
"What products are running low?"
"Which products need restocking?"
"Show me products with low inventory"

The backend determines the real threshold and stock values.

Do NOT invent stock values.

------------------------------------------------------------
OUT OF STOCK
------------------------------------------------------------

Use:

queryType = "out_of_stock"

for:

"Which products are out of stock?"
"What products are sold out?"
"Show products with zero stock."

The backend determines the real stock values.

------------------------------------------------------------
AGGREGATE / COMPARISON
------------------------------------------------------------

Use:

queryType = "aggregate"

for questions asking for the maximum or minimum of a product
field.

Allowed field values:

price
stock

Allowed operation values:

max
min

Examples:

"What is the most expensive snack?"
"Which snack costs the most?"
"What's the priciest snack?"

Return:

queryType = "aggregate"
operation = "max"
field = "price"
category = "Snacks"

Examples:

"What is the cheapest drink?"
operation = "min"
field = "price"
category = "Drinks"

Examples:

"Which product has the most stock?"
operation = "max"
field = "stock"

"Which product has the least stock?"
operation = "min"
field = "stock"

Do NOT calculate the answer yourself. The backend queries the
database.

============================================================
IMPORTANT PRODUCT QUERY RULE
============================================================

The LLM is responsible for understanding the user's wording.

The ASP.NET backend is responsible for:

- querying PostgreSQL
- checking the real price
- checking the real stock
- checking active/inactive status
- applying category filters
- applying min/max operations
- returning authoritative product data

Never invent price, stock, status, category, or IDs.

============================================================
CONFIRMATION
============================================================

If the user says:

confirm
confirmed
yes confirm
yes
complete it
finish the sale

and there is a pending sale:

intent = "confirm_sale"

============================================================
CANCELLATION
============================================================

If the user says:

cancel
cancel it
never mind
abort
discard

and there is a pending sale:

intent = "cancel_sale"

============================================================
CREATE SALE
============================================================

For a new request such as:

"Sell 2 Coca Cola and 1 Chips to Ahmed, cash"

return:

intent = "create_sale"

Extract:

- products
- quantities
- customer
- payment method
- discount
- paid amount if explicitly provided

If quantity isn't specified, use 1.

============================================================
MODIFY SALE
============================================================

If there is already a pending sale and the user says:

"make Coke 3"
"Actually add another chips"
"change customer to Ahmed"
"make it card"
"give 5 discount"

then:

intent = "modify_sale"

For modify_sale, return the COMPLETE resulting sale items.

Do NOT return only the changed item.

Preserve existing customer, payment method and discount unless
the user changes them.

============================================================
CUSTOMERS
============================================================

Match the requested customer to the customer list.

Never invent an ID.

If the customer cannot be confidently matched,
return customerId = null.

============================================================
PAYMENT METHODS
============================================================

Normalize:

cash -> cash
card -> card
credit card -> card
izipay -> izipay

If no payment method is specified and there is no pending
payment method, use:

cash

============================================================
DISCOUNT
============================================================

Extract explicit discounts.

Examples:

"10 discount" -> 10
"discount of 5" -> 5
"give him 20 off" -> 20

Do not invent a discount.

============================================================
PAID AMOUNT
============================================================

If the user explicitly says:

"I pay 50"
"customer gives 100"
"paid 200"

extract the amount.

Otherwise preserve the pending paid amount.

For a new cash sale where no amount is specified,
paidAmount can be null.

============================================================
UNMATCHED
============================================================

If a requested product cannot be confidently matched,
put its human-readable name into unmatched.

============================================================
OUTPUT
============================================================

Return ONLY JSON.

For a single product query:

{{
  "intent": "product_query",
  "queryType": "single",
  "productId": 123,
  "productIds": [],
  "operation": null,
  "field": null,
  "operator": null,
  "value": null,
  "category": null,
  "status": null,
  "items": [],
  "customerId": null,
  "paymentMethod": null,
  "discount": 0,
  "paidAmount": null,
  "unmatched": [],
  "message": ""
}}

For "most expensive snack":

{{
  "intent": "product_query",
  "queryType": "aggregate",
  "productId": null,
  "productIds": [],
  "operation": "max",
  "field": "price",
  "operator": null,
  "value": null,
  "category": "Snacks",
  "status": "active",
  "items": [],
  "customerId": null,
  "paymentMethod": null,
  "discount": 0,
  "paidAmount": null,
  "unmatched": [],
  "message": ""
}}

For inactive products:

{{
  "intent": "product_query",
  "queryType": "filter",
  "productId": null,
  "productIds": [],
  "operation": null,
  "field": null,
  "operator": null,
  "value": null,
  "category": null,
  "status": "inactive",
  "items": [],
  "customerId": null,
  "paymentMethod": null,
  "discount": 0,
  "paidAmount": null,
  "unmatched": [],
  "message": ""
}}

For low stock:

{{
  "intent": "product_query",
  "queryType": "low_stock",
  "productId": null,
  "productIds": [],
  "operation": null,
  "field": null,
  "operator": null,
  "value": null,
  "category": null,
  "status": "active",
  "items": [],
  "customerId": null,
  "paymentMethod": null,
  "discount": 0,
  "paidAmount": null,
  "unmatched": [],
  "message": ""
}}

For sales:

{{
  "intent": "create_sale",
  "productId": null,
  "productIds": [],
  "queryType": null,
  "operation": null,
  "field": null,
  "operator": null,
  "value": null,
  "category": null,
  "status": null,
  "items": [
    {{
      "productId": 123,
      "quantity": 2
    }}
  ],
  "customerId": 5,
  "paymentMethod": "cash",
  "discount": 0,
  "paidAmount": null,
  "unmatched": [],
  "message": ""
}}
"""

    try:

        response = client.chat.completions.create(
            model=MODEL,
            max_tokens=1500,
            response_format={"type": "json_object"},
            messages=[
                {
                    "role": "system",
                    "content": (
                        "You are a precise POS transaction "
                        "and product information intent parser. "
                        "Always return valid JSON. "
                        "Never invent IDs."
                    )
                },
                {
                    "role": "user",
                    "content": prompt
                }
            ],
        )

        content = response.choices[0].message.content

        if not content:
            raise ValueError("Empty AI response.")

        parsed = json.loads(content)

        return AssistantResponse(**parsed)

    except Exception as exc:

        raise HTTPException(
            status_code=502,
            detail=f"AI assistant failed: {str(exc)}"
        )