# CDR-0001 : Une mention légale = une règle en Template Method

- **Statut** : accepté le 2026-10-18 · pas encore mis en œuvre
- **Décideur** : Samir

## Problème

Les mentions légales sont chorégraphiées : chaque mention réagit aux événements du devis, réévalue l'état
et pose ou retire sa mention ([ADR-0003](../adr/ADR-0003-reactions-par-evenements-idempotentes.md),
amendement). Il faut pouvoir **ajouter une mention sans modifier l'existant**, et rendre impossible un
gestionnaire qui ne serait pas fondé sur l'état.

## Décision

1. **Une classe de base, `LegalMentionRule`, fixe le mécanisme une fois pour toutes** (Template Method).
   À chaque événement du devis, elle évalue la règle sur l'état courant, puis pose ou retire la mention.
   Le résultat est idempotent par construction.
2. **Chaque mention est une classe dérivée** qui ne fournit que trois choses : son code, sa condition
   d'application et sa composition.

   ```csharp
   public sealed class ReducedVatRateMention : LegalMentionRule
   {
       protected override MentionCode Code => MentionCode.ReducedVatRate;
       protected override bool AppliesTo(Quote quote, BillingContext context) => quote.Lines.Any(l => l.VatRate.Percent == 10m);
       protected override LegalMention Compose(Quote quote, BillingContext context, IMentionTexts texts) => new(Code, texts.ReducedVatRate());
   }
   ```

3. **Une mention stockée est `LegalMention(Code, Text)`.**
   - Le **code** identifie la mention. Il sert aussi à Factur-X, qui exige des mentions codées.
   - Le **texte** vient d'un **catalogue unique** (`IMentionTexts`), pour que le texte destiné à
     l'utilisateur reste rassemblé et traduisible (règle de langue du CLAUDE.md).
   - On n'ajoutera des données structurées à une mention que le jour où un adaptateur en aura besoin.
4. **Les règles sont découvertes par injection de dépendances.** Ajouter une mention demande une classe,
   un texte dans le catalogue et un test, et rien d'autre.

## Options écartées

| Option | Raison |
|---|---|
| Un builder | Il règle l'ordre de construction d'un objet, pas l'extension. |
| Un résolveur central avec un `switch` sur le code | Chaque nouvelle mention le modifierait, et les cas particuliers y signaleraient un mauvais modèle. |
| Un type de données par mention dans une structure commune | La structure commune changerait à chaque nouvelle mention. |
| Un dictionnaire générique de données | On perdrait le typage, et les erreurs n'apparaîtraient qu'à l'exécution. |
| Des gestionnaires écrits librement, sans classe de base | Rien n'empêcherait un gestionnaire fondé sur le delta, faux dès que deux lignes déclenchent la même mention. |

## Conséquences

- La base est le seul endroit où vivent l'approche fondée sur l'état et l'idempotence. Un test de rejeu
  sur la base les couvre pour toutes les mentions.
- Le catalogue des textes grandit avec chaque mention : c'est voulu, puisqu'il est la future table de
  traduction.
