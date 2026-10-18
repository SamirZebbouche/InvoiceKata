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
