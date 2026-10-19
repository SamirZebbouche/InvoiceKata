# language: fr
Fonctionnalité: Le devis et la facture d'un particulier
  Un particulier n'a ni SIREN ni numéro de TVA. Une société comme un entrepreneur individuel peuvent lui établir
  un devis, puis sa facture. L'indemnité forfaitaire de 40 € ne se réclame qu'à un professionnel, et les pénalités
  de retard d'un particulier suivent le droit commun : l'intérêt au taux légal (ADR-0012).

  Contexte:
    Étant donné l'émetteur de type société "Batifolio" configuré
    Et "Batifolio" a enregistré le client particulier "Jeanne Martin"
    Et nous sommes le 18/10/2026

  Scénario: Une société établit un devis pour un particulier
    Quand "Batifolio" crée un devis pour "Jeanne Martin"
    Alors le devis est un brouillon sans ligne, établi pour "Jeanne Martin"
    Et le devis porte la mention "Batifolio, SAS au capital de 20 000 €, SIREN 912 345 678"

  Scénario: Un entrepreneur individuel établit un devis pour un particulier
    Étant donné l'entrepreneur individuel "Jean Dupont" configuré
    Et "Jean Dupont" a enregistré le client particulier "Paul Leroy"
    Quand "Jean Dupont" crée un devis pour "Paul Leroy"
    Alors le devis est un brouillon sans ligne, établi pour "Paul Leroy"
    Et le devis porte la mention "TVA non applicable, art. L. 223 et s. du code des impositions sur les biens et services (CIBS)"

  @pdf
  Scénario: Le PDF du devis d'un particulier porte son nom et son adresse
    Étant donné un devis brouillon de "Batifolio" pour "Jeanne Martin"
    Et le devis contient les lignes suivantes :
      | désignation     | quantité | unité   | prix unitaire HT | taux de TVA |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
    Quand je finalise le devis
    Alors le PDF du devis contient :
      | texte         |
      | Jeanne Martin |
      | 3 rue des Lilas |
      | 550,00 €      |

  Scénario: La facture d'un particulier ne réclame pas l'indemnité forfaitaire de 40 €
    Étant donné un devis brouillon de "Batifolio" pour "Jeanne Martin"
    Et le devis contient les lignes suivantes :
      | désignation     | quantité | unité   | prix unitaire HT | taux de TVA |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
    Et le devis est finalisé
    Quand j'émets la facture du devis
    Alors la facture porte les mentions suivantes :
      | mention                                                                                                  |
      | Échéance : 17/11/2026.                                                                                   |
      | Escompte pour paiement anticipé : néant.                                                                 |
      | Pénalités de retard : intérêts au taux légal, à compter de la mise en demeure (art. 1231-6 du code civil). |
    Et la facture ne porte pas de mention contenant "40 €"

  @pdf
  Scénario: Le PDF de la facture d'un particulier ne porte pas l'indemnité forfaitaire
    Étant donné un devis brouillon de "Batifolio" pour "Jeanne Martin"
    Et le devis contient les lignes suivantes :
      | désignation     | quantité | unité   | prix unitaire HT | taux de TVA |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
    Et le devis est finalisé
    Quand j'émets la facture du devis
    Alors le PDF de la facture contient :
      | texte                                       |
      | Jeanne Martin                               |
      | intérêts au taux légal                      |
    Et le PDF de la facture ne contient pas "Indemnité forfaitaire"
