# ADR-0010 : La facture, émise à partir d'un devis finalisé

- **Statut** : accepté le 2026-10-19
- **Date** : 2026-10-19
- **Décideur** : Samir

## Contexte

Une facture est un document légal : elle porte un numéro unique pris dans une suite continue, sans trou,
elle se conserve 10 ans, et elle ne se modifie jamais ([ADR-0009](ADR-0009-documents-legaux-jamais-reecrits.md)).
Elle doit aussi porter les conditions de paiement : la date d'échéance, le taux des pénalités de retard,
l'indemnité forfaitaire de 40 € pour frais de recouvrement due par un professionnel, et les conditions
d'escompte ([service-public, F23208](https://entreprendre.service-public.gouv.fr/vosdroits/F23208)). Dans le
produit, une facture naît d'un devis.

## Décision

1. **Un devis finalisé suffit pour émettre sa facture.** Le « bon pour accord » du client n'est pas
   enregistré par le produit.
2. **Un devis donne au plus une facture.** Le cas d'usage le vérifie, et le port le bloque sous son
   verrou, comme pour les numéros ([CDR-0007](../cdr/CDR-0007-numerotation-des-devis.md)) ; un second essai
   répond `409`.
3. **Le numéro est `F-2026-0001`** : une suite par émetteur, remise à zéro chaque année, continue et sans
   trou dans l'année. Il est déduit des factures stockées (le plus grand de l'année, plus un), sans compteur
   séparé ([ADR-0006](ADR-0006-persistance-par-port-secondaire.md)). Le domaine le calcule, et le port
   refuse un doublon. Les avoirs sont hors roadmap : il n'y a qu'une série.
4. **La facture recopie le devis tel qu'émis** : ses lignes, ses montants et sa ventilation de la TVA,
   sans rien recalculer, et ses mentions, sauf la validité du devis. Elle est stockée telle qu'émise, et
   créée sans jamais être réécrite.
5. **Les mentions de paiement sont composées à l'émission**, comme la mention de validité à la
   finalisation d'un devis (amendement du [CDR-0001](../cdr/CDR-0001-mentions-en-template-method.md)) :
   - l'échéance, 30 jours après l'émission par défaut, ou de 1 à 60 jours à la demande (60 jours est le
     plafond légal entre professionnels) ;
   - pas d'escompte pour paiement anticipé ;
   - des pénalités de retard à trois fois le taux d'intérêt légal, le minimum légal ;
   - l'indemnité forfaitaire de 40 €.
6. **Le PDF de la facture** est rendu par le même template que le devis, en réaction à `InvoiceIssued`,
   juste après l'enregistrement ([ADR-0005](ADR-0005-rendu-pdf-par-typst.md), amendement).

## Options écartées

| Option | Raison |
|---|---|
| Exiger l'acceptation du client avant de facturer | Une étape et un état de plus, que Samir n'a pas retenus. |
| Une facture qui lit ses montants dans le devis | Un document légal porte son propre contenu (ADR-0009). |
| Rejouer les règles de mention sur la facture | Elles connaissent le devis : il faudrait les généraliser, sans besoin. |
| Un `DocumentNumber` commun au devis et à la facture | Il faudrait retoucher le devis pour une abstraction à deux cas ; la fusion viendra avec un troisième type de document. |
| Un port de plus pour les PDF de facture | Ce serait une copie du premier : le port des PDF reçoit une référence de document. |

## Conséquences

- **La date d'exécution de la prestation** est obligatoire quand elle diffère de la date de la facture. Le
  produit ne la connaît pas encore : elle est réputée égale à la date d'émission. Comme le RCS de l'émetteur,
  c'est à reprendre.
