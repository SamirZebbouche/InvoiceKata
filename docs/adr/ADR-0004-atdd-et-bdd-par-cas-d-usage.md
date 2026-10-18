# ADR-0004 : ATDD et BDD pour chaque cas d'usage

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Chaque nouveau cas d'usage doit être spécifié par des exemples lisibles sans lire le code. Dans la
réalité, ces exemples seraient validés par un PO ; ici, c'est Samir qui en joue le rôle. La règle TDD du
[CLAUDE.md](../../CLAUDE.md) s'applique toujours.

## Décision

1. **Une double boucle.**
   - Boucle externe, en ATDD : chaque cas d'usage commence par un scénario Gherkin, que l'on voit échouer.
   - Boucle interne, en TDD unitaire : on fait passer le scénario par des tests sur le domaine (calcul,
     arrondis, invariants).
   - Un cas d'usage est livré quand son scénario passe.
2. **Le niveau d'acceptation : les cas d'usage de l'application**, avec des adaptateurs en mémoire. Ni
   HTTP ni PDF réel : les scénarios vérifient le document résolu (lignes, totaux, mentions), et le rendu a
   ses propres tests d'intégration.
3. **Les outils.** Reqnroll, le successeur de SpecFlow, sur xUnit.
4. **Les langues.** Les fichiers `.feature` sont écrits en **français** (`# language: fr`), parce qu'ils
   s'adressent au PO. Les step definitions et le code restent en anglais.

## Options écartées

| Option | Raison |
|---|---|
| Des tests xUnit nommés en Given/When/Then, sans Gherkin | La spécification ne serait pas lisible sans lire le code. |
| Une acceptation passant par une API HTTP | Il n'y a pas d'API dans le périmètre : il faudrait d'abord construire de la plomberie. |
| L'acceptation seule, sans TDD unitaire | Les cas limites se noieraient dans des scénarios lents à diagnostiquer. |
| SpecFlow | En fin de vie depuis fin 2024. |

## Conséquences

- Le [CLAUDE.md](../../CLAUDE.md) déclare l'exception de langue pour les fichiers `.feature`.
- Chaque lot de la roadmap s'ouvre par ses scénarios.
