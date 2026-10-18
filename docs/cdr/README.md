# Code Design Reports

Un **CDR** trace une décision de **conception du code** : un motif, une modélisation, une convention.
Les décisions d'**architecture** (frontières, persistance, contrats publics) sont des
[ADR](../../README.md#décisions).

Chaque CDR suit la même trame : le problème, le motif retenu, où il s'applique dans le code, les options
écartées et les conséquences.

## Les CDR

| CDR | Décision | Mise en œuvre |
|---|---|---|
| [0001](CDR-0001-mentions-en-template-method.md) | Une mention légale = une règle en Template Method | En place |
| [0002](CDR-0002-evenements-leves-par-l-agregat.md) | Les événements levés par l'agrégat, dispatchés jusqu'à épuisement | En place, en un seul passage |
| [0003](CDR-0003-objets-valeur-et-proprietes-requises.md) | Objets valeur contre l'inversion, propriétés `required` pour les enregistrements | En place |
| [0004](CDR-0004-erreurs-et-codes-http.md) | Les erreurs du domaine traduites en codes HTTP | En place |
| [0005](CDR-0005-controleurs-plutot-que-minimal-apis.md) | Des contrôleurs plutôt que des minimal APIs | En place |

## Carte des motifs en usage

| Motif | Où | Décision |
|---|---|---|
| Strategy | Le régime de TVA, dès qu'il en existe un deuxième | [ADR-0002](../adr/ADR-0002-variations-selon-emetteur-et-client.md) |
| Template Method | Les règles de mentions légales | [CDR-0001](CDR-0001-mentions-en-template-method.md) |
| Monde fermé (liste blanche) | Les contextes de facturation pris en charge | [ADR-0002](../adr/ADR-0002-variations-selon-emetteur-et-client.md) |
| Événements de domaine, chorégraphie | Les mentions (avant l'enregistrement), le PDF (après) | [ADR-0003](../adr/ADR-0003-reactions-par-evenements-idempotentes.md), [CDR-0002](CDR-0002-evenements-leves-par-l-agregat.md) |
| Composite | Le validateur final du document résolu | [ADR-0005](../adr/ADR-0005-rendu-pdf-par-typst.md) |
| Port et adaptateur | La persistance, filtrée par émetteur | [ADR-0006](../adr/ADR-0006-persistance-par-port-secondaire.md) |
| Objets valeur, identifiants typés | `Money`, `Quantity`, `VatRate`, `Siren`, `VatNumber`, `IssuerId`… | [CDR-0003](CDR-0003-objets-valeur-et-proprietes-requises.md), [ADR-0007](../adr/ADR-0007-saas-multi-emetteur.md) |
| Hiérarchie d'exceptions de domaine | `ResourceNotFoundException` et ses dérivées | [CDR-0004](CDR-0004-erreurs-et-codes-http.md) |
