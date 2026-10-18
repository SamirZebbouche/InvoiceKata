# CDR-0003 : Objets valeur contre l'inversion, propriétés `required` pour les enregistrements

- **Statut** : accepté le 2026-10-18 · en place
- **Décideur** : Samir

## Problème

Samir a relevé une obsession des types primitifs : `Address(string, string, string)` et des commandes qui
alignaient quatre `string`. Un SIREN pouvait prendre en silence la place d'un n° de TVA. L'excès inverse,
des objets valeur partout, est tout aussi nuisible (règle `arch-no-uniform-patterns`).

## Décision

1. **Un objet valeur seulement si des primitives peuvent être inversées dans un appel, ou s'il porte du
   comportement.**

   | Type | Raison |
   |---|---|
   | `Siren`, `VatNumber` | Deux chaînes côte à côte, faciles à inverser. Ils porteront aussi la validation et la clé de TVA. |
   | `Money` | Il porte la règle d'arrondi ([ADR-0008](../adr/ADR-0008-arrondis.md)). |
   | `Quantity`, `VatRate`, `DiscountRate` | Des décimaux côte à côte avec le prix, et deux pourcentages faciles à inverser. `VatRate` calcule la TVA d'une base, `DiscountRate` le prix net, sans l'arrondir. |
   | `IssuerId`, `CustomerId`, `QuoteId`, `QuoteLineId` | Des GUID interchangeables sinon ([ADR-0007](../adr/ADR-0007-saas-multi-emetteur.md)). |

2. **Un enregistrement à plusieurs champs de même type prend des propriétés `required`** construites par
   initialiseur (`Address`, `NewQuoteLine`). Chaque champ est nommé à l'appel, sans un type par champ.
3. **Les objets valeur restent dans le domaine.** Les DTO de l'API sont primitifs, et la frontière
   traduit.
4. **Un ensemble fermé est une énumération**, avec un `default` qui lève une exception et un test qui
   parcourt toutes les valeurs (`LegalForm`, règle `dotnet-csharp-idioms`).

## Options écartées

| Option | Raison |
|---|---|
| Des chaînes et des décimaux partout | Les inversions passent en silence. |
| Un type par champ (`Street`, `PostalCode`, `City`) | Des types sans comportement : c'est l'obsession des objets valeur. |
| Des objets valeur jusque dans le JSON de l'API | Le domaine et la présentation se mélangent. |

## Conséquences

- La validation (format du SIREN, clé du n° de TVA) reste à écrire, comme un comportement à part, avec ses
  tests.
