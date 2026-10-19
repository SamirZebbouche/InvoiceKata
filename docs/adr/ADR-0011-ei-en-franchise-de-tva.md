# ADR-0011 : Un second contexte, l'entrepreneur individuel en franchise de TVA

- **Statut** : accepté le 2026-10-19
- **Date** : 2026-10-19
- **Décideur** : Samir

## Contexte

Le produit ne prend en charge qu'un contexte : une société au régime normal de TVA facture un professionnel
([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)). Le second est un entrepreneur individuel (EI) en
franchise en base de TVA, qui facture un professionnel. Ses règles, vérifiées sur les sites officiels le
19 octobre 2026 :

- **La mention « EI »** : le nom de l'entrepreneur est « précédé ou suivi de la mention "Entrepreneur individuel
  ou EI" » sur tous ses documents professionnels, devis et factures compris (article R526-27 du code de
  commerce, en vigueur depuis le 15 mai 2022 ; [service-public, A15744](https://entreprendre.service-public.gouv.fr/actualites/A15744)).
- **La franchise en base** : la facture porte « TVA non applicable, art. L. 223 et s. du code des impositions
  sur les biens et services (CIBS) » depuis le 1er septembre 2026. L'ancienne référence, « art. 293 B du CGI »,
  est tolérée jusqu'au 31 décembre 2027 ([service-public, F23208](https://entreprendre.service-public.gouv.fr/vosdroits/F23208)).
- **Le SIREN** est obligatoire. Un EI en franchise ne facture pas de TVA, et n'a pas à porter de numéro de TVA.

## Décision

1. **Le même agrégat `Issuer`, avec un type et un régime de TVA.** Une société est au régime normal, et porte
   sa forme juridique, son capital et son numéro de TVA. Un EI est en franchise, et ne porte que son nom, son
   SIREN et son adresse. Les invariants dépendent du type, comme pour le client. L'état de l'émetteur passe
   en version 2, et un émetteur v1 se lit comme une société au régime normal.
2. **Le régime de TVA est une Strategy** (décision prévue par l'ADR-0002 dès le deuxième régime) : le régime
   normal calcule la TVA par taux ([ADR-0008](ADR-0008-arrondis.md)) ; la franchise n'en calcule aucune, et
   le total TTC est le total HT. Le devis garde le régime de son émetteur, fixé à sa création ; son état passe
   en version 3.
3. **Une ligne à taux non nul est refusée sur un devis en franchise** (`422`) : le monde reste fermé, mieux
   vaut refuser qu'émettre une facture fausse. Le taux reste obligatoire dans la requête, et vaut 0 pour un EI.
4. **La liste blanche gagne une ligne** : EI × professionnel.
5. **Les mentions** : la mention d'identité légale reste une seule règle, dont le texte dépend du type
   d'émetteur (« Jean Dupont EI, SIREN … ») ; deux règles ne posent jamais une mention sous le même code. Une
   règle de plus pose la mention de franchise, avec la référence au CIBS.
6. **Un template par type d'émetteur** ([ADR-0005](ADR-0005-rendu-pdf-par-typst.md), décision 3) : `ei.typ`,
   sans colonne ni ligne de TVA, choisi par l'adaptateur selon le type d'émetteur du document résolu.

## Options écartées

| Option | Raison |
|---|---|
| La référence « art. 293 B du CGI » | Une tolérance qui prend fin en 2027 ; la règle est la référence au CIBS. |
| Un agrégat EI à part | Il dédoublerait le dépôt, les routes et le contrat de l'émetteur. |
| Ignorer en silence le taux d'une ligne en franchise | Ce qui a été saisi et ce qui est facturé diffèreraient. |
| Un taux facultatif dans la requête, à 0 par défaut | Un devis de société sans taux passerait en silence à 0 %. |
| Un seul template, qui masque la TVA selon le régime | Une condition sur le profil légal dans le template ([ADR-0005](ADR-0005-rendu-pdf-par-typst.md), décision 7). |
| Deux règles d'identité légale sous le même code | Chacune retirerait la mention que l'autre vient de poser. |
