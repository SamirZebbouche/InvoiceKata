# language: fr
Fonctionnalité: Émettre la facture d'un devis
  Un émetteur émet la facture d'un devis finalisé. La facture reprend le devis tel qu'il a été émis, reçoit un
  numéro pris dans une suite continue et les mentions de paiement, puis elle ne change plus.

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

  Scénario: Émettre la facture d'un devis finalisé
    Quand j'émets la facture du devis
    Alors la facture est émise sous le numéro "F-2026-0001", datée du 30/11/2026, payable au plus tard le 30/12/2026, d'après le devis "D-2026-0001"
    Et les totaux de la facture sont :
      | total HT | total TVA | total TTC |
      | 1600,00  | 320,00    | 1920,00   |
    Et la facture porte les mentions suivantes :
      | mention                                                   |
      | Échéance : 30/12/2026.                                    |
      | Escompte pour paiement anticipé : néant.                  |
      | Pénalités de retard : trois fois le taux d'intérêt légal. |
      | Indemnité forfaitaire pour frais de recouvrement : 40 €.  |
    Et la facture ne porte pas de mention contenant "Devis valable"

  Scénario: Le délai de paiement se choisit à l'émission
    Quand j'émets la facture du devis, payable sous 45 jours
    Alors la facture est émise sous le numéro "F-2026-0001", datée du 30/11/2026, payable au plus tard le 14/01/2027, d'après le devis "D-2026-0001"

  Scénario: Les numéros de facture se suivent sans trou
    Étant donné "Batifolio" a déjà facturé 2 devis pour "Les Toits Rouges SARL"
    Quand j'émets la facture du devis
    Alors la facture est émise sous le numéro "F-2026-0003", datée du 30/11/2026, payable au plus tard le 30/12/2026, d'après le devis "D-2026-0001"

  Scénario: Un devis donne au plus une facture
    Étant donné la facture du devis est émise
    Quand j'essaie d'émettre la facture du devis
    Alors l'émission est refusée, car le devis a déjà sa facture

  Scénario: Un devis brouillon ne se facture pas
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 1        | jour  | 800,00           | 20          |
    Quand j'essaie d'émettre la facture du devis
    Alors l'émission est refusée, car le devis n'est pas finalisé
