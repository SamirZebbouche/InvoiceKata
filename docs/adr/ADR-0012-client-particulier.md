# ADR-0012 : Un client particulier

- **Statut** : accepté le 2026-10-19
- **Date** : 2026-10-19
- **Décideur** : Samir

## Contexte

Le produit refuse tout devis pour un particulier : la liste blanche ne connaît que des clients professionnels
([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)). Le particulier s'enregistre déjà comme client, sans
SIREN ni numéro de TVA. Ses règles, vérifiées sur les sites officiels le 19 octobre 2026 :

- **Les mentions de la facture** s'appliquent « que le client soit un particulier, un professionnel ou une entité
  publique » : l'échéance, l'escompte et le taux des pénalités de retard. Le SIREN et le numéro de TVA du client ne
  concernent qu'une entreprise. L'adresse du client figure sur la facture, « sauf opposition pour un particulier »
  ([service-public, F31808](https://entreprendre.service-public.gouv.fr/vosdroits/F31808), vérifiée le 11 août 2026).
- **L'indemnité forfaitaire de 40 €** « ne peut être réclamée que par un client professionnel » (F31808). La DGCCRF
  recommande de ne pas la faire figurer sur les factures adressées à des consommateurs
  ([questions-réponses de la DGCCRF](https://www.economie.gouv.fr/files/directions_services/dgccrf/actualites/breves/2013/QR_indemnite_forfaitaire.pdf)).
- **Les pénalités de retard** : « trois fois le taux d'intérêt légal » est le minimum que le code de commerce fixe
  entre professionnels. Face à un consommateur, le droit commun s'applique : le retard de paiement d'une somme
  d'argent est réparé par l'intérêt au taux légal, à compter de la mise en demeure
  ([art. 1231-6 du code civil](https://www.legifrance.gouv.fr/codes/article_lc/LEGIARTI000032010133)).
- **Le médiateur de la consommation** : le professionnel inscrit ses coordonnées sur son site, ses conditions
  générales de vente et ses bons de commande, ou à défaut sur tout autre support adapté
  ([DGCCRF](https://www.economie.gouv.fr/dgccrf/les-fiches-pratiques/la-mediation-de-la-consommation-ce-que-vous-devez-savoir)).

## Décision

1. **La liste blanche gagne deux lignes** : société × particulier et EI × particulier. Les règles propres au
   particulier ne dépendent que du client, et valent pour les deux émetteurs. La liste couvre désormais toutes les
   paires connues : le monde reste fermé ([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)), et un
   type futur, une entité publique par exemple, sera refusé tant qu'il n'y figure pas.
2. **Les mentions de paiement deviennent des règles de facture**, sur le modèle des mentions du devis
   ([CDR-0001](../cdr/CDR-0001-mentions-en-template-method.md)) : une règle par mention, qui dit quand elle
   s'applique et ce qu'elle dit. Leur interface est rangée dans `Application/Invoices/Mentions`. `Invoice.IssueFor`
   reçoit les mentions retenues au lieu d'un enregistrement à quatre textes fixes.
3. **L'indemnité de 40 € ne s'applique qu'à un client professionnel.**
4. **Les pénalités de retard restent une seule règle**, dont le texte dépend du type de client : « trois fois le taux
   d'intérêt légal » pour un professionnel, « intérêts au taux légal, à compter de la mise en demeure (art. 1231-6 du
   code civil) » pour un particulier. Deux règles ne posent jamais une mention sous le même code
   ([ADR-0011](ADR-0011-ei-en-franchise-de-tva.md), décision 5).
5. **L'état de la facture exige l'échéance, l'escompte et les pénalités.** La facture ne connaît pas le type de son
   client : l'indemnité est vérifiée par le validateur final, à qui le document résolu donne désormais le type de
   client. Une facture déjà émise garde ses quatre mentions, et se relit telle quelle.
6. **L'échéance reste réglable de 1 à 60 jours** pour tous les clients : la loi ne plafonne pas le délai d'un
   particulier, mais rien ne demande un réglage à part.

## Hors périmètre

| Règle | Raison |
|---|---|
| Le médiateur de la consommation | L'obligation vise d'abord le site, les conditions générales et les bons de commande, que le produit ne produit pas. Risque connu : un devis signé qui sert de bon de commande devrait le porter. Il faudrait alors donner un médiateur à l'émetteur. |
| Les mentions de devis propres à un secteur (bâtiment, dépannage à domicile…) | Le produit ne connaît pas le secteur de l'émetteur. |
| Le droit de rétractation de 14 jours | Il dépend du lieu de conclusion du contrat (hors établissement, à distance), que le produit ne connaît pas. |
| La garantie légale de conformité | Elle concerne certaines catégories de biens, et le produit ne connaît pas la nature de ce qu'il facture. |
| L'opposition du particulier à l'affichage de son adresse | Aucun client ne l'a demandé ; l'adresse reste obligatoire à l'enregistrement. |

## Options écartées

| Option | Raison |
|---|---|
| `IssueInvoice` qui choisit les textes selon le type de client | Un `if` sur le profil, que l'[ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md) refuse. |
| Le type de client recopié sur le devis, et la facture qui décide | Un `switch` sur le profil dans le domaine, et une migration de l'état du devis sans besoin. |
| « Trois fois le taux d'intérêt légal » pour tous les clients | Un minimum légal entre professionnels, mais une clause pénale face à un consommateur, qui pourrait être jugée abusive. |
| Deux règles de pénalités, une par type de client | Chacune retirerait la mention que l'autre vient de poser. |
