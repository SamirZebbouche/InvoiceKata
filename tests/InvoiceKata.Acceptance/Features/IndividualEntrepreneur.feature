# language: fr
Fonctionnalité: Le devis et la facture d'un entrepreneur individuel en franchise de TVA
  Un entrepreneur individuel (EI) en franchise en base ne facture pas de TVA. Ses devis et ses factures le
  disent, et portent la mention « EI » à côté de son nom.

  Contexte:
    Étant donné l'entrepreneur individuel "Jean Dupont" configuré
    Et "Jean Dupont" a enregistré le client professionnel "Les Toits Rouges SARL"
    Et nous sommes le 18/10/2026
    Et un devis brouillon de "Jean Dupont" pour "Les Toits Rouges SARL"

  Scénario: Le devis d'un EI en franchise ne porte pas de TVA
    Quand j'ajoute au devis la ligne suivante :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour  | 800,00           | 0           |
    Alors les totaux du devis sont :
      | total HT | total TVA | total TTC |
      | 1600,00  | 0,00      | 1600,00   |
    Et le devis porte la mention "TVA non applicable, art. L. 223 et s. du code des impositions sur les biens et services (CIBS)"
    Et le devis porte la mention "Jean Dupont EI, SIREN 853 123 456"

  Scénario: Une ligne avec un taux de TVA est refusée
    Quand j'essaie d'ajouter au devis la ligne suivante :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour  | 800,00           | 20          |
    Alors la ligne est refusée, car le devis est en franchise de TVA
    Et le devis contient 0 ligne

  Scénario: La facture d'un EI reprend la franchise
    Étant donné le devis contient les lignes suivantes :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour  | 800,00           | 0           |
    Et le devis est finalisé
    Quand j'émets la facture du devis
    Alors les totaux de la facture sont :
      | total HT | total TVA | total TTC |
      | 1600,00  | 0,00      | 1600,00   |
    Et la facture porte les mentions suivantes :
      | mention                                                                                     |
      | TVA non applicable, art. L. 223 et s. du code des impositions sur les biens et services (CIBS) |
      | Jean Dupont EI, SIREN 853 123 456                                                           |
      | Indemnité forfaitaire pour frais de recouvrement : 40 €.                                    |
