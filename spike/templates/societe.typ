// Template for an issuer of type "société" (SAS, SARL…): quotes and invoices share it.
// It only lays out a resolved document: no sum, no rule, no condition on the legal profile.
// Display rounding is its only arithmetic: amounts arrive exact, as strings, and are read as decimals.

#let data = yaml(sys.inputs.at("data", default: "../data/devis.yaml"))
#let doc = data.document
#let issuer = data.issuer
#let customer = data.customer

#let accent = rgb("#1f4e79")
#let nbsp = "\u{00A0}"
#let thin = "\u{202F}"

#let group-thousands(digits) = {
  let groups = ()
  let rest = digits
  while rest.len() > 3 {
    groups.insert(0, rest.slice(rest.len() - 3))
    rest = rest.slice(0, rest.len() - 3)
  }
  groups.insert(0, rest)
  groups.join(thin)
}

// "1234.5" -> "1 234,50" (display rounding to `digits` decimals, half away from zero).
#let number(value, digits: 2) = {
  let rounded = calc.round(decimal(value), digits: digits)
  let sign = if rounded < decimal("0") { "-" } else { "" }
  let parts = str(calc.abs(rounded)).split(".")
  let fraction = if parts.len() > 1 { parts.at(1) } else { "" }
  fraction = (fraction + "0" * digits).slice(0, digits)
  sign + group-thousands(parts.at(0)) + if digits > 0 { "," + fraction } else { "" }
}

#let money(value) = number(value) + nbsp + "€"
#let quantity(value) = number(value, digits: 3)
#let rate(value) = str(value).replace(".", ",") + nbsp + "%"
#let date(iso) = {
  let (y, m, d) = iso.split("-")
  d + "/" + m + "/" + y
}

#let legal-identity = (
  issuer.name + " " + issuer.legal_form + " au capital de " + number(issuer.capital, digits: 0) + nbsp + "€",
  "SIREN " + issuer.siren + " – RCS " + issuer.rcs,
  "TVA intracommunautaire " + issuer.vat_number,
).join(" · ")

#set document(title: doc.title + " " + doc.number)

// Factur-X: the caller may pass the structured XML of the same document; it is embedded as is.
#if "attachment" in sys.inputs {
  // Factur-X readers look the file up by this exact name, so it must not be the source path.
  pdf.attach(
    "factur-x.xml",
    read(sys.inputs.attachment, encoding: none),
    relationship: "alternative",
    mime-type: "text/xml",
    description: "Factur-X",
  )
}
#set text(font: "Libertinus Serif", size: 9.5pt, lang: "fr")
#set page(
  paper: "a4",
  margin: (top: 3.2cm, bottom: 2.4cm, x: 1.6cm),
  header: context {
    grid(
      columns: (1fr, auto),
      align: (left + bottom, right + bottom),
      [
        #text(size: 16pt, weight: "bold", fill: accent, issuer.name)
        #h(0.4em) #text(fill: accent, issuer.legal_form) \
        #text(size: 8.5pt, issuer.activity)
      ],
      text(size: 8pt)[
        #issuer.address.join(", ") \
        #issuer.phone · #issuer.email
      ],
    )
    v(-0.4em)
    line(length: 100%, stroke: 0.6pt + accent)
  },
  footer: context {
    set text(size: 7.5pt)
    line(length: 100%, stroke: 0.4pt + luma(160))
    grid(
      columns: (1fr, auto),
      legal-identity,
      [#doc.title #doc.number — page #counter(page).display() / #counter(page).final().first()],
    )
  },
)

// Document and customer blocks.
#grid(
  columns: (1fr, 1fr),
  column-gutter: 1cm,
  [
    #text(size: 20pt, weight: "bold", upper(doc.title)) \
    *N°* #doc.number \
    *Date :* #date(doc.issued_on)
    #for reference in doc.at("references", default: ()) [
      \ #text(style: "italic", reference)
    ]
  ],
  block(stroke: 0.5pt + luma(150), inset: 8pt, width: 100%)[
    *#customer.name* \
    #customer.address.join([ \ ])
    #if "siren" in customer [ \ #text(size: 8pt)[SIREN #customer.siren] ]
    #if "vat_number" in customer [ · #text(size: 8pt)[TVA #customer.vat_number] ]
  ],
)

#v(0.6em)
*Objet :* #doc.subject
#if "site_address" in data [ \ *Adresse du chantier :* #data.site_address.join(", ") ]
#v(0.4em)

// Lines, grouped by section; the header row repeats on every page.
#let cell-head(body) = table.cell(fill: accent, text(fill: white, weight: "bold", body))
#table(
  columns: (1fr, auto, auto, auto, auto, auto, auto),
  align: (left, right, center, right, right, right, right),
  stroke: (x, y) => (bottom: 0.3pt + luma(200)),
  inset: (x: 4pt, y: 3.5pt),
  table.header(
    cell-head[Désignation], cell-head[Qté], cell-head[U], cell-head[P.U. HT],
    cell-head[Remise], cell-head[Total HT], cell-head[TVA],
  ),
  ..for section in data.sections {
    (
      table.cell(colspan: 7, fill: luma(235), text(weight: "bold", section.title)),
      ..for l in section.lines {
        (
          l.designation,
          quantity(l.quantity),
          l.unit,
          money(l.unit_price_excl_vat),
          if "discount_rate" in l { rate(l.discount_rate) } else { "" },
          money(l.total_excl_vat),
          rate(l.vat_rate),
        )
      },
    )
  },
)

// Totals, legal mentions and acceptance box form one unbreakable block: they always share a page,
// so the client's signature and the mentions sit next to the price they relate to.
#block(breakable: false, width: 100%)[
  #v(0.6em)
  #align(right, table(
    columns: (auto, auto),
    align: (left, right),
    stroke: none,
    inset: (x: 6pt, y: 3pt),
    [Total HT], money(data.totals.excl_vat),
    ..for b in data.vat_breakdown {
      ([TVA #rate(b.rate) sur #money(b.base_excl_vat)], money(b.vat))
    },
    [Total TVA], money(data.totals.vat),
    table.hline(stroke: 0.6pt + accent),
    table.cell(fill: luma(235))[*Total TTC*], table.cell(fill: luma(235))[*#money(data.totals.incl_vat)*],
  ))

  // Legal mentions, already resolved by the domain.
  #v(0.8em)
  #set text(size: 8.5pt)
  #for mention in data.mentions [
    - #mention
  ]

  #if doc.acceptance_box {
    v(1em)
    block(stroke: 0.5pt + luma(150), inset: 10pt, width: 55%)[
      *Bon pour accord* — date et signature du client, précédées de la mention « Bon pour accord » :
      #v(2.2cm)
    ]
  }
]
