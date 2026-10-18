# Spike : rendu d'un document résolu en PDF

Branche jetable, **jamais mergée** ([ADR-0001](../docs/adr/ADR-0001-spike-jetable-puis-lots-livrables.md)).

**Question.** Peut-on, depuis .NET, rendre en PDF un devis ou une facture déjà résolus (YAML), avec un
template Typst sans logique légale ?

**Réponse : oui.**

## Contenu

| Chemin | Rôle |
|---|---|
| `data/generate.py` | Joue le rôle du domaine : calcule exactement les montants et écrit les YAML résolus |
| `data/*.yaml` | Un devis, une facture issue de ce devis, un devis long qui teste la pagination, et un devis court qui tient sur une page |
| `templates/societe.typ` | Le template d'un émetteur de type société, commun au devis et à la facture |
| `render/` | Une console .NET qui lance `typst compile` et mesure le temps de rendu |
| `out/*.pdf` | Les PDF produits |

```
python -I spike/data/generate.py
dotnet run --project spike/render -- templates/societe.typ data/devis.yaml out/devis.pdf
```

## Constats

1. **L'appel depuis .NET fonctionne** par un sous-processus `typst compile`, avec `--root` et
   `--input data=/…`. Un rendu prend environ 230 ms à chaud. La toute première compilation a pris 3,4 s,
   à cause du cache des polices. Typst 0.15.1 est le seul prérequis : la police par défaut est embarquée
   dans le binaire.
2. **Un même template sert pour le devis et pour la facture.** Le titre, le numéro, la référence
   « D'après le devis… » et le cadre « Bon pour accord » sont des données.
3. **Les montants arrivent exacts, en chaînes**, et Typst les lit avec le type `decimal`. En nombres
   YAML, ils seraient lus comme des flottants. Le template ne fait qu'un arrondi d'affichage, plus le
   format français (espace fine pour les milliers, virgule, espace insécable avant €). Cela tient en une
   vingtaine de lignes.
4. **Afficher le prix unitaire brut et la remise**, plutôt qu'un prix net arrondi, évite l'incohérence
   visible sur le devis réel étudié (18,720 × 30,00 affiché, mais 561,54 calculé). C'est un choix de
   présentation, propre à chaque template.
5. **La pagination est gérée.** La ligne d'en-tête du tableau se répète sur chaque page. L'en-tête de
   page, l'identité légale en pied de page et la mention « <numéro> — page n / N » apparaissent sur chaque
   page. C'est une **obligation** pour la facture : le numéro sur toutes les pages et une pagination n/N
   ([service-public, F23208](https://entreprendre.service-public.gouv.fr/vosdroits/F23208)).
6. **Les totaux, les mentions et le cadre « Bon pour accord » forment un seul bloc insécable**, à la
   demande de Samir : ils sont toujours sur la même page. Aucun texte ne l'impose, mais une signature placée
   à côté du prix et des conditions prouve mieux l'accord du client. Revers de cette règle : si le bloc ne
   tient pas sous les lignes, il part seul en haut de la page suivante.
7. **Le template ne pose de conditions que sur la présence d'une donnée facultative** (référence, SIREN du
   client, cadre d'acceptation). Il ne contient aucune règle sur le profil légal.

## Pièges rencontrés

- **Git Bash convertit `/data/devis.yaml` en chemin Windows** avant de le passer à Typst. Il faut
  `MSYS_NO_PATHCONV=1` en ligne de commande. Le problème ne se pose pas depuis .NET avec `ArgumentList`.
- **Typst n'autorise pas les fichiers hors de `--root`** : les données à rendre doivent se trouver sous la
  racine, ou dans un dossier temporaire qui sert de racine, comme dans cvGenerator.

## Questions ouvertes

- **Les totaux seuls en haut d'une page.** Faut-il les laisser ainsi, faire suivre les dernières lignes du
  tableau avec eux, ou ajouter des reports (« Total à reporter » en bas de page, « Report » en haut de la
  suivante) ?
- **Les sections.** Sont-elles obligatoires ou facultatives ? Sur le devis court, un titre de section
  pour trois lignes alourdit la page.
- **Les quantités.** Elles s'affichent à trois décimales (« 1,000 u »). Faut-il retirer les zéros
  inutiles, ou laisser chaque ligne déclarer sa précision ?
- **Le contrat YAML entre le domaine et le rendu** : sa structure, les mentions en liste de phrases ou en
  champs, l'identité légale composée par le template ou fournie déjà résolue. Factur-X apporte un
  élément de réponse : des phrases à plat ne suffisent pas. Le XML exige des données structurées
  (échéance, date de livraison, IBAN, mentions codées).

## Factur-X

Question posée par Samir au débrief : Typst sait-il produire du Factur-X, c'est-à-dire un PDF/A-3 qui
embarque le même document en XML CII ? Le juge est **Mustang 2.26.0** (CLI Java, Apache-2.0), qui valide
à la fois le PDF/A, par veraPDF, et le XML Factur-X, avec les règles françaises 2026.

| Essai | Verdict |
|---|---|
| `data/factur-x.xml`, produit par `generate.py` (profil BASIC) | ✅ Valide, sans erreur ni avertissement |
| `out/facture-a3.pdf` : la facture en `--pdf-standard a-3b` | ✅ PDF/A-3b conforme |
| `out/facture-x.pdf` : Typst seul, avec `pdf.attach("factur-x.xml", …)` | ❌ La pièce jointe est présente, mais il manque les métadonnées XMP Factur-X (`DocumentType`, `DocumentFileName`, `Version`, `ConformanceLevel`), que Typst ne sait pas écrire |
| `out/facture-x-combine.pdf` : `facture-a3.pdf` post-traité par `combine` de Mustang | ✅ Factur-X valide : PDF valide, XML valide, 0 erreur |

**Réponse : Typst produit le PDF/A-3, et un post-traitement ajoute le XML et les métadonnées XMP.**
Mustang en est la solution de repli prouvée. Un équivalent .NET sera cherché en temps voulu.

Le validateur a aussi appris plusieurs choses :
- certaines mentions françaises sont des **notes codées** (PMT, PMD, AAB) ;
- les **adresses électroniques** de l'émetteur et du client sont obligatoires (BT-34, BT-49) ;
- un virement exige un **IBAN** ;
- `Decimal.normalize()` écrivait 50 sous la forme `5E+1` dans le YAML et le XML. Le bug est corrigé.

```
typst compile templates/societe.typ out/facture-a3.pdf --root . --pdf-standard a-3b --input data=/data/facture.yaml
java -jar Mustang-CLI-2.26.0.jar --action combine --source out/facture-a3.pdf --source-xml data/factur-x.xml \
  --out out/facture-x-combine.pdf --format fx --version 2 --profile B --no-additional-attachments
java -jar Mustang-CLI-2.26.0.jar --action validate --source out/facture-x-combine.pdf
```

## Hors du spike

Un template pour un autre type d'entreprise, par exemple un EI en franchise de TVA, sans colonnes de TVA.
L'appel de Mustang depuis la console .NET n'a pas été fait : la combinaison a été lancée à la main.
