# CDR-0007 : La numérotation des devis, calculée par le domaine et bloquée par le port

- **Statut** : accepté le 2026-10-18
- **Décideur** : Samir

## Problème

Un devis reçoit son numéro à la finalisation. Deux devis d'un même émetteur ne doivent jamais porter le
même numéro. Samir a demandé une double sécurité (« ceinture et bretelles ») : le port bloque un doublon,
et le domaine empêche lui aussi la collision. Or le domaine ne voit qu'un devis à la fois, et deux
requêtes simultanées peuvent finaliser deux devis du même émetteur.

## Décision

1. **Le format est `D-2026-0001`** : un préfixe, l'année de la finalisation, puis une séquence par
   émetteur sur au moins quatre chiffres, remise à zéro chaque année. `QuoteNumber` est un objet valeur :
   il se lit, s'écrit et refuse un format invalide.
2. **Le domaine calcule le numéro suivant à partir des numéros déjà pris.** `QuoteNumbering` reçoit
   l'année et les numéros de l'émetteur, puis rend le plus grand de l'année, plus un. Le numéro qu'il rend
   ne peut donc pas être un numéro qu'il connaît. Il n'y a pas de compteur séparé : un numéro n'existe que
   si le devis existe ([ADR-0006](../adr/ADR-0006-persistance-par-port-secondaire.md)).
3. **Le port refuse un doublon à l'enregistrement.** L'adaptateur fichiers vérifie, sous le verrou qui
   sérialise déjà ses écritures, qu'aucun autre devis de l'émetteur ne porte ce numéro, puis il écrit.
   Une finalisation simultanée qui aurait obtenu le même numéro est refusée par une exception du domaine,
   traduite en `409`. Avec PostgreSQL, ce contrat devient un index unique. La suite de contrat le vérifie
   sur chaque adaptateur.
4. **Le cas d'usage enchaîne les deux** : il lit les numéros pris, demande le suivant au domaine,
   finalise le devis, puis l'enregistre.

## Options écartées

| Option | Raison |
|---|---|
| Un *interlock* (`Interlocked.Increment`) ou un verrou dans le domaine | Le domaine dépendrait d'un état partagé, impossible à isoler dans les tests. Ce compteur serait perdu au redémarrage, et il ne protège qu'un seul processus. Un verrou est un mécanisme de concurrence : il appartient à l'adaptateur, où il existe déjà. |
| Un verrou singleton dans la couche Application | Il ne protège qu'un seul processus, et il disparaîtrait au passage à plusieurs instances. |
| Un compteur dans l'agrégat `Issuer` | Il faudrait enregistrer l'émetteur et le devis ensemble, sans transaction : le compteur pourrait diverger des devis réellement enregistrés. |
| Un agrégat « séquence de numéros » avec verrouillage optimiste | Le même risque de divergence, avec un agrégat de plus à persister. |
| `Quote.Finalize` qui vérifie l'unicité | Un devis ne voit que lui-même. |
| Une séquence continue, sans l'année | Elle est moins lisible, et rien ne l'impose pour un devis. |
| Un préfixe réglable par émetteur | Personne ne le demande (YAGNI). |

## Conséquences

- La numérotation des factures ([ADR-0006](../adr/ADR-0006-persistance-par-port-secondaire.md)) reprendra
  le même partage, avec en plus la continuité sans trou qu'impose la loi.
- Une collision entre deux requêtes simultanées répond `409`, et le client relance la finalisation.
