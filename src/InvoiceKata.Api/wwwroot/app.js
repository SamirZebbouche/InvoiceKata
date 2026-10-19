// The demo front: plain JavaScript, no framework, no build. It only calls the API's routes (docs/API.md).
// The API has no list routes, so the page keeps what it creates in the browser's local storage.
"use strict";

const storeKey = "invoicekata-demo";
const money = new Intl.NumberFormat("fr-FR", { style: "currency", currency: "EUR" });
const number = new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 3 });
const $ = (id) => document.getElementById(id);

let state = load();

function load() {
  try {
    return JSON.parse(localStorage.getItem(storeKey)) ?? {};
  } catch {
    return {};
  }
}

function save() {
  try {
    localStorage.setItem(storeKey, JSON.stringify(state));
  } catch {
    // Private browsing: the demo still works, it is just forgotten on reload.
  }
}

// Calls the API; a refusal comes back as Problem Details (RFC 9457) and is shown as is.
async function api(method, path, body) {
  const response = await fetch(path, {
    method,
    headers: body === undefined ? {} : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) {
    const errors = data?.errors ? Object.values(data.errors).flat().join(" ") : "";
    throw new Error(`${response.status} — ${data?.title ?? "Erreur"}${data?.detail ? " : " + data.detail : ""}${errors ? " " + errors : ""}`);
  }
  return { data, location: response.headers.get("Location") };
}

function idFrom(location) {
  return location.split("/").pop();
}

function show(text, isError = false) {
  const message = $("message");
  message.textContent = text;
  message.className = isError ? "message error" : "message";
  message.hidden = false;
}

async function run(action, success) {
  try {
    await action();
    if (success) show(success);
  } catch (error) {
    show(error.message, true);
  }
  render();
}

function fields(form) {
  return Object.fromEntries(new FormData(form).entries());
}

function decimal(value) {
  return value === "" || value === undefined ? undefined : Number(value);
}

function date(iso) {
  const [year, month, day] = iso.split("-");
  return `${day}/${month}/${year}`;
}

// ---- Steps ----

$("issuer-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = fields(event.target);
  run(async () => {
    const { location } = await api("POST", "/issuers", {
      name: form.name,
      legalForm: form.legalForm,
      shareCapital: decimal(form.shareCapital),
      siren: form.siren,
      vatNumber: form.vatNumber,
      address: { street: form.street, postalCode: form.postalCode, city: form.city },
    });
    state = { issuerId: idFrom(location), issuerName: form.name };
    save();
  }, "Émetteur configuré.");
});

$("customer-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = fields(event.target);
  const professional = form.kind === "Professional";
  run(async () => {
    const { location } = await api("POST", `/issuers/${state.issuerId}/customers`, {
      kind: form.kind,
      name: form.name,
      siren: professional ? form.siren : undefined,
      vatNumber: professional ? form.vatNumber : undefined,
      address: { street: form.street, postalCode: form.postalCode, city: form.city },
    });
    state = { ...state, customerId: idFrom(location), customerName: form.name, quoteId: undefined, quote: undefined, invoice: undefined };
    save();
  }, "Client enregistré.");
});

$("create-quote").addEventListener("click", () =>
  run(async () => {
    const { data } = await api("POST", `/issuers/${state.issuerId}/quotes`, { customerId: state.customerId });
    state = { ...state, quoteId: data.id, invoice: undefined };
    await refreshQuote();
  }, "Devis brouillon créé."));

$("line-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = fields(event.target);
  run(async () => {
    const { data } = await api("POST", `/issuers/${state.issuerId}/quotes/${state.quoteId}/lines`, {
      designation: form.designation,
      quantity: decimal(form.quantity),
      unit: form.unit,
      unitPriceExcludingVat: decimal(form.unitPriceExcludingVat),
      vatRate: decimal(form.vatRate),
      discountRate: decimal(form.discountRate) || undefined,
    });
    state.quote = data;
    save();
  }, "Ligne ajoutée : les totaux et les mentions sont recalculés.");
});

$("finalize-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = fields(event.target);
  run(async () => {
    const { data } = await api("POST", `/issuers/${state.issuerId}/quotes/${state.quoteId}/finalization`, { validityDays: decimal(form.validityDays) });
    state.quote = data;
    save();
  }, "Devis finalisé : il est figé, et son PDF est rendu.");
});

$("invoice-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = fields(event.target);
  run(async () => {
    const { data } = await api("POST", `/issuers/${state.issuerId}/invoices`, { quoteId: state.quoteId, paymentDays: decimal(form.paymentDays) });
    state.invoice = data;
    save();
  }, "Facture émise : son PDF est rendu.");
});

