# ADR-0005 : Le rendu PDF par Typst, à partir d'un document résolu

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Le spike de rendu ([ADR-0001](ADR-0001-spike-jetable-puis-lots-livrables.md)), sur la branche
[`spike/pdf-rendering`](https://github.com/SamirZebbouche/InvoiceKata/tree/spike/pdf-rendering/spike),
devait répondre à une question : peut-on, depuis .NET, rendre en PDF un devis ou une facture déjà résolus,
avec un template sans logique légale ? La réponse est oui.

Samir a ensuite repris les PDF avec l'agent : un devis, une facture, un devis long de 3 pages et un devis
court d'une page. Il a aussi demandé de vérifier Factur-X, c'est-à-dire un PDF/A-3 qui embarque le même
document en XML CII. Le juge était un validateur indépendant, Mustang 2.26.0. Cet ADR intègre ce débrief.

## Décision

1. **Une entrée et une sortie.** Le rendu reçoit un **document résolu** en YAML et produit un PDF. Les
   calculs (lignes, totaux, TVA par taux) et les mentions légales sont déjà faits par le domaine
   ([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)).
2. **Le moteur est Typst**, déjà éprouvé par Samir sur cvGenerator. Il est appelé en sous-processus
   (`typst compile`, avec `--root` sur un dossier de travail temporaire), et sa version est épinglée.
3. **Un template par type d'entreprise émettrice.** Un même template sert pour le devis et pour la
   facture : du point de vue du rendu, une facture est un devis dont le titre, le numéro et les références
   sont différents.
4. **Les montants sont transmis exacts**, et l'arrondi d'affichage relève du rendu. Le spike les passe
   sous forme de chaînes, lues avec le type `decimal` de Typst : en nombres YAML, ils seraient lus comme
   des flottants.
5. **Chaque page porte le numéro du document et une pagination n/N.** C'est une obligation pour la
   facture : « Le numéro de la facture […] doit figurer sur toutes les pages » et « elles doivent être
   numérotées selon une séquence n/N » ([service-public, F23208](https://entreprendre.service-public.gouv.fr/vosdroits/F23208),
   vérifiée le 7 août 2026). Pour le devis, rien ne l'exige : c'est l'agent qui propose de lui appliquer
   la même règle.
6. **Les totaux, les mentions légales et le cadre « Bon pour accord » forment un bloc insécable**, et
   restent donc toujours sur la même page. C'est une exigence de Samir. Aucun texte ne l'impose, mais une
   signature placée à côté du prix et des conditions qu'elle accepte prouve mieux l'accord du client.
7. **Un template ne pose de conditions que sur la présence d'une donnée facultative.** Il ne contient
   aucune règle sur le profil légal.
8. **Factur-X : Typst, puis un post-traitement, avec Mustang en repli.** Le spike l'a établi :
   - Typst produit un PDF/A-3b conforme, mais il ne sait pas écrire les métadonnées XMP Factur-X ;
   - le PDF/A-3 de Typst, combiné au XML par Mustang, donne un Factur-X valide (PDF et XML valides,
     0 erreur, profil BASIC).

   Mustang, une CLI Java sous licence Apache-2.0, est la solution de repli. Un équivalent .NET sera
   cherché en temps voulu, ou pas : Factur-X n'est pas dans la roadmap du kata.

## Options écartées

| Option | Raison |
|---|---|
| QuestPDF | La mise en page s'écrit en C#, sans fichier de template, ce qui contredit le choix « YAML + template ». |
| Un rendu HTML vers PDF par Chromium | Il faut embarquer un navigateur, et le rendu des polices est moins déterministe. |
| Des montants en nombres YAML | Typst les lirait comme des flottants. |
| Un template par combinaison émetteur × client | La variation selon le client passe par les mentions, qui sont déjà résolues : le template n'a pas à la connaître. |
| Laisser les totaux, les mentions et la signature se répartir sur plusieurs pages | Une signature isolée du prix qu'elle accepte affaiblit la preuve de l'accord. |

## Questions ouvertes

Ce qui suit a été tranché dans le spike par l'agent seul, et reste à décider par Samir :

- **Le contrat YAML entre le domaine et le rendu** : sa structure et ses noms de champs ; des mentions en
  liste de phrases ou en champs structurés ; une identité légale composée par le template (« SAS au
  capital de… ») ou fournie déjà résolue. Il est à fixer avant le lot 2, quand le domaine produira ce
  document. Factur-X apporte un élément de réponse : des phrases à plat ne suffisent pas. Les règles
  françaises 2026 exigent des données structurées (échéance, date de livraison, IBAN, adresses
  électroniques des parties, mentions codées PMT, PMD et AAB).
- **Les totaux seuls en haut d'une page** : c'est le revers de la décision 6. Faut-il le laisser ainsi,
  faire suivre les dernières lignes du tableau avec le bloc, ou ajouter des reports (« Total à reporter »
  en bas de page, « Report » en haut de la suivante) ?
- **Les sections** : obligatoires ou facultatives ? Sur un devis de trois lignes, un titre de section
  alourdit la page.
- **Les quantités** : elles s'affichent à trois décimales (« 1,000 u »). Faut-il retirer les zéros
  inutiles, ou laisser chaque ligne déclarer sa précision ?

## Conséquences

- Un rendu prend environ 230 ms à chaud. Il n'a pas sa place dans le noyau synchrone : il est déclenché
  en réaction à `QuoteFinalized` et `InvoiceIssued` ([ADR-0003](ADR-0003-reactions-par-evenements-idempotentes.md)).
- Si Factur-X entre un jour dans le produit, la voie est connue (décision 8). Son émission devient
  obligatoire pour les PME le 1er septembre 2027. Le profil (BASIC dans le spike) et le format des
  adresses électroniques (SIREN, schéma `0225`, accepté par Mustang) seront à confirmer.
- Ajouter un type d'entreprise, par exemple un EI en franchise de TVA sans colonnes de TVA, revient à
  ajouter un template.
