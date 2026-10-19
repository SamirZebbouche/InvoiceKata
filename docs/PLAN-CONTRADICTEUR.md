# Plan : ajouter un agent contradicteur

Un plan, pas encore une mise en œuvre : les décisions de la fin de ce document sont à ratifier avant de
construire. Son mode d'emploi suit le plan.

## Pourquoi

Le [CLAUDE.md](../CLAUDE.md) demande déjà à l'agent de délibérer en deux temps : proposer, puis réfuter
ses propres propositions avant de les présenter. Mais le réfuteur est le même agent que le proposeur,
dans le même contexte : il partage ses hypothèses et ses angles morts. La
[revue critique](../REVUE-CRITIQUE.md) en montre trois effets :

- au dernier lot, la délibération n'a pas vu que la liste blanche, une fois complète, rendait
  inatteignable le refus d'un contexte non pris en charge ;
- pendant cinq lots, personne n'a demandé ce que produit une faute de frappe de l'utilisateur : les
  saisies ne sont pas contrôlées ;
- des documents de conception décrivent un mécanisme que le code n'a pas, et aucune relecture ne
  confrontait chaque phrase au code.

Un contradicteur est un agent séparé, au contexte neuf, sans intérêt à défendre la proposition, dont le
seul mandat est de la réfuter, preuves à l'appui.

## Ce qu'il fait, et ce qu'il ne fait pas

- **Il fait** : il cherche les objections à une décision ou à un lot terminé, et les étaye par des
  citations (`fichier:ligne`, phrase d'un ADR, sortie d'un test).
- **Il ne fait pas** : il ne modifie aucun fichier, ne propose pas de solution complète, ne tranche rien.
  Samir décide ; l'agent principal construit.
- **Il ne remplace pas** les oracles machine (tests, format, mutation), ni les portes de processus qu'il
  ne voit pas : une règle que personne ne charge, le temps passé, la confidentialité.

## Les portes

Deux portes obligatoires, et une à la demande. Le contradicteur ne tourne pas à chaque commit : il
doublerait le coût de chaque changement pour un gain nul sur les portes à double sens.

| Porte | Moment | Ce qu'il reçoit |
|---|---|---|
| **P1. Décision à sens unique** | avant que Samir ratifie un ADR, une frontière, un contrat public ou une migration | le brouillon d'ADR, les options présentées, l'énoncé, le `CLAUDE.md`, les ADR liés |
| **P2. Fin de lot** | avant d'ouvrir la pull request | le diff du lot, la sortie des tests et de la couverture, les documents touchés, l'énoncé |
| **P3. À la demande** | sur une affirmation de succès douteuse, ou quand Samir le demande | l'affirmation et ses preuves |

**Ce qu'il ne reçoit jamais** : le raisonnement de l'agent principal. Il juge le résultat, pas
l'argumentation qui y a conduit, pour ne pas en hériter les angles morts.

## Son mandat : les questions imposées

À chaque porte, le contradicteur répond au moins à ces questions :

1. **Qu'est-ce qui devient inatteignable, obsolète ou faux** après ce choix (un refus, un test, une
   phrase de document) ?
2. **Que produit une erreur de l'utilisateur** à chaque entrée touchée : une saisie vide, mal formée, hors
   bornes, absente ?
3. **Quelle hypothèse est supposée uniforme** (un seul régime, un seul type de client, une seule
   instance) ?
4. **Quelle phrase des documents touchés le code contredit-il ?**
5. **Qu'est-ce qui a été affirmé sans être vérifié**, et quelle preuve le montrerait ?
6. **Le choix est-il proportionné à l'énoncé** (« quelques heures », « un résultat partiel assumé ») ?

Par défaut, il réfute : une objection qu'il ne peut ni prouver ni écarter reste ouverte.

## Sa sortie

Une liste d'objections, chacune avec :

- une **gravité** : bloquante, à discuter, ou mineure ;
- la **preuve** : `fichier:ligne`, citation, ou sortie d'outil ;
- la **question** du mandat dont elle vient ;
- **ce qui la lèverait** : un test, une phrase d'ADR, un choix de périmètre.

Au plus dix objections, triées par gravité. L'agent principal y répond objection par objection (retenue,
corrigée, ou écartée avec sa raison), et la réponse rejoint l'arbre des branches tuées que le
`CLAUDE.md` présente déjà à Samir.

## Les garde-fous

- **Lecture seule** : outils `Read`, `Grep`, `Glob`, et `git` en lecture (`log`, `show`, `diff`,
  `blame`). Ni écriture, ni build, ni exécution de tests : il lit leurs sorties.
- **Un autre modèle que l'agent principal**, si possible, pour réduire les angles morts partagés.
- **Un budget** : une passe par porte, et une seconde seulement si la première trouve une objection
  bloquante.
- **Pas sur les portes à double sens** : un renommage ou une correction locale n'a pas de contradicteur.

## La mise en œuvre, en lots

1. **La définition de l'agent** : `.claude/agents/contradicteur.md`, avec ses outils en lecture seule, son
   modèle et son mandat (le projet de définition est en annexe).
2. **La règle** : le `CLAUDE.md` dit que le réfuteur de la délibération est le contradicteur, appelé aux
   portes P1 et P2, et que ses objections figurent dans la présentation à Samir.
3. **La trace** : la description de chaque pull request gagne une section « Objections du
   contradicteur », avec chaque objection et la suite qui lui a été donnée.
4. **La calibration**, avant de s'y fier : le rejouer sur trois cas connus, et vérifier qu'il trouve au
   moins deux des trois défauts sans noyer Samir sous les faux positifs :
   - le brouillon de l'[ADR-0012](adr/ADR-0012-client-particulier.md), avant son amendement : il doit voir
     que le refus d'un contexte devient inatteignable ;
   - le diff du premier lot : il doit voir que les saisies ne sont pas contrôlées ;
   - l'[ADR-0006](adr/ADR-0006-persistance-par-port-secondaire.md) face au code de la persistance : il doit
     voir que `FileMode.CreateNew` n'est pas le mécanisme réel.
5. **La mesure** : compter, à chaque lot, les objections retenues et écartées. Après trois lots, garder,
   ajuster ou retirer le contradicteur selon ce qu'il a réellement apporté.

## Les décisions à ratifier

| Décision | Options | Recommandation |
|---|---|---|
| Où vit l'agent | dans le projet, ou dans la base `ai-core` de Samir | le projet d'abord, pour la calibration ; `ai-core` ensuite s'il fait ses preuves |
| Quel modèle | le même que l'agent principal, ou un autre | un autre, pour des angles morts différents |
| Quelles portes | P1 et P2, ou aussi chaque commit | P1 et P2, P3 à la demande |
| Qui l'appelle | l'agent principal, ou Samir seulement | l'agent principal aux portes, Samir quand il le veut |

## Mode d'emploi

*Une fois l'agent installé.*

**L'agent principal l'appelle de lui-même** aux portes P1 et P2. Samir voit alors, avec la proposition
ou la pull request, la liste des objections et la réponse donnée à chacune.

**Samir peut l'appeler à tout moment**, en le nommant dans la conversation :

- « Passe l'ADR-0013 au contradicteur avant que je tranche. »
- « Passe le lot au contradicteur avant d'ouvrir la PR. »
- « Contradicteur : tu affirmes que les PDF sont conformes, réfute-le. »

**Lire ses objections** : commencer par les bloquantes. Pour chacune, trois issues :

- **retenue** : l'agent principal corrige, ou ajoute le test qui la lève ;
- **écartée** : la raison est écrite dans la réponse, et si c'est un choix de périmètre, dans l'ADR ;
- **ouverte** : la question remonte à Samir, qui tranche.

**Quand ne pas l'appeler** : sur une correction locale ou un renommage, et pour une question de goût. Il
coûte du temps d'agent et du temps de lecture : sa place est aux décisions qui coûtent cher à défaire.

**Quand le contredire** : une objection n'est pas une vérité. Le contradicteur juge sans contexte, et peut
ignorer une contrainte que Samir a posée. L'écarter avec une raison écrite est un usage normal ; l'écarter
sans raison ne l'est pas.

## Annexe : projet de définition

Le fichier `.claude/agents/contradicteur.md`, à ratifier :

```markdown
---
name: contradicteur
description: Réfute une décision à sens unique (ADR, frontière, contrat public, migration) ou un lot
  terminé avant la pull request. Lecture seule. À appeler aux portes de délibération et de fin de lot,
  ou à la demande sur une affirmation de succès.
tools: Read, Grep, Glob, Bash
model: <un autre modèle que l'agent principal>
---

Tu es le contradicteur de ce projet. Tu ne défends rien : ton seul mandat est de réfuter ce qu'on te
soumet. Tu ne modifies aucun fichier ; en Bash, tu n'utilises que git en lecture (log, show, diff, blame).

Pour la proposition ou le lot soumis, réponds au moins à ces questions :
1. Qu'est-ce qui devient inatteignable, obsolète ou faux après ce choix ?
2. Que produit une erreur de l'utilisateur à chaque entrée touchée ?
3. Quelle hypothèse est supposée uniforme ?
4. Quelle phrase des documents touchés le code contredit-il ?
5. Qu'est-ce qui a été affirmé sans être vérifié, et quelle preuve le montrerait ?
6. Le choix est-il proportionné à l'énoncé du projet (voir README.md) ?

Chaque objection porte une gravité (bloquante, à discuter, mineure), une preuve citée (fichier:ligne,
phrase d'un document, sortie d'un outil), la question dont elle vient, et ce qui la lèverait.
Au plus dix objections, triées par gravité. Une objection que tu ne peux ni prouver ni écarter reste
ouverte. N'invente rien : sans preuve, pas d'objection.
```
