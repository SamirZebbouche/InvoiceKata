# CDR-0004 : Les erreurs du domaine traduites en codes HTTP

- **Statut** : accepté le 2026-10-18 · en place pour `400` et `404`
- **Décideur** : Samir

## Problème

Les cas d'usage échouent pour des raisons métier : ressource inconnue, donnée invalide, action interdite
par l'état, contexte non pris en charge. L'API doit répondre avec le bon code et au format Problem Details
(RFC 9457), sans que le domaine connaisse HTTP. Samir a choisi les exceptions de domaine plutôt que le
result pattern (ADR-0002).

## Décision

1. **Une exception par raison métier**, regroupée sous une classe de base quand plusieurs partagent la
   même réponse : `ResourceNotFoundException`, dont dérivent `IssuerNotFoundException`,
   `CustomerNotFoundException` et `QuoteNotFoundException`.
2. **La traduction se fait à un seul endroit, au plus haut niveau** : un gestionnaire d'exceptions de
   l'API (`IExceptionHandler`) traduit chaque classe en code HTTP, au format Problem Details. Aucune
   exception générique n'est attrapée ailleurs (règle `dotnet-error-handling`).
3. **La correspondance :**

   | Erreur | Code | Où |
   |---|---|---|
   | Ressource inconnue, ou appartenant à un autre émetteur | `404` | `ResourceNotFoundException` |
   | Donnée invalide à la frontière (forme juridique inconnue…) | `400` | La validation de l'endpoint |
   | Action interdite par l'état du document | `409` | À venir |
   | Contexte de facturation non pris en charge | `422` | À venir (`UnsupportedBillingContextException`) |

4. **Une ressource d'un autre émetteur répond `404`, jamais `403`** ([ADR-0007](../adr/ADR-0007-saas-multi-emetteur.md)).

## Options écartées

| Option | Raison |
|---|---|
| Le result pattern | Samir a retenu les exceptions de domaine : un refus est franc et ne se rattrape pas en cours de route. |
| Un `try/catch` par endpoint | La traduction serait dupliquée et finirait par diverger. |
| Attraper `Exception` dans les cas d'usage | Des bugs seraient avalés, et le système resterait dans un état inconnu. |

## Conséquences

- Les tests d'API exigent une réponse Problem Details sur chaque erreur. Ils ne passent donc pas sur un
  simple `404` dû à une route absente.
