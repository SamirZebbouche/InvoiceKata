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
