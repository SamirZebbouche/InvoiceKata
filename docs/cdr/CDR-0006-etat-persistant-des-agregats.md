# CDR-0006 : L'état persistant des agrégats, garanti par une interface

- **Statut** : accepté le 2026-10-18 · en place pour l'émetteur, le client et le devis (brouillon et finalisé, état v2)
- **Décideur** : Samir

## Problème

Le domaine protège ses objets (constructeurs privés, événements levés à la création). Un adaptateur doit
pourtant les **reconstruire** depuis un fichier, puis demain depuis PostgreSQL, sans lever d'événement et
sans repasser par les règles de création. Samir voulait trois garanties :
- la présence des méthodes de persistance dans les classes à persister ;
- un objet métier qui ne soit pas alourdi ;
- aucun objet reconstruit dans un état faux quand le domaine évolue.

## Décision

1. **Une interface, vérifiée par le compilateur** : `IPersistable<TSelf, TState>`, avec `TState ToState()`
   et `static abstract TSelf FromState(TState state)` (membre statique abstrait). Une classe à persister
   ne peut pas oublier l'un des deux sens.
2. **La persistance vit dans un fichier partiel à part** (`Quote.State.cs`, `Issuer.State.cs`,
   `Customer.State.cs`) : le fichier métier ne change pas.
3. **Un état est un `record` simple**, défini dans le domaine, avec un **numéro de version**. Les
   adaptateurs le sérialisent tel quel ; aucun d'eux ne connaît les objets internes.
4. **`FromState` revérifie les invariants structurels** (identifiants, SIREN d'un professionnel, absence
   de SIREN chez un particulier, lignes uniques, codes connus) et refuse un état faux. Il **ne rejoue pas
   les règles de création** : un devis valide quand il a été créé le reste, même si la liste blanche
   change. Il ne lève aucun événement.
5. **On ne stocke pas ce qui se calcule** : les totaux et la ventilation de la TVA d'un brouillon sont
   recalculés à partir des lignes. Ses mentions sont stockées. Elles se réévaluent au moindre changement
   du devis ; un recalcul au chargement s'ajoutera le jour où une règle de mention changera entre deux
   déploiements.
6. **Un document figé, à venir (devis finalisé, facture), stockera ses montants et ses mentions tels
   qu'émis**, et ne les recalculera jamais ([ADR-0009](../adr/ADR-0009-documents-legaux-jamais-reecrits.md)).
7. **Les migrations se font de version en version, à la lecture** (*upcasting*), pour la raison légale
   que pose l'[ADR-0009](../adr/ADR-0009-documents-legaux-jamais-reecrits.md). Aucun convertisseur n'est
   écrit avant le premier changement de forme : un **fichier de référence par agrégat**, dans les tests,
   échoue ce jour-là et force la décision.
8. **L'adaptateur en mémoire stocke lui aussi des états**, pas des références : une modification non
   enregistrée n'est pas conservée, quel que soit l'adaptateur. La suite de contrat le vérifie.

## Options écartées

| Option | Raison |
|---|---|
| Des mappers dans l'adaptateur, sans contrat dans le domaine | Ils ont besoin d'une porte de reconstruction publique, avec de longues listes de paramètres ; et rien ne garantit qu'une nouvelle classe à persister aura son mapper. |
| Une reconstruction `internal`, ouverte par `InternalsVisibleTo` | Un couplage caché entre le domaine et un adaptateur précis. |
| Sérialiser directement les objets du domaine (`[JsonConstructor]`, accesseurs privés) | La persistance entre dans le domaine, et le format des fichiers dépend de la forme des classes. |
| Une reconstruction par réflexion | Un couplage caché qui casse à chaque renommage. |

## Conséquences

- **Le premier convertisseur est v1 → v2** : la version 2 du devis ajoute sa finalisation (numéro, dates et
  montants tels qu'émis). Comme elle n'ajoute qu'un champ facultatif, un fichier v1 se lit dans l'état
  courant, et le convertisseur vérifie seulement qu'il s'agit d'un brouillon avant de monter la version.
  Le jour où une version renommera ou retirera un champ, l'ancienne forme aura son propre `record`.
  Les fichiers de référence v1 et v2 sont tous deux lus par les tests.

- Avec PostgreSQL, l'état se stocke en JSONB, presque comme un fichier, ou sert de modèle de persistance à
  des tables relationnelles (EF Core ou Dapper). La suite de contrat garantit que rien ne change pour le
  domaine.
