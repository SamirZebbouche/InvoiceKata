# InvoiceKata

Exercice technique proposé par un recruteur dans le cadre d'un processus de recrutement : développer un petit
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

Stack : C# / .NET, avec `dotnet test`, `dotnet format --verify-no-changes` et `dotnet stryker` comme preuves.

**Rapports de test**, publiés par la CI à chaque passage sur `main` : <https://samirzebbouche.github.io/InvoiceKata/>.
On y trouve les scénarios d'acceptation en français, avec le résultat de chaque étape, la couverture du code et
le rapport des tests par mutation (Stryker.NET), dont le score bloque la CI sous 70 %.

## Lancer l'API

```
dotnet run --project src/InvoiceKata.Api
```

Les données sont des fichiers JSON, un dossier par émetteur, sous `src/InvoiceKata.Api/data`
(réglage `Persistence:Directory`). Elles survivent au redémarrage.

Le PDF d'un devis finalisé est rendu par [Typst](https://typst.app) 0.15.1, qui doit être installé et
accessible sous le nom `typst` (réglage `Rendering:Typst`). Il est rangé à côté du devis, et se télécharge par
`GET /issuers/{issuerId}/quotes/{id}/pdf`.

## Périmètre retenu

La partie back d'un logiciel de devis et de factures, exposée par une **API** ([docs/API.md](docs/API.md)) :
un SaaS où chaque entreprise émettrice configure son profil. On enregistre un client, on crée un devis, on y
ajoute ou on en retire des lignes, et la TVA est recalculée à chaque fois. On finalise ensuite le devis
pour en rendre le PDF, puis on génère la facture à partir du devis. Les mentions légales dépendent de
l'émetteur et du client, et tout contexte non pris en charge est refusé
([ADR-0002](docs/adr/ADR-0002-variations-selon-emetteur-et-client.md)).

## Décisions

Les décisions d'**architecture** sont des ADR, ci-dessous. Les décisions de **conception du code**
(motifs, modélisation, conventions) sont des CDR, avec la carte des motifs en usage :
[docs/cdr/README.md](docs/cdr/README.md).

| ADR | Décision |
|---|---|
| [0001](docs/adr/ADR-0001-spike-jetable-puis-lots-livrables.md) | Un spike de rendu jetable, puis des lots livrables |
| [0002](docs/adr/ADR-0002-variations-selon-emetteur-et-client.md) | Les variations selon l'émetteur et le client, dans un monde fermé |
| [0003](docs/adr/ADR-0003-reactions-par-evenements-idempotentes.md) | Les réactions par événements de domaine, idempotentes |
| [0004](docs/adr/ADR-0004-atdd-et-bdd-par-cas-d-usage.md) | ATDD et BDD pour chaque cas d'usage |
| [0005](docs/adr/ADR-0005-rendu-pdf-par-typst.md) | Le rendu PDF par Typst, à partir d'un document résolu |
| [0006](docs/adr/ADR-0006-persistance-par-port-secondaire.md) | La persistance derrière un port secondaire, des fichiers pour le kata |
| [0007](docs/adr/ADR-0007-saas-multi-emetteur.md) | Un SaaS multi-émetteur, l'émetteur dans la route |
| [0008](docs/adr/ADR-0008-arrondis.md) | Les arrondis : précision dans le domaine, centimes pour les montants légaux |
| [0009](docs/adr/ADR-0009-documents-legaux-jamais-reecrits.md) | Les documents légaux ne sont jamais réécrits, les migrations se font à la lecture |
| [0010](docs/adr/ADR-0010-facture-emise-depuis-un-devis-finalise.md) | La facture, émise à partir d'un devis finalisé |

## Roadmap

Chaque PR laisse un système fonctionnel : la fin de chaque PR est un point d'arrêt possible. Le détail des
PR, des scénarios et des questions à trancher se trouve dans [docs/ROADMAP.md](docs/ROADMAP.md).

| Lot | Contenu | Ce qui apparaît |
|---|---|---|
| Spike ✅ | YAML + template → PDF, jamais mergé ([branche `spike/pdf-rendering`](https://github.com/SamirZebbouche/InvoiceKata/tree/spike/pdf-rendering/spike)) | [ADR-0005](docs/adr/ADR-0005-rendu-pdf-par-typst.md) |
| 1 | Client, devis brouillon, ajout et retrait de lignes, TVA. Un seul contexte : une société au régime normal qui facture un professionnel. Exposé par l'API, stocké en fichiers. | Le premier scénario ATDD, le refus des contextes non pris en charge, et la suite de contrat du port de persistance |
| 2 | Finalisation du devis, qui est alors figé, et rendu de son PDF | `QuoteFinalized`, le premier gestionnaire idempotent |
| 3 | Facture créée à partir du devis : numérotation continue, immuabilité, mentions de paiement | `InvoiceIssued` |
| 4 | Un second contexte : un EI en franchise de TVA | La Strategy de TVA |
| 5 | Un client particulier | De nouvelles règles de mentions |
