# Roadmap

Chaque **PR** laisse un système fonctionnel : l'API démarre, et la suite de tests, la CI et
`dotnet format` sont au vert. **La fin de chaque PR est un point d'arrêt possible.**

Chaque cas d'usage s'ouvre par ses scénarios Gherkin en français, que l'on voit d'abord échouer
([ADR-0004](adr/ADR-0004-atdd-et-bdd-par-cas-d-usage.md)). Une PR est découpée en commits ; chaque commit
est un incrément TDD.

**Livraison : mardi 20 octobre 2026 au soir.**
- **Minimum présentable** : le lot 2, c'est-à-dire un devis complet jusqu'à son PDF.
- **Cible** : le lot 3, c'est-à-dire le parcours complet du devis à la facture.
- **Bonus** : les lots 4 et 5, qui démontrent la modularité.

## Lot 1 — Le devis brouillon

**Un seul contexte** : une société au régime normal de TVA facture un professionnel.

### PR 1.1 — Le squelette qui marche

- **Mise en place**, sans comportement : la solution et ses projets (structure validée), et une CI
  GitHub Actions qui lance `dotnet test` et `dotnet format --verify-no-changes`.
- **Scénarios** :
  - configurer un émetteur de type société ([ADR-0007](adr/ADR-0007-saas-multi-emetteur.md)) ;
  - enregistrer un client professionnel pour cet émetteur ;
  - créer un devis brouillon pour ce client ;
  - ajouter une ligne, ce qui calcule le total HT, la TVA et le total TTC.
- **Ce qui apparaît** :
  - les objets valeur `Money`, `Quantity` et `VatRate` ;
  - `Issuer`, `Customer`, `Quote` et `QuoteLine` ;
  - les cas d'usage `ConfigureIssuer`, `RegisterCustomer`, `CreateQuote` et `AddQuoteLine` ;
  - l'adaptateur en mémoire ;
  - les premiers endpoints de l'API, avec leurs tests d'intégration.

### PR 1.2 — Les règles du devis

- **Scénarios** :
  - retirer une ligne, ce qui recalcule les totaux ;
  - un devis à deux taux de TVA, ventilée par taux (la TVA est calculée sur le total HT de chaque taux,
    pas ligne par ligne) ;
  - une remise sur une ligne ;
  - un contexte non pris en charge (par exemple un client particulier), refusé à la création du devis ;
  - une mention de contexte (l'identité légale de l'émetteur) présente dès la création du devis ;
  - une mention déclenchée par une ligne (le taux réduit de 10 %) : elle apparaît avec la première ligne
    concernée, et ne disparaît qu'au retrait de la dernière.
- **Ce qui apparaît** :
  - la liste blanche des contextes de facturation ;
  - `UnsupportedBillingContextException` ([ADR-0002](adr/ADR-0002-variations-selon-emetteur-et-client.md)) ;
  - les événements `QuoteDrafted`, `QuoteLineAdded` et `QuoteLineRemoved`, et le dispatcher en mémoire,
    exécuté dans l'unité de travail ([ADR-0003](adr/ADR-0003-reactions-par-evenements-idempotentes.md),
    amendement) ;
  - les premiers gestionnaires de mentions, fondés sur l'état et couverts par des tests de rejeu.

### PR 1.3 — La persistance en fichiers

- **Ce qui apparaît** :
  - la suite de contrat du port, rejouée sur l'adaptateur en mémoire puis sur l'adaptateur fichiers ;
  - l'adaptateur fichiers en JSON, qui respecte les règles de l'[ADR-0006](adr/ADR-0006-persistance-par-port-secondaire.md) ;
  - l'API branchée sur les fichiers, de sorte que les données survivent à un redémarrage.

### PR 1.4 — Les tests unitaires de la couche Application

Ajoutée en chemin : un scénario d'acceptation ne dispense pas d'un test unitaire.
- **Ce qui apparaît** :
  - le projet `InvoiceKata.Application.Tests` ;
  - les tests unitaires des règles de mention, de leurs textes et du dispatcher d'événements ;
  - la correction du capital social, qui était arrondi dans la mention légale.

## Lot 2 — La finalisation et le PDF du devis

### PR 2.1 — Finaliser le devis

- **Scénarios** :
  - finaliser un devis, qui reçoit alors un numéro, une date et une date de validité (30 jours par défaut,
    ou la durée demandée) ;
  - les numéros se suivent par émetteur et repartent à 1 chaque année ;
  - toute modification d'un devis finalisé est refusée, comme une seconde finalisation ou celle d'un devis
    sans ligne.
- **Ce qui apparaît** :
  - `QuoteNumber` et `QuoteNumbering`, et le refus d'un doublon par le port
    ([CDR-0007](cdr/CDR-0007-numerotation-des-devis.md)) ;
  - l'événement `QuoteFinalized` ;
  - la mention de validité, composée à la finalisation (amendement du
    [CDR-0001](cdr/CDR-0001-mentions-en-template-method.md)) ;
  - l'état v2 du devis, qui garde ses montants tels qu'émis, et le premier convertisseur de version ;
  - la route `POST …/finalization` et les refus en `409`.

