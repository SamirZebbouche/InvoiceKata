# CDR-0001 : Une mention légale = une règle en Template Method

- **Statut** : accepté le 2026-10-18 · en place (`LegalMentionRule` ; règles : `IssuerLegalIdentityMention`,
  `ReducedVatRateMention`)
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
   public sealed class IssuerLegalIdentityMention(IMentionTexts texts) : LegalMentionRule(texts)
   {
       protected override MentionCode Code => MentionCode.IssuerLegalIdentity;
       protected override bool AppliesTo(QuoteScope scope) => scope.Issuer.Kind == IssuerKind.Company;
       protected override string Compose(QuoteScope scope) => Texts.IssuerLegalIdentity(scope.Issuer);
   }
   ```

   `QuoteScope` donne au gestionnaire le devis, son émetteur et son client.

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

## Amendement du 2026-10-18 : la mention de validité est composée à la finalisation

La durée de validité n'est pas déduite du contenu du devis. C'est un paramètre de la finalisation
(30 jours par défaut, réglable à la requête). Le cas d'usage de finalisation demande donc son texte au
catalogue `IMentionTexts`, et le domaine pose la mention et fige le devis **en une seule opération**.
Aucune règle chorégraphiée ne la produit. Un devis finalisé refuse ensuite toute modification, mentions
comprises, sans exception ([ADR-0009](../adr/ADR-0009-documents-legaux-jamais-reecrits.md)).

| Option écartée | Raison |
|---|---|
| Une règle de plus, qui réagit à `QuoteFinalized` avant l'enregistrement | Le devis finalisé devrait encore accepter des changements de mentions : le domaine ne garantirait plus que ce qui est émis ne change jamais. |
| Un événement `QuoteFinalizing`, levé avant le gel | Un concept nouveau, qui n'existerait que pour ordonner les appels. |
| La phrase de validité écrite par le template PDF | Le texte sortirait du catalogue unique, et la mention n'apparaîtrait pas dans l'API. |

Les mentions de contexte gardent la valeur qu'elles avaient dans le brouillon. Si un émetteur peut un jour
modifier son profil, la finalisation devra les réévaluer avant le gel.

## Amendement du 2026-10-19 : les mentions de paiement de la facture sont aussi des règles

Les mentions de paiement dépendent du client : l'indemnité de 40 € ne se réclame qu'à un professionnel, et les
pénalités d'un particulier suivent le code civil ([ADR-0012](../adr/ADR-0012-client-particulier.md)). Elles
deviennent des règles, sur le même modèle : la base `InvoiceMentionRule` fixe le mécanisme, et chaque règle dit
quand elle s'applique et ce qu'elle dit. Une facture ne change jamais : ses règles ne réagissent à aucun
événement. Le cas d'usage d'émission les interroge une fois, et `Invoice.IssueFor` reçoit les mentions
retenues.

| Option écartée | Raison |
|---|---|
| `IssueInvoice` qui choisit les textes selon le type de client | Un `if` sur le profil, que l'[ADR-0002](../adr/ADR-0002-variations-selon-emetteur-et-client.md) refuse. |
| Des règles de facture qui réagissent à `InvoiceIssued`, comme celles du devis | La facture devrait accepter des mentions après son émission : le domaine ne garantirait plus qu'elle ne change jamais. |

## Conséquences

- La classe de base `LegalMentionRule` est le seul endroit où vivent l'approche fondée sur l'état et
  l'idempotence. Un scénario de rejeu les couvre pour toutes les mentions : rejouer deux fois toute
  l'histoire d'un devis ne change pas ses mentions. Il attrape une règle qui garderait un état d'un
  événement à l'autre, par exemple un compteur.
- La version fondée sur le delta a été écrite, puis vue en échec, avant la vraie règle du taux réduit :
  elle retirait la mention au retrait de la première ligne à 10 %, alors qu'une autre restait.
- Le catalogue des textes grandit avec chaque mention : c'est voulu, puisqu'il est la future table de
  traduction.
