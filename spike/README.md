# Spike : rendu d'un document résolu en PDF

Branche jetable, **jamais mergée** ([ADR-0001](../docs/adr/ADR-0001-spike-jetable-puis-lots-livrables.md)).

**Question.** Peut-on, depuis .NET, rendre en PDF un devis ou une facture déjà résolus (YAML), avec un
template Typst sans logique légale ?

**Réponse : oui.**

## Contenu

| Chemin | Rôle |
|---|---|
| `data/generate.py` | Joue le rôle du domaine : calcule exactement les montants et écrit les YAML résolus |
| `data/*.yaml` | Un devis, une facture issue du devis, et un devis long qui teste la pagination |
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
5. **La pagination est gérée** : la ligne d'en-tête du tableau se répète sur chaque page, les totaux
   restent groupés, et l'en-tête de page, l'identité légale en pied de page et « page x / y » apparaissent
   partout.
6. **Le template ne pose de conditions que sur la présence d'une donnée facultative** (référence, SIREN du
   client, cadre d'acceptation). Il ne contient aucune règle sur le profil légal.

## Pièges rencontrés

- **Git Bash convertit `/data/devis.yaml` en chemin Windows** avant de le passer à Typst. Il faut
  `MSYS_NO_PATHCONV=1` en ligne de commande. Le problème ne se pose pas depuis .NET avec `ArgumentList`.
- **Typst n'autorise pas les fichiers hors de `--root`** : les données à rendre doivent se trouver sous la
  racine, ou dans un dossier temporaire qui sert de racine, comme dans cvGenerator.

## Hors du spike

Factur-X (PDF/A-3 avec un XML embarqué), et un template par autre type d'entreprise, par exemple un EI en
franchise de TVA, sans colonnes de TVA.
