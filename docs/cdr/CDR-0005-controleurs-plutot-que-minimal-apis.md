# CDR-0005 : Des contrôleurs plutôt que des minimal APIs

- **Statut** : accepté le 2026-10-18 · en place
- **Décideur** : Samir

## Problème

Les routes étaient déclarées en minimal APIs : une méthode d'extension `Map…Endpoints` par ressource,
appelée une à une dans `Program.cs`. Samir n'en voulait pas : « on appelle des `Map` dans tous les
sens ».

## Décision

1. **Une ressource = un contrôleur** `[ApiController]`, avec des routes en attributs : `IssuersController`
   (`issuers`), `CustomersController` (`issuers/{issuerId}/customers`), `QuotesController`
   (`issuers/{issuerId}/quotes`).
2. **`Program.cs` déclare `AddControllers()` et un seul `MapControllers()`.** Ajouter une ressource ne
   touche plus `Program.cs`, en dehors de l'enregistrement des cas d'usage.
3. **Les cas d'usage sont injectés par le constructeur du contrôleur** (règle
   `dotnet-dependency-injection`).
4. **Les DTO de requête et de réponse vivent à côté de leur contrôleur** et restent primitifs
   ([CDR-0003](CDR-0003-objets-valeur-et-proprietes-requises.md)).

## Options écartées

| Option | Raison |
|---|---|
| Garder les minimal APIs | Un `Map…` par ressource à appeler dans `Program.cs`, et des routes éparpillées dans des lambdas. |
| Des minimal APIs découvertes par réflexion | Elles retireraient les appels de `Program.cs`, mais au prix d'une mécanique maison, alors que MVC le fait déjà. |

## Conséquences

- Le refactoring n'a changé aucun test : les 19 tests d'API passent tels quels.
- Avec `[ApiController]` et les types non nullables, un champ obligatoire absent du JSON est refusé en
  `400` (Problem Details) avant d'atteindre le cas d'usage. C'est plus strict qu'avec les minimal APIs, et
  un test d'API le couvre.
