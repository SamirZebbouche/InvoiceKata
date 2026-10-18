# ADR-0009 : Les documents légaux ne sont jamais réécrits, les migrations se font à la lecture

- **Statut** : accepté le 2026-10-18
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Les données stockées changeront de forme à mesure que le domaine évolue. Il faut décider comment migrer.
Or une facture émise est un document légal : elle se conserve 10 ans, et son contenu ne doit pas être
modifié pendant toute la durée de conservation. On la corrige par un avoir, jamais en la réécrivant
([service-public, F23208](https://entreprendre.service-public.gouv.fr/vosdroits/F23208)). L'entreprise doit
aussi pouvoir garantir l'authenticité, l'intégrité et la lisibilité de ses factures (piste d'audit fiable).

## Décision

1. **Un document figé (devis finalisé, facture émise, avoir) n'est jamais réécrit**, ni par le produit ni
   par une migration.
2. **Il est stocké tel qu'émis** : ses montants et ses mentions sont enregistrés, jamais recalculés, même si
   une règle de calcul ou de mention change ensuite.
3. **Les migrations se font de version en version, à la lecture** (*upcasting*). Chaque état stocké porte
   sa version. Au chargement, un état ancien est converti en mémoire vers la version courante, sans être
   réécrit. Cela vaut pour tous les états, brouillons compris, pour qu'il n'y ait qu'une seule manière de
   migrer.
4. **Une version inconnue est refusée** : on ne charge pas un état qu'on ne sait pas lire.
5. **Aucun convertisseur n'est écrit avant le premier changement de forme.** Un fichier de référence par
   agrégat, dans les tests, échoue ce jour-là et force la décision
   ([CDR-0006](../cdr/CDR-0006-etat-persistant-des-agregats.md)).

## Options écartées

| Option | Raison |
|---|---|
| Une migration en lot qui réécrit les données au déploiement | Elle réécrirait des factures émises, ce qui touche à leur intégrité légale ; c'est aussi une étape de déploiement risquée. |
| Une migration en lot pour les brouillons, à la lecture pour les documents figés | Deux mécanismes de migration à maintenir, pour un gain faible. |
| Recalculer un document figé à partir des règles du moment | Une facture d'hier changerait si une règle changeait aujourd'hui. |

## Conséquences

- La chaîne de convertisseurs s'allonge à chaque évolution de forme, et chaque version reste lisible tant
  que des données de cette version existent.
- Avec PostgreSQL, les migrations de schéma (tables, colonnes) restent possibles, mais elles ne modifient
  jamais le contenu d'un document figé.
