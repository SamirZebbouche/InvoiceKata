# ADR-0005 : Le rendu PDF par Typst, à partir d'un document résolu

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Le spike de rendu ([ADR-0001](ADR-0001-spike-jetable-puis-lots-livrables.md)), sur la branche
`spike/pdf-rendering`, devait répondre à une question : peut-on, depuis .NET, rendre en PDF un devis ou
une facture déjà résolus, avec un template sans logique légale ? La réponse est oui ; les constats sont
détaillés dans `spike/README.md`, sur cette branche.

## Décision

1. **Une entrée et une sortie.** Le rendu reçoit un **document résolu** en YAML et produit un PDF. Les
   calculs (lignes, totaux, TVA par taux) et les mentions légales sont déjà faits par le domaine
   ([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)).
2. **Le moteur est Typst**, déjà éprouvé par Samir sur cvGenerator. Il est appelé en sous-processus
   (`typst compile`, avec `--root` sur un dossier de travail temporaire), et sa version est épinglée.
3. **Un template par type d'entreprise émettrice.** Un même template sert pour le devis et pour la
   facture : du point de vue du rendu, une facture est un devis dont le titre, le numéro et les références
   sont différents.
4. **Les montants sont transmis exacts, sous forme de chaînes**, et le template les lit avec le type
   `decimal` de Typst. L'arrondi d'affichage et le format français relèvent du rendu.
5. **Un template ne pose de conditions que sur la présence d'une donnée facultative.** Il ne contient
   aucune règle sur le profil légal.

## Options écartées

| Option | Raison |
|---|---|
| QuestPDF | La mise en page s'écrit en C#, sans fichier de template, ce qui contredit le choix « YAML + template ». |
| Un rendu HTML vers PDF par Chromium | Il faut embarquer un navigateur, et le rendu des polices est moins déterministe. |
| Des montants en nombres YAML | Typst les lirait comme des flottants. |
| Un template par combinaison émetteur × client | La variation selon le client passe par les mentions, qui sont déjà résolues : le template n'a pas à la connaître. |

## Conséquences

- Un rendu prend environ 230 ms à chaud. Il n'a pas sa place dans le noyau synchrone : il est déclenché
  en réaction à `QuoteFinalized` et `InvoiceIssued` ([ADR-0003](ADR-0003-reactions-par-evenements-idempotentes.md)).
- La facture électronique (Factur-X : un PDF/A-3 qui embarque un XML) est hors du périmètre. Son
  émission devient obligatoire pour les PME le 1er septembre 2027 ; il faudra alors vérifier ce que Typst
  sait produire.
- Ajouter un type d'entreprise, par exemple un EI en franchise de TVA sans colonnes de TVA, revient à
  ajouter un template.
