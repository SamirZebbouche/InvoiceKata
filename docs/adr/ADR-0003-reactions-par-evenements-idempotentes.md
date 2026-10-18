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
| Chorégraphier le calcul (chaque règle de mention réagit à un événement) | *Option reprise par l'amendement du 2026-10-18, pour les mentions légales.* La raison invoquée ici, « plus personne ne garantit que le document est complet », ne tient plus depuis le validateur final de l'ADR-0005. |
| Chorégraphier l'émission de la facture | Une étape qui échoue après l'attribution du numéro laisse un trou ou une facture à moitié faite, ce qui impose des compensations sur un document immuable. |
| Tout orchestrer, sans événements | Chaque nouvelle réaction modifierait le noyau. |
| Un broker de messages dès le départ | Une porte à sens unique, alors qu'il n'y a ni second processus ni besoin de durabilité. |
| Une idempotence par registre générique des événements traités | Elle ne protège pas contre un même fait publié deux fois sous deux identifiants. On la réévaluera si un broker arrive. |

## Conséquences

- Une réaction qui échoue se rejoue sans remettre en cause le fait qui l'a déclenchée.
- Le jour où une réaction devra survivre à un redémarrage, on ajoutera un outbox ou un broker, ce qui
  passera par un nouvel ADR.

## Amendement du 2026-10-18 : les mentions légales sont chorégraphiées

Samir a observé que les mentions légales ne dépendent pas toutes des mêmes faits. Certaines ne dépendent
que du contexte et sont connues dès la création du devis ; d'autres ne se déclenchent qu'à l'ajout de
certaines lignes (taux réduit, nature des opérations, éco-participation…). Faire de chaque mention une
réaction indépendante allège le modèle : il n'y a plus de résolveur central qui connaisse toutes les
règles.

1. **Chaque règle de mention est un gestionnaire d'événements.** Elle réagit aux événements qui la
   concernent : `QuoteDrafted` (le contexte est fixé), `QuoteLineAdded`, `QuoteLineRemoved`, puis
   `QuoteFinalized`.
2. **Un gestionnaire est fondé sur l'état, pas sur le delta.** À chaque événement, il réévalue sa règle
   sur l'état courant du document, puis pose ou retire sa mention. Il est donc **idempotent** par
   construction, et il reste juste quand deux lignes déclenchent la même mention et qu'on n'en retire
   qu'une.
3. **Il y a deux moments de dispatch.**
   - Les gestionnaires de mentions s'exécutent **dans la même unité de travail, avant
     l'enregistrement** : le document est complet chaque fois qu'on le relit, et le noyau légal n'est
     jamais cohérent seulement à terme.
   - Les réactions à un fait acquis (PDF, envoi, archivage) s'exécutent **après l'enregistrement**,
     comme le prévoit la décision 2.
4. **Le validateur final ne s'exécute qu'une fois terminées toutes les réactions déclenchées.** Le
   dispatcher traite les événements jusqu'à épuisement, y compris ceux que lèvent les gestionnaires, et
   le validateur ([ADR-0005](ADR-0005-rendu-pdf-par-typst.md)) ne passe qu'ensuite.

Options écartées par cet amendement :

| Option | Raison |
|---|---|
| Des gestionnaires fondés sur le delta (ajouter la mention à l'ajout de la ligne, la retirer au retrait) | Ils deviennent faux dès que deux lignes déclenchent la même mention. |
| Des mentions résolues après l'enregistrement | Le noyau légal ne serait plus cohérent qu'à terme : un devis relu juste après une modification pourrait être incomplet. |
| Un résolveur central, avec des conditions sur le profil et les lignes | Chaque nouvelle mention toucherait le même code, et les cas particuliers y signaleraient un mauvais modèle. |
