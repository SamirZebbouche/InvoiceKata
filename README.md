# InvoiceKata

Exercice technique proposé par Fimeco dans le cadre d'un processus de recrutement : développer un petit
logiciel qui établit des **devis** et des **factures**.

## L'énoncé

- **Sujet** : un logiciel de devis et de factures.
- **Périmètre** : libre, et volontairement laissé au choix du candidat.
- **Méthode** : travailler avec ses agents IA, comme d'habitude.
- **Temps** : quelques heures. Un résultat partiel assumé vaut mieux qu'un résultat complet obtenu en y
  passant un week-end.
- **Critères** : les choix et la façon de les expliquer, pas la finition.

## Les livrables

1. **Le logiciel**, dans ce dépôt.
2. **[Une page sur le travail avec les agents](TRAVAIL-AVEC-LES-AGENTS.md)** : ce qui leur a été délégué, ce qui a été gardé, comment
   leur production a été vérifiée, et ce qui n'a pas marché du premier coup.

## Calendrier

| Date | Étape |
|---|---|
| Mardi 20 octobre 2026, au soir | Envoi du dépôt |
| Jeudi 22 octobre 2026 | Revue de l'exercice, en entretien |

## Le cadre de travail

Les règles suivies par les agents viennent de ma base de pratiques personnelle et sont installées dans
le dépôt :

- [CLAUDE.md](CLAUDE.md) pour la méthode : délibérer avant de proposer, travailler en TDD, livrer en
  lots relisibles, vérifier avant d'affirmer, respecter les limites d'action sur git, et suivre les
  conventions de langue ;
- [.claude/skills/](.claude/skills/) pour les compétences à la demande : architecture, conception
  objet, idiomes C# et .NET, tests, git.

Stack : C# / .NET, avec `dotnet test` et `dotnet format --verify-no-changes` comme preuves.

## Périmètre retenu

La partie back d'un logiciel de devis et de factures. On enregistre un client, on crée un devis, on y
ajoute ou on en retire des lignes, et la TVA est recalculée à chaque fois. On finalise ensuite le devis
pour en rendre le PDF, puis on génère la facture à partir du devis. Les mentions légales dépendent de
l'émetteur et du client, et tout contexte non pris en charge est refusé
([ADR-0002](docs/adr/ADR-0002-variations-selon-emetteur-et-client.md)).

## Décisions

| ADR | Décision |
|---|---|
| [0001](docs/adr/ADR-0001-spike-jetable-puis-lots-livrables.md) | Un spike de rendu jetable, puis des lots livrables |
| [0002](docs/adr/ADR-0002-variations-selon-emetteur-et-client.md) | Les variations selon l'émetteur et le client, dans un monde fermé |
| [0003](docs/adr/ADR-0003-reactions-par-evenements-idempotentes.md) | Les réactions par événements de domaine, idempotentes |
| [0004](docs/adr/ADR-0004-atdd-et-bdd-par-cas-d-usage.md) | ATDD et BDD pour chaque cas d'usage |
| [0005](docs/adr/ADR-0005-rendu-pdf-par-typst.md) | Le rendu PDF par Typst, à partir d'un document résolu |

## Roadmap

Chaque lot laisse un système fonctionnel : la fin de chaque lot est un point d'arrêt possible.

| Lot | Contenu | Ce qui apparaît |
|---|---|---|
| Spike ✅ | YAML + template → PDF, jamais mergé ([branche `spike/pdf-rendering`](https://github.com/SamirZebbouche/InvoiceKata/tree/spike/pdf-rendering/spike)) | [ADR-0005](docs/adr/ADR-0005-rendu-pdf-par-typst.md) |
| 1 | Client, devis brouillon, ajout et retrait de lignes, TVA. Un seul contexte : une société au régime normal qui facture un professionnel. | Le premier scénario ATDD et le refus des contextes non pris en charge |
| 2 | Finalisation du devis, qui est alors figé, et rendu de son PDF | `QuoteFinalized`, le premier gestionnaire idempotent |
| 3 | Facture créée à partir du devis : numérotation continue, immuabilité, mentions de paiement | `InvoiceIssued` |
| 4 | Un second contexte : un EI en franchise de TVA | La Strategy de TVA |
| 5 | Un client particulier | De nouvelles règles de mentions |
