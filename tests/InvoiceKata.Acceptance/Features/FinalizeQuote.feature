# language: fr
Fonctionnalité: Finaliser un devis
  Un émetteur finalise un devis brouillon. Le devis reçoit alors un numéro, une date et une date de
  validité, puis il est figé : c'est le document envoyé au client.

  Contexte:
    Étant donné l'émetteur de type société "Batifolio" configuré
    Et "Batifolio" a enregistré le client professionnel "Les Toits Rouges SARL"
    Et nous sommes le 18/10/2026
    Et un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour  | 800,00           | 20          |

  Scénario: Finaliser un devis lui donne un numéro, une date et une validité de 30 jours
    Quand je finalise le devis
    Alors le devis est finalisé sous le numéro "D-2026-0001", daté du 18/10/2026 et valable jusqu'au 17/11/2026
    Et le devis porte la mention "Devis valable 30 jours, jusqu'au 17/11/2026."

  Scénario: La durée de validité se choisit à la finalisation
    Quand je finalise le devis pour une validité de 15 jours
    Alors le devis est finalisé sous le numéro "D-2026-0001", daté du 18/10/2026 et valable jusqu'au 02/11/2026
    Et le devis porte la mention "Devis valable 15 jours, jusqu'au 02/11/2026."

  Scénario: Les numéros se suivent pour un même émetteur
    Étant donné "Batifolio" a déjà finalisé 2 devis pour "Les Toits Rouges SARL"
    Quand je finalise le devis
    Alors le devis est finalisé sous le numéro "D-2026-0003", daté du 18/10/2026 et valable jusqu'au 17/11/2026

  Scénario: Chaque émetteur a sa propre numérotation
    Étant donné l'émetteur de type société "Atelier Martin" configuré
    Et "Atelier Martin" a enregistré le client professionnel "Menuiserie Dupont"
    Et "Atelier Martin" a déjà finalisé 2 devis pour "Menuiserie Dupont"
    Quand je finalise le devis
    Alors le devis est finalisé sous le numéro "D-2026-0001", daté du 18/10/2026 et valable jusqu'au 17/11/2026

  Scénario: La numérotation repart à 1 au changement d'année
    Étant donné nous sommes le 31/12/2026
    Et "Batifolio" a déjà finalisé 2 devis pour "Les Toits Rouges SARL"
    Et nous sommes le 01/01/2027
    Quand je finalise le devis
    Alors le devis est finalisé sous le numéro "D-2027-0001", daté du 01/01/2027 et valable jusqu'au 31/01/2027

  Scénario: Un devis finalisé refuse l'ajout d'une ligne
    Étant donné le devis est finalisé
    Quand j'essaie d'ajouter au devis la ligne suivante :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Formation   | 1        | jour  | 600,00           | 20          |
    Alors la modification est refusée, car le devis est finalisé
    Et le devis contient 1 ligne

  Scénario: Un devis finalisé refuse le retrait d'une ligne
    Étant donné le devis est finalisé
    Quand j'essaie de retirer la ligne "Audit" du devis
    Alors la modification est refusée, car le devis est finalisé
    Et le devis contient 1 ligne

  Scénario: Un devis ne se finalise qu'une fois
    Étant donné le devis est finalisé
    Quand j'essaie de finaliser le devis
    Alors la modification est refusée, car le devis est finalisé
    Et le devis est finalisé sous le numéro "D-2026-0001", daté du 18/10/2026 et valable jusqu'au 17/11/2026

  Scénario: Un devis sans ligne ne peut pas être finalisé
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Quand j'essaie de finaliser le devis
    Alors la finalisation est refusée, car le devis n'a aucune ligne
