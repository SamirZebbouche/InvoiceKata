# ADR-0001 : Un spike de rendu jetable, puis des lots livrables

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

L'exercice dispose de quelques heures, avec une livraison le 20 octobre 2026. La démarche retenue impose
qu'**après chaque PR, le système fonctionne** : où que le travail s'arrête, la solution est opérationnelle.

Le principal inconnu technique est la chaîne de rendu : produire un PDF à partir d'un YAML et d'un
template, comme le fait cvGenerator avec Typst.

## Décision

1. **Un spike de rendu**, sur la branche `spike/pdf-rendering`. Il est poussé pour rester visible, mais
   n'est **jamais mergé** et n'est pas soumis à la règle TDD. Il répond à une seule question : peut-on
   rendre un devis en PDF à partir d'un YAML et d'un template ? Il teste **le moteur de rendu, et lui
   seul** : son YAML d'entrée est un devis déjà résolu, mentions légales comprises. La complexité des
   profils n'en fait pas partie. Ce qu'il produit, c'est un savoir, consigné dans un ADR sur le rendu, et
   non du code.
2. **Ensuite, une roadmap en lots**, sur le modèle d'un MVP enrichi pas à pas. Chaque lot est développé en
   TDD, et chaque PR laisse un système fonctionnel.

## Options écartées

| Option | Raison |
|---|---|
| Un POC mergé comme squelette de bout en bout, couvert par un seul test de bout en bout | Écartée par Samir. Elle aurait fait entrer sur `main` du code qui n'a pas été écrit en TDD. |
| Pas de POC du tout | Le risque sur le rendu n'aurait été découvert qu'en plein lot. |

## Conséquences

- Le chemin de rendu est écrit deux fois, une fois dans le spike puis une fois en TDD. Ce coût est assumé.
- Le [CLAUDE.md](../../CLAUDE.md) déclare la zone `spike/*` dans les notes de la règle TDD.
