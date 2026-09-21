"use client";

import { useState } from "react";
import { ai } from "@/lib/api";
import {
  Sparkles,
  Send,
  Loader2,
  Check,
  X,
  Circle,
  UserRound,
  CreditCard,
  Package,
  ShoppingCart,
  AlertTriangle,
  Banknote,
  Receipt,
} from "@/components/Icons";

type Message = {
  role: "user" | "assistant";
  text: string;
};

type Preview = {
  customerName?: string;
  paymentMethod: string;
  subtotal: number;
  discount: number;
  total: number;
  paidAmount: number;
  changeAmount: number;

  items: {
    productId: number;
    sku: string;
    name: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
    availableStock: number;
    insufficientStock: boolean;
  }[];
};

export default function AiAssistant() {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [preview, setPreview] = useState<Preview | null>(null);
  const [busy, setBusy] = useState(false);
  const [activity, setActivity] = useState<string>("");

  async function sendMessage() {
    const value = text.trim();

    if (!value || busy) return;

    setText("");

    setMessages((prev) => [
      ...prev,
      {
        role: "user",
        text: value,
      },
    ]);

    setBusy(true);

    try {
      setActivity("Understanding request");

      await new Promise((resolve) => setTimeout(resolve, 300));

      setActivity("Finding products");

      await new Promise((resolve) => setTimeout(resolve, 300));

      setActivity("Checking stock");

      const response = await ai.assistant(value);

      setActivity("Preparing sale");

      await new Promise((resolve) => setTimeout(resolve, 300));

      if (response.preview) {
        setPreview(response.preview);
      }

      let assistantMessage = response.message;

      if (response.unmatched?.length) {
        assistantMessage +=
          "\n\nI couldn't identify:\n" +
          response.unmatched.map((item) => `• ${item}`).join("\n");
      }

      if (response.warnings?.length) {
        assistantMessage +=
          "\n\nWarnings:\n" +
          response.warnings.map((item) => `• ${item}`).join("\n");
      }

      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          text: assistantMessage,
        },
      ]);

      if (response.saleCreated) {
        setPreview(null);
      }
    } catch (error) {
      console.error(error);

      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          text:
            "I couldn't process that request. " +
            "Please check that the POS API and AI service are running.",
        },
      ]);
    } finally {
      setBusy(false);
      setActivity("");
    }
  }

  function handleKeyDown(
    event: React.KeyboardEvent<HTMLInputElement>
  ) {
    if (event.key === "Enter") {
      event.preventDefault();
      sendMessage();
    }
  }

  return (
    <>
      {/* =====================================================
          FLOATING AI BUTTON
      ====================================================== */}

      <button
        onClick={() => setOpen((prev) => !prev)}
        className="
          fixed
          bottom-6
          right-6
          z-50
          flex
          items-center
          gap-2
          rounded-xl
          bg-blue-600
          px-4
          py-3
          text-sm
          font-semibold
          text-white
          shadow-lg
          shadow-blue-200
          transition
          hover:bg-blue-700
          hover:shadow-xl
        "
      >
        <Sparkles size={18} />
        AI Assistant
      </button>

      {/* =====================================================
          AI ASSISTANT PANEL
      ====================================================== */}

      {open && (
        <div
          className="
            fixed
            bottom-20
            right-6
            z-50
            flex
            h-[680px]
            w-[430px]
            max-w-[calc(100vw-2rem)]
            flex-col
            overflow-hidden
            rounded-2xl
            border
            border-slate-200
            bg-white
            shadow-2xl
          "
        >
          {/* =================================================
              HEADER
          ================================================== */}

          <div
            className="
              flex
              items-center
              justify-between
              border-b
              border-slate-200
              bg-white
              px-5
              py-4
            "
          >
            <div className="flex items-center gap-3">
              <div
                className="
                  flex
                  h-10
                  w-10
                  items-center
                  justify-center
                  rounded-xl
                  bg-blue-50
                  text-blue-600
                "
              >
                <Sparkles size={20} />
              </div>

              <div>
                <h2 className="font-bold text-slate-900">
                  AI Assistant
                </h2>

                <div className="mt-0.5 flex items-center gap-1.5">
                  <Circle
                    size={7}
                    fill="currentColor"
                    className="text-emerald-500"
                  />

                  <p className="text-xs text-slate-500">
                    POS Assistant
                  </p>
                </div>
              </div>
            </div>

            <button
              onClick={() => setOpen(false)}
              aria-label="Close AI Assistant"
              className="
                flex
                h-8
                w-8
                items-center
                justify-center
                rounded-lg
                text-slate-400
                transition
                hover:bg-slate-100
                hover:text-slate-700
              "
            >
              <X size={18} />
            </button>
          </div>

          {/* =================================================
              MESSAGES
          ================================================== */}

          <div
            className="
              flex-1
              space-y-3
              overflow-y-auto
              bg-slate-50
              p-4
            "
          >
            {/* EMPTY STATE */}

            {messages.length === 0 && (
              <div className="space-y-4">
                <div
                  className="
                    rounded-xl
                    border
                    border-slate-200
                    bg-white
                    p-4
                    shadow-sm
                  "
                >
                  <div className="flex items-start gap-3">
                    <div
                      className="
                        flex
                        h-9
                        w-9
                        shrink-0
                        items-center
                        justify-center
                        rounded-lg
                        bg-blue-50
                        text-blue-600
                      "
                    >
                      <Sparkles size={17} />
                    </div>

                    <div>
                      <p className="font-semibold text-slate-900">
                        How can I help?
                      </p>

                      <p className="mt-1 text-sm text-slate-500">
                        Describe a sale naturally and I&apos;ll
                        prepare it for you.
                      </p>
                    </div>
                  </div>
                </div>

                {/* CAPABILITIES */}

                <div className="grid grid-cols-2 gap-2">
                  <div
                    className="
                      rounded-xl
                      border
                      border-slate-200
                      bg-white
                      p-3
                    "
                  >
                    <Package
                      size={17}
                      className="text-blue-600"
                    />

                    <p className="mt-2 text-xs font-medium text-slate-700">
                      Find products
                    </p>
                  </div>

                  <div
                    className="
                      rounded-xl
                      border
                      border-slate-200
                      bg-white
                      p-3
                    "
                  >
                    <UserRound
                      size={17}
                      className="text-violet-600"
                    />

                    <p className="mt-2 text-xs font-medium text-slate-700">
                      Select customer
                    </p>
                  </div>

                  <div
                    className="
                      rounded-xl
                      border
                      border-slate-200
                      bg-white
                      p-3
                    "
                  >
                    <Banknote
                      size={17}
                      className="text-emerald-600"
                    />

                    <p className="mt-2 text-xs font-medium text-slate-700">
                      Handle payment
                    </p>
                  </div>

                  <div
                    className="
                      rounded-xl
                      border
                      border-slate-200
                      bg-white
                      p-3
                    "
                  >
                    <Receipt
                      size={17}
                      className="text-amber-600"
                    />

                    <p className="mt-2 text-xs font-medium text-slate-700">
                      Prepare sale
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* CHAT MESSAGES */}

            {messages.map((message, index) => (
              <div
                key={index}
                className={
                  message.role === "user"
                    ? "ml-10 flex justify-end"
                    : "mr-6 flex justify-start"
                }
              >
                <div
                  className={
                    message.role === "user"
                      ? `
                        max-w-[85%]
                        rounded-2xl
                        rounded-br-md
                        bg-blue-600
                        px-4
                        py-3
                        text-sm
                        text-white
                        shadow-sm
                      `
                      : `
                        max-w-[90%]
                        whitespace-pre-line
                        rounded-2xl
                        rounded-bl-md
                        border
                        border-slate-200
                        bg-white
                        px-4
                        py-3
                        text-sm
                        leading-5
                        text-slate-700
                        shadow-sm
                      `
                  }
                >
                  {message.text}
                </div>
              </div>
            ))}

            {/* =================================================
                ACTIVITY INDICATOR
            ================================================== */}

            {busy && (
              <div className="mr-6 flex justify-start">
                <div
                  className="
                    flex
                    items-center
                    gap-2
                    rounded-2xl
                    rounded-bl-md
                    border
                    border-slate-200
                    bg-white
                    px-4
                    py-3
                    text-xs
                    text-slate-500
                    shadow-sm
                  "
                >
                  <Loader2
                    size={15}
                    className="animate-spin text-blue-600"
                  />

                  <span>{activity}</span>
                </div>
              </div>
            )}

            {/* =================================================
                SALE PREVIEW
            ================================================== */}

            {preview && (
              <div
                className="
                  mt-4
                  overflow-hidden
                  rounded-xl
                  border
                  border-slate-200
                  bg-white
                  shadow-sm
                "
              >
                {/* PREVIEW HEADER */}

                <div
                  className="
                    flex
                    items-center
                    justify-between
                    border-b
                    border-slate-100
                    bg-slate-50
                    px-4
                    py-3
                  "
                >
                  <div className="flex items-center gap-2">
                    <Receipt
                      size={17}
                      className="text-blue-600"
                    />

                    <h3 className="text-sm font-bold text-slate-900">
                      Sale Preview
                    </h3>
                  </div>

                  <span
                    className="
                      flex
                      items-center
                      gap-1
                      rounded-full
                      bg-amber-50
                      px-2.5
                      py-1
                      text-[11px]
                      font-medium
                      text-amber-700
                    "
                  >
                    <Circle
                      size={6}
                      fill="currentColor"
                    />

                    Pending
                  </span>
                </div>

                <div className="p-4">
                  {/* CUSTOMER */}

                  {preview.customerName && (
                    <div
                      className="
                        mb-3
                        flex
                        items-center
                        gap-2
                        text-sm
                      "
                    >
                      <UserRound
                        size={15}
                        className="text-slate-400"
                      />

                      <span className="text-slate-400">
                        Customer
                      </span>

                      <span className="font-medium text-slate-800">
                        {preview.customerName}
                      </span>
                    </div>
                  )}

                  {/* PAYMENT */}

                  <div
                    className="
                      mb-4
                      flex
                      items-center
                      gap-2
                      text-sm
                    "
                  >
                    <CreditCard
                      size={15}
                      className="text-slate-400"
                    />

                    <span className="text-slate-400">
                      Payment
                    </span>

                    <span className="font-medium capitalize text-slate-800">
                      {preview.paymentMethod}
                    </span>
                  </div>

                  {/* PRODUCTS */}

                  <div className="space-y-3">
                    {preview.items.map((item) => (
                      <div
                        key={item.productId}
                        className="
                          flex
                          items-start
                          justify-between
                          gap-3
                        "
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <Package
                              size={14}
                              className="shrink-0 text-slate-400"
                            />

                            <span className="truncate text-sm font-medium text-slate-800">
                              {item.name}
                            </span>
                          </div>

                          <div className="ml-5 mt-1 text-xs text-slate-400">
                            {item.quantity} ×{" "}
                            {item.unitPrice.toFixed(2)} DZD
                          </div>

                          {item.insufficientStock && (
                            <div
                              className="
                                ml-5
                                mt-1
                                flex
                                items-center
                                gap-1
                                text-xs
                                text-red-600
                              "
                            >
                              <AlertTriangle size={12} />

                              <span>
                                Only {item.availableStock} in stock
                              </span>
                            </div>
                          )}
                        </div>

                        <span className="shrink-0 text-sm font-semibold text-slate-800">
                          {item.lineTotal.toFixed(2)} DZD
                        </span>
                      </div>
                    ))}
                  </div>

                  {/* TOTALS */}

                  <div
                    className="
                      mt-4
                      space-y-2
                      border-t
                      border-slate-100
                      pt-4
                    "
                  >
                    <div className="flex justify-between text-sm text-slate-500">
                      <span>Subtotal</span>

                      <span>
                        {preview.subtotal.toFixed(2)} DZD
                      </span>
                    </div>

                    <div className="flex justify-between text-sm text-slate-500">
                      <span>Discount</span>

                      <span>
                        -{preview.discount.toFixed(2)} DZD
                      </span>
                    </div>

                    <div
                      className="
                        flex
                        items-center
                        justify-between
                        border-t
                        border-slate-100
                        pt-2
                      "
                    >
                      <span className="text-sm font-bold text-slate-900">
                        Total
                      </span>

                      <span className="text-lg font-black text-blue-600">
                        {preview.total.toFixed(2)} DZD
                      </span>
                    </div>

                    {preview.paymentMethod === "cash" && (
                      <div className="flex justify-between text-xs text-slate-400">
                        <span>Change</span>

                        <span>
                          {preview.changeAmount.toFixed(2)} DZD
                        </span>
                      </div>
                    )}
                  </div>

                  {/* CONFIRMATION INFO */}

                  <div
                    className="
                      mt-4
                      flex
                      items-center
                      gap-2
                      rounded-lg
                      border
                      border-blue-100
                      bg-blue-50
                      px-3
                      py-2.5
                      text-xs
                      text-blue-700
                    "
                  >
                    <Check
                      size={15}
                      className="shrink-0"
                    />

                    <span>
                      Send <strong>confirm</strong> to complete
                      this sale.
                    </span>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* =================================================
              INPUT AREA
          ================================================== */}

          <div
            className="
              border-t
              border-slate-200
              bg-white
              p-3
            "
          >
            <div className="flex gap-2">
              <input
                value={text}
                onChange={(e) => setText(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={busy}
                placeholder="Sell 2 Coke to Ahmed..."
                className="
                  h-11
                  min-w-0
                  flex-1
                  rounded-xl
                  border
                  border-slate-200
                  bg-slate-50
                  px-4
                  text-sm
                  text-slate-900
                  outline-none
                  transition
                  placeholder:text-slate-400
                  focus:border-blue-500
                  focus:bg-white
                  focus:ring-2
                  focus:ring-blue-100
                  disabled:cursor-not-allowed
                  disabled:opacity-60
                "
              />

              <button
                onClick={sendMessage}
                disabled={busy || !text.trim()}
                aria-label="Send message"
                className="
                  flex
                  h-11
                  w-11
                  shrink-0
                  items-center
                  justify-center
                  rounded-xl
                  bg-blue-600
                  text-white
                  shadow-sm
                  transition
                  hover:bg-blue-700
                  disabled:cursor-not-allowed
                  disabled:opacity-40
                "
              >
                {busy ? (
                  <Loader2
                    size={18}
                    className="animate-spin"
                  />
                ) : (
                  <Send size={18} />
                )}
              </button>
            </div>

            <p className="mt-2 text-center text-[10px] text-slate-400">
              AI prepares the sale. You confirm before anything
              is created.
            </p>
          </div>
        </div>
      )}
    </>
  );
}