# ADR-0002 : Les variations selon l'émetteur et le client

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Le contenu légal d'un devis et d'une facture dépend de **qui facture** (une société ou un entrepreneur
individuel, en franchise de TVA ou non, artisan ou non) et de **qui est facturé** (un professionnel ou un
particulier). Les sources sont les fiches officielles :
[mentions obligatoires d'une facture](https://entreprendre.service-public.gouv.fr/vosdroits/F31808),
vérifiée le 11 août 2026, et [devis](https://www.service-public.gouv.fr/particuliers/vosdroits/F31144).

Le produit doit être modulaire : on ajoute un cas pris en charge à la fois. Le pire résultat possible
serait une **facture fausse**. Il vaut mieux ne pas en produire du tout.

## Décision

1. **Le régime de TVA est une variation exclusive** : un seul régime s'applique (normal, franchise
   293 B, autoliquidation…). C'est une **Strategy**, choisie selon le contexte de facturation (émetteur et
   client).
2. **Les mentions légales sont une variation additive** : plusieurs s'appliquent à la fois (forme
   juridique, « EI », indemnité de 40 € en B2B, garantie de conformité en B2C…). Elles prennent la forme
   d'une **liste de règles** : chaque règle dit si elle s'applique au contexte et ajoute ses mentions.
   *(Amendé le 2026-10-18 : ces règles sont des gestionnaires d'événements fondés sur l'état, voir
   l'[ADR-0003](ADR-0003-reactions-par-evenements-idempotentes.md).)*
3. **Le monde est fermé.** Une liste blanche énumère les contextes pris en charge. Tout autre contexte est
   refusé par une **exception de domaine**, levée à la **création du devis**, pour qu'un devis qui ne
   pourra pas être facturé ne soit jamais rédigé. Cette liste blanche documente ce que le produit sait
   faire.
4. **Les abstractions viennent avec le deuxième cas.** La Strategy n'est introduite qu'au moment où le
   deuxième régime arrive, en TDD : une Strategy qui n'a qu'une implémentation est une abstraction qui
   ment.
5. **Le profil est résolu avant le rendu.** Le domaine produit un document complet : lignes, totaux,
   TVA et mentions. Le rendu se contente de le mettre en forme. Aucun template ne contient de logique
   légale.
6. **Les modules sont des dossiers et des espaces de noms** dans un seul projet de domaine.

## Options écartées

| Option | Raison |
|---|---|
| Une Strategy par combinaison émetteur × client | Le nombre de combinaisons explose et les mentions partagées sont dupliquées. |
| Des `if` ou des `switch` sur le profil dans le domaine | Un cas particulier écrit à la main révèle un mauvais modèle, et chaque nouveau profil oblige à toucher tous ces endroits. |
| Des plugins chargés dynamiquement | C'est une porte à sens unique, sans second consommateur pour la justifier. |
| Un moteur de règles, ou des règles en YAML | De la logique légale sans types ni tests. |
| `NotImplementedException` pour un cas non pris en charge | Elle signale du code pas encore écrit, sans distinguer un refus voulu d'un oubli. Elle serait levée tard et confondue avec un bug. Samir retient l'exception de domaine. |

## Conséquences

- Chaque nouveau cas est un lot : une ligne dans la liste blanche, ses règles ou sa stratégie, et leurs
  tests.
- Le format de sortie n'est pas traité ici : PDF seul, ou facture électronique (Factur-X), dont l'émission
  devient obligatoire pour les PME le 1er septembre 2027. Il fera l'objet de l'ADR sur le rendu.