### PR 2.2 — Le rendu du PDF

- **Scénarios** :
  - le PDF d'un devis finalisé porte son numéro, ses lignes, ses totaux et ses mentions ;
  - rejouer la finalisation ne refait pas le PDF ;
  - un devis brouillon n'a pas de PDF.
- **Ce qui apparaît** :
  - le contrat du document résolu, en JSON (amendement de l'[ADR-0005](adr/ADR-0005-rendu-pdf-par-typst.md)) ;
  - le validateur final, composé des validateurs de chaque partie ;
  - l'adaptateur Typst, dans son propre projet, avec le template « société » repris du spike ;
  - la publication après l'enregistrement, et la réaction qui rend le PDF, idempotente et couverte par un
    test de rejeu ;
  - le stockage des PDF, qui n'écrase jamais un PDF ;
  - l'endpoint de téléchargement du PDF ;
  - Typst installé dans la CI ;
  - les PDF produits par les tests, publiés en artefacts de la CI et dans les rapports.

## Lot 3 — La facture

### PR 3.1 — Émettre la facture à partir du devis

- **Scénarios** :
  - émettre la facture d'un devis finalisé, avec ses mentions de paiement : échéance, escompte, pénalités,
    indemnité de 40 € ;
  - le délai de paiement se choisit à l'émission ;
  - les numéros se suivent sans trou ;
  - un devis donne au plus une facture ;
  - un devis brouillon ne se facture pas ;
  - le PDF de la facture porte son numéro, son devis, ses totaux et ses mentions de paiement.
- **Ce qui apparaît** ([ADR-0010](adr/ADR-0010-facture-emise-depuis-un-devis-finalise.md)) :
  - `Invoice`, qui recopie le devis tel qu'émis et n'a aucune méthode qui la modifie ;
  - `InvoiceNumber` et `InvoiceNumbering`, et le port qui refuse un numéro en double ou une seconde facture
    pour un même devis ;
  - l'événement `InvoiceIssued` et le PDF de la facture, avec le même template ;
  - le stockage des PDF, généralisé à tout type de document ;
  - les routes `POST /invoices`, `GET /invoices/{id}` et `GET /invoices/{id}/pdf`.

## Lot 4 — Un second contexte : un EI en franchise de TVA

### PR 4.1 — Un EI en franchise de TVA

- **Scénarios** :
  - un devis d'EI en franchise, sans TVA, avec la mention « TVA non applicable, art. L. 223 et s. du CIBS »
    (la référence en vigueur depuis le 1er septembre 2026) et la mention « EI » ;
  - une ligne avec un taux de TVA est refusée sur un devis en franchise ;
  - la facture d'un EI, sans TVA, avec les mêmes mentions.
- **Ce qui apparaît** ([ADR-0011](adr/ADR-0011-ei-en-franchise-de-tva.md)) :
  - le type et le régime de TVA de l'émetteur : `POST /issuers` prend un `kind`, et l'état de l'émetteur passe
    en version 2 ;
  - la Strategy du régime de TVA, avec ses deux implémentations : le régime normal, et la franchise, qui
    refuse un taux et ne calcule aucune TVA ;
  - le régime gardé par le devis à sa création, dont l'état passe en version 3 ;
  - la mention de franchise, et la mention d'identité légale propre à l'EI ;
  - le template `ei.typ`, sans TVA, avec un « Net à payer », choisi selon le type d'émetteur ;
  - une ligne de plus dans la liste blanche.

## Lot 5 — Un client particulier

### PR 5.1

- **Scénarios** :
  - un devis, puis une facture, pour un particulier : pas d'indemnité de 40 €, pas de SIREN client, et des
    pénalités de retard au taux légal ([ADR-0012](adr/ADR-0012-client-particulier.md)).
- **Ce qui apparaît** :
  - de nouvelles règles de mentions ;
  - une ligne de plus dans la liste blanche.

## Hors roadmap

Les avoirs, les factures d'acompte, Factur-X (la voie est connue : décision 8 de l'ADR-0005),
PostgreSQL, l'authentification et le multi-utilisateur.

Ajouté pour la démo, hors périmètre : un front de démonstration servi par l'API (une page sans framework ni
build, zone de démo du CLAUDE.md).

## Questions à trancher en chemin

| Quand | Question |
|---|---|
| PR 2.1 | Le format du numéro de devis, et la durée de validité par défaut. **Tranché** : `D-2026-0001` et 30 jours ([CDR-0007](cdr/CDR-0007-numerotation-des-devis.md)) |
| PR 3.1 | Faut-il l'acceptation du client (« bon pour accord ») avant de pouvoir facturer, ou un devis finalisé suffit-il ? **Tranché** : un devis finalisé suffit ([ADR-0010](adr/ADR-0010-facture-emise-depuis-un-devis-finalise.md)) |
| PR 3.1 | Le format du numéro de facture. **Tranché** : `F-2026-0001`, une seule série puisque les avoirs sont hors roadmap ([ADR-0010](adr/ADR-0010-facture-emise-depuis-un-devis-finalise.md)) |
