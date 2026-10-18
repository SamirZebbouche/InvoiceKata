# language: fr
@pdf
Fonctionnalité: Le PDF du devis
  Un devis finalisé a son PDF : c'est le document que l'émetteur envoie à son client.

  Contexte:
    Étant donné l'émetteur de type société "Batifolio" configuré
    Et "Batifolio" a enregistré le client professionnel "Les Toits Rouges SARL"
    Et nous sommes le 18/10/2026
    Et un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation     | quantité | unité   | prix unitaire HT | taux de TVA |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
      | Audit           | 2        | jour    | 800,00           | 20          |

  Scénario: Le PDF d'un devis finalisé porte son numéro, ses lignes, ses totaux et ses mentions
    Quand je finalise le devis
    Alors le PDF du devis contient :
      | texte                                                                                                                               |
      | DEVIS                                                                                                                               |
      | D-2026-0001                                                                                                                         |
      | 18/10/2026                                                                                                                          |
      | Pose de faïence                                                                                                                     |
      | Total TTC                                                                                                                           |
      | 2 470,00 €                                                                                                                          |
      | Taux de TVA réduit de 10 % appliqué aux travaux d'amélioration d'un logement achevé depuis plus de deux ans (art. 279-0 bis du CGI). |
      | Devis valable 30 jours, jusqu'au 17/11/2026.                                                                                        |
      | Batifolio, SAS au capital de 20 000 €, SIREN 912 345 678                                                                            |
      | Bon pour accord                                                                                                                     |
      | page 1 / 1                                                                                                                          |

  Scénario: Rejouer la finalisation ne refait pas le PDF
    Étant donné le devis est finalisé
    Quand l'événement de finalisation du devis est rejoué
    Alors le PDF du devis n'a pas changé

  Scénario: Un devis brouillon n'a pas de PDF
    Quand j'essaie d'obtenir le PDF du devis
    Alors la demande est refusée, car le devis n'est pas encore finalisé