// An individual has no SIREN nor VAT number: the fields are disabled, and not sent.
$("customer-form").elements.kind.addEventListener("change", (event) => {
  const individual = event.target.value === "Individual";
  for (const name of ["siren", "vatNumber"]) {
    $("customer-form").elements[name].disabled = individual;
  }
});

$("reset").addEventListener("click", () => {
  state = {};
  save();
  $("message").hidden = true;
  render();
});

async function removeLine(lineId) {
  await run(async () => {
    const { data } = await api("DELETE", `/issuers/${state.issuerId}/quotes/${state.quoteId}/lines/${lineId}`);
    state.quote = data;
    save();
  }, "Ligne retirée.");
}

async function refreshQuote() {
  const { data } = await api("GET", `/issuers/${state.issuerId}/quotes/${state.quoteId}`);
  state.quote = data;
  save();
}

// ---- Rendering ----

function lock(stepId, locked) {
  $(stepId).classList.toggle("locked", locked);
}

function totals(element, document) {
  element.replaceChildren(
    row("Total HT", document.totals.excludingVat),
    ...document.vatBreakdown.map((rate) => row(`TVA ${number.format(rate.rate)} % sur ${money.format(rate.baseExcludingVat)}`, rate.vat)),
    row("Total TVA", document.totals.vat),
    row("Total TTC", document.totals.includingVat, "grand"));
}

function row(label, amount, className) {
  const div = document.createElement("div");
  if (className) div.className = className;
  const name = document.createElement("span");
  name.textContent = label;
  const value = document.createElement("span");
  value.textContent = money.format(amount);
  div.append(name, value);
  return div;
}

function mentions(element, list) {
  element.replaceChildren(...list.map((mention) => {
    const item = document.createElement("li");
    item.textContent = mention.text;
    return item;
  }));
}

function cell(text, className) {
  const td = document.createElement("td");
  td.textContent = text;
  if (className) td.className = className;
  return td;
}

function render() {
  lock("customer-step", !state.issuerId);
  lock("quote-step", !state.customerId);
  lock("invoice-step", state.quote?.status !== "Finalized");

  $("issuer-summary").hidden = !state.issuerId;
  $("issuer-summary").textContent = state.issuerId ? `${state.issuerName} — ${state.issuerId}` : "";
  $("customer-summary").hidden = !state.customerId;
  $("customer-summary").textContent = state.customerId ? `${state.customerName} — ${state.customerId}` : "";

  const quote = state.quote;
  $("quote").hidden = !quote;
  if (quote) {
    const finalized = quote.status === "Finalized";
    $("quote-summary").innerHTML = "";
    $("quote-summary").append(
      document.createTextNode(finalized
        ? `Devis ${quote.number} du ${date(quote.issuedOn)}, valable jusqu'au ${date(quote.validUntil)}`
        : "Devis brouillon"),
      Object.assign(document.createElement("span"), { className: finalized ? "chip final" : "chip", textContent: finalized ? "finalisé" : "brouillon" }));

    $("quote-lines").replaceChildren(...quote.lines.map((line) => {
      const tr = document.createElement("tr");
      const remove = document.createElement("td");
      if (!finalized) {
        const button = Object.assign(document.createElement("button"), { type: "button", className: "link", textContent: "Retirer" });
        button.addEventListener("click", () => removeLine(line.id));
        remove.append(button);
      }
      tr.append(
        cell(line.designation), cell(number.format(line.quantity), "num"), cell(line.unit), cell(money.format(line.unitPriceExcludingVat), "num"),
        cell(line.discountRate ? `${number.format(line.discountRate)} %` : "", "num"), cell(money.format(line.totalExcludingVat), "num"),
        cell(`${number.format(line.vatRate)} %`, "num"), remove);
      return tr;
    }));
    totals($("quote-totals"), quote);
    mentions($("quote-mentions"), quote.mentions);
    $("line-form").hidden = finalized;
    $("finalize-form").hidden = finalized;
    $("quote-pdf").hidden = !finalized;
    $("quote-pdf").href = finalized ? `/issuers/${state.issuerId}/quotes/${state.quoteId}/pdf` : "#";
  }

  const invoice = state.invoice;
  $("invoice").hidden = !invoice;
  $("invoice-form").hidden = Boolean(invoice);
  if (invoice) {
    $("invoice-summary").textContent =
      `Facture ${invoice.number} du ${date(invoice.issuedOn)}, d'après le devis ${invoice.quoteNumber}, payable au plus tard le ${date(invoice.dueOn)}`;
    totals($("invoice-totals"), invoice);
    mentions($("invoice-mentions"), invoice.mentions);
    $("invoice-pdf").href = `/issuers/${state.issuerId}/invoices/${invoice.id}/pdf`;
  }
}

render();
