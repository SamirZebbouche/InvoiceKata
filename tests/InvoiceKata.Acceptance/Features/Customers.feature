# language: fr
Fonctionnalité: Enregistrer un client
  Un émetteur enregistre ses clients avant de leur établir des devis. Chaque émetteur ne voit que les siens.

  Contexte:
    Étant donné l'émetteur de type société "Batifolio" configuré

  Scénario: Enregistrer un client professionnel
    Quand "Batifolio" enregistre le client professionnel suivant :
      | champ         | valeur                |
      | nom           | Les Toits Rouges SARL |
      | SIREN         | 834567890             |
      | numéro de TVA | FR45834567890         |
      | adresse       | 4 place du Marché     |
      | code postal   | 33000                 |
      | ville         | Bordeaux              |
    Alors "Batifolio" retrouve ce client professionnel avec ces informations

  Scénario: Un émetteur ne voit pas les clients d'un autre émetteur
    Étant donné l'émetteur de type société "Atelier Martin" configuré
    Et "Batifolio" a enregistré le client professionnel "Les Toits Rouges SARL"
    Quand "Atelier Martin" cherche ce client
    Alors le client est introuvable
