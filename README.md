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

_À définir._
