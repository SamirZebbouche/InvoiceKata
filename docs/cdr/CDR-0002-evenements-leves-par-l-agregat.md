# CDR-0002 : Les événements levés par l'agrégat, dispatchés jusqu'à épuisement

- **Statut** : accepté le 2026-10-18 · pas encore mis en œuvre
- **Décideur** : Samir

## Problème

L'[ADR-0003](../adr/ADR-0003-reactions-par-evenements-idempotentes.md) dit **quand** les événements sont
traités : les mentions dans l'unité de travail, avant l'enregistrement ; les réactions à un fait acquis,
après. Il reste à décider **où** ils naissent et **comment** ils sont distribués.

## Décision

1. **L'agrégat lève ses événements.** `Quote` accumule `QuoteDrafted`, `QuoteLineAdded` et
   `QuoteLineRemoved` dans une liste interne, que l'application relève. Le domaine ne connaît ni le
   dispatcher ni les gestionnaires.
2. **L'application porte le dispatcher et les gestionnaires.** Le dispatcher appelle, pour chaque
   événement, tous les gestionnaires de ce type enregistrés par injection de dépendances.
3. **Le cas d'usage orchestre l'unité de travail** : l'opération sur l'agrégat, puis le dispatch des
   gestionnaires de mentions **jusqu'à épuisement**, y compris des événements que ces gestionnaires lèvent
   à leur tour, puis l'enregistrement, et seulement ensuite les réactions à un fait acquis.
4. **Le dispatcher est en mémoire, dans le processus**, sans bus ni broker.

## Options écartées

| Option | Raison |
|---|---|
| Un bus d'infrastructure (MediatR, broker…) | Une dépendance et une porte à sens unique, sans second processus ni besoin de durabilité. |
| Des gestionnaires dans le domaine | L'orchestration et l'injection entreraient dans le domaine. |
| Publier les événements directement depuis l'agrégat (événements statiques) | Un état global difficile à tester, et un ordre d'exécution caché. |
| Dispatcher après l'enregistrement pour les mentions | Le devis enregistré serait incomplet un instant, ce que l'ADR-0003 interdit pour le noyau légal. |

## Conséquences

- L'épuisement doit avoir une borne, pour qu'une boucle entre gestionnaires échoue franchement au lieu de
  tourner indéfiniment. Elle sera fixée et testée avec le dispatcher.
- Le validateur final ne s'exécute qu'après l'épuisement (ADR-0003, amendement).
