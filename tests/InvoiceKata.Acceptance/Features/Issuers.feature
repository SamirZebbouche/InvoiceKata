# language: fr
Fonctionnalité: Configurer un émetteur
  Une entreprise cliente de l'éditeur configure son profil d'émetteur avant de pouvoir facturer.

  Scénario: Configurer un émetteur de type société
    Quand je configure l'émetteur de type société suivant :
      | champ           | valeur              |
      | dénomination    | Batifolio           |
      | forme juridique | SAS                 |
      | capital         | 20000               |
      | SIREN           | 912345678           |
      | numéro de TVA   | FR12912345678       |
      | adresse         | 12 rue des Artisans |
      | code postal     | 47000               |
      | ville           | Agen                |
    Alors l'émetteur est configuré avec ces informations
