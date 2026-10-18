# language: fr
Fonctionnalité: Créer un devis
  Un émetteur crée un devis brouillon pour l'un de ses clients, puis y ajoute des lignes.

  Contexte:
    Étant donné l'émetteur de type société "Batifolio" configuré
    Et "Batifolio" a enregistré le client professionnel "Les Toits Rouges SARL"

  Scénario: Créer un devis brouillon pour un client
    Quand "Batifolio" crée un devis pour "Les Toits Rouges SARL"
    Alors le devis est un brouillon sans ligne, établi pour "Les Toits Rouges SARL"

  Scénario: Un émetteur ne peut pas établir de devis pour le client d'un autre émetteur
    Étant donné l'émetteur de type société "Atelier Martin" configuré
    Quand "Atelier Martin" crée un devis pour "Les Toits Rouges SARL"
    Alors le client est introuvable

  Scénario: Ajouter une ligne calcule les totaux du devis
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Quand j'ajoute au devis la ligne suivante :
      | désignation | quantité | unité | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour  | 800,00           | 20          |
    Alors le devis contient 1 ligne
    Et les totaux du devis sont :
      | total HT | total TVA | total TTC |
      | 1600,00  | 320,00    | 1920,00   |
    Et la TVA du devis est ventilée ainsi :
      | taux | base HT | TVA    |
      | 20   | 1600,00 | 320,00 |

  Scénario: Retirer une ligne recalcule les totaux du devis
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation | quantité | unité   | prix unitaire HT | taux de TVA |
      | Audit       | 2        | jour    | 800,00           | 20          |
      | Formation   | 1        | forfait | 500,00           | 20          |
    Quand je retire la ligne "Formation" du devis
    Alors le devis contient 1 ligne
    Et les totaux du devis sont :
      | total HT | total TVA | total TTC |
      | 1600,00  | 320,00    | 1920,00   |

  Scénario: Un devis à deux taux de TVA ventile la TVA par taux
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Quand j'ajoute au devis la ligne suivante :
      | désignation    | quantité | unité   | prix unitaire HT | taux de TVA |
      | Audit          | 2        | jour    | 800,00           | 20          |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
    Alors les totaux du devis sont :
      | total HT | total TVA | total TTC |
      | 2100,00  | 370,00    | 2470,00   |
    Et la TVA du devis est ventilée ainsi :
      | taux | base HT | TVA    |
      | 10   | 500,00  | 50,00  |
      | 20   | 1600,00 | 320,00 |

  Scénario: Une remise s'applique au prix unitaire sans l'arrondir
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Quand j'ajoute au devis la ligne suivante :
      | désignation | quantité | unité | prix unitaire HT | remise | taux de TVA |
      | Faïence     | 18,72    | u     | 33,33            | 10     | 10          |
    Alors les totaux du devis sont :
      | total HT | total TVA | total TTC |
      | 561,54   | 56,15     | 617,69    |

  Scénario: Un contexte de facturation non pris en charge est refusé dès la création du devis
    Étant donné "Batifolio" a enregistré le client particulier "Jeanne Martin"
    Quand "Batifolio" crée un devis pour "Jeanne Martin"
    Alors le contexte de facturation n'est pas pris en charge

  Scénario: L'identité légale de l'émetteur figure sur le devis dès sa création
    Quand "Batifolio" crée un devis pour "Les Toits Rouges SARL"
    Alors le devis porte la mention "Batifolio, SAS au capital de 20 000 €, SIREN 912 345 678"

  Scénario: La mention du taux réduit suit les lignes à 10 %
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation     | quantité | unité   | prix unitaire HT | taux de TVA |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
      | Carrelage       | 1        | forfait | 300,00           | 10          |
      | Audit           | 1        | jour    | 800,00           | 20          |
    Alors le devis porte la mention "Taux de TVA réduit de 10 % appliqué aux travaux d'amélioration d'un logement achevé depuis plus de deux ans (art. 279-0 bis du CGI)."
    Quand je retire la ligne "Pose de faïence" du devis
    Alors le devis porte la mention "Taux de TVA réduit de 10 % appliqué aux travaux d'amélioration d'un logement achevé depuis plus de deux ans (art. 279-0 bis du CGI)."
    Quand je retire la ligne "Carrelage" du devis
    Alors le devis ne porte pas de mention contenant "Taux de TVA réduit"

  Scénario: Rejouer les événements d'un devis ne change pas ses mentions
    Étant donné un devis brouillon de "Batifolio" pour "Les Toits Rouges SARL"
    Et le devis contient les lignes suivantes :
      | désignation     | quantité | unité   | prix unitaire HT | taux de TVA |
      | Pose de faïence | 1        | forfait | 500,00           | 10          |
    Quand tous les événements du devis sont rejoués
    Alors le devis porte exactement les mêmes mentions qu'avant
