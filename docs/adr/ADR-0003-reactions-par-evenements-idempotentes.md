# ADR-0003 : Des réactions par événements de domaine, idempotentes

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Une fois la stratégie choisie ([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)), Samir veut
que certaines parties s'enchaînent par événements, en chorégraphie plutôt qu'en orchestration. La
contrainte de l'ADR-0002 s'applique toujours : jamais de facture fausse.

## Décision

1. **Le noyau légal reste synchrone.** Résoudre le contexte, calculer les lignes, les totaux, la TVA et
   les mentions, finaliser le devis, émettre la facture et lui attribuer son numéro : tout cela se fait
   par appels directs, dans une seule transaction. Ce qui est irréversible doit réussir en entier ou ne
   pas avoir lieu.
2. **Ce qui réagit à un fait acquis est chorégraphié.** Ces réactions passent par des événements de
   domaine : `QuoteFinalized` déclenche le rendu du PDF du devis, `InvoiceIssued` celui du PDF de la
   facture, et plus tard l'envoi ou l'archivage. Les événements sont publiés en mémoire, par un
   dispatcher dans le processus.
3. **Chaque événement est traité de façon idempotente.** Traiter deux fois le même événement laisse le
   même état. On obtient cette idempotence par une **clé métier** plutôt que par un registre technique :
   un PDF est identifié par son document, et un devis donne au plus une facture. Chaque gestionnaire
   d'événement a un test de rejeu.
4. **Les événements arrivent avec la première réaction réelle**, sans doute le rendu du PDF à la
   finalisation, et ils sont écrits en TDD.

## Options écartées

| Option | Raison |
|---|---|
| Chorégraphier le calcul (chaque règle de mention réagit à un événement) | Plus personne ne garantit que le document est complet. Le monde fermé de l'ADR-0002 ne tient plus, et une mention manquante passerait inaperçue. |
| Chorégraphier l'émission de la facture | Une étape qui échoue après l'attribution du numéro laisse un trou ou une facture à moitié faite, ce qui impose des compensations sur un document immuable. |
| Tout orchestrer, sans événements | Chaque nouvelle réaction modifierait le noyau. |
| Un broker de messages dès le départ | Une porte à sens unique, alors qu'il n'y a ni second processus ni besoin de durabilité. |
| Une idempotence par registre générique des événements traités | Elle ne protège pas contre un même fait publié deux fois sous deux identifiants. On la réévaluera si un broker arrive. |

## Conséquences

- Une réaction qui échoue se rejoue sans remettre en cause le fait qui l'a déclenchée.
- Le jour où une réaction devra survivre à un redémarrage, on ajoutera un outbox ou un broker, ce qui
  passera par un nouvel ADR.
