# language: fr
@pdf
Fonctionnalité: Le PDF de la facture
  Une facture émise a son PDF, rendu par le même template que le devis.

  Contexte:
    Étant donné l'émetteur de type société "Batifolio" configuré
    Et "Batifolio" a enregistré le client professionnel "Les Toits Rouges SARL"
    Et nous sommes le 18/10/2026
    Et un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour  | 800,00           | 20          |
    Et le devis est finalisé
    Et nous sommes le 30/11/2026

  Scénario: Le PDF d'une facture porte son numéro, son devis, ses totaux et ses mentions de paiement
    Quand j'émets la facture du devis
    Alors le PDF de la facture contient :
      | texte                                                    |
      | FACTURE                                                  |
      | F-2026-0001                                              |
      | 30/11/2026                                               |
      | D'après le devis n° D-2026-0001 du 18/10/2026            |
      | Total TTC                                                |
      | 1 920,00 €                                               |
      | Échéance : 30/12/2026.                                   |
      | Escompte pour paiement anticipé : néant.                 |
      | Pénalités de retard : trois fois le taux d'intérêt légal. |
      | Indemnité forfaitaire pour frais de recouvrement : 40 €. |
      | Batifolio, SAS au capital de 20 000 €, SIREN 912 345 678 |
      | page 1 / 1                                               |
    Et le PDF de la facture ne contient pas "Bon pour accord"
