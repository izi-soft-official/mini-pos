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


@app.post("/assistant", response_model=AssistantResponse)
def assistant(request: AssistantRequest):

    catalog_text = "\n".join(
        f"{p.id} | {p.name} | SKU: {p.sku}"
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

You NEVER invent product IDs.

You NEVER invent customer IDs.

The backend will validate everything.

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

Return exactly one of:

create_sale
modify_sale
confirm_sale
cancel_sale
unknown

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

IMPORTANT:

For modify_sale, return the COMPLETE resulting sale items.

Do NOT return only the changed item.

For example:

Pending:

Coke x2
Chips x1

User:

"make Coke 3"

Return:

Coke x3
Chips x1

Preserve existing customer, payment method and discount
unless the user changes them.

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

Format:

{{
  "intent": "create_sale",
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
                        "intent parser. Always return valid JSON."
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