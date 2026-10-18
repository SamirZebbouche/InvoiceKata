# Travailler avec les agents

L'agent (Claude Code, modèle Opus 5.5, dans VS Code) tient cette page lui-même, à la demande de Samir. Il
y consigne les faits au fil de l'eau : qui a décidé quoi, ce qui a été vérifié et comment, ce qui a raté.
Les quatre sections du haut répondent aux questions de l'énoncé. Le journal du bas les alimente, et il
sera condensé à la fin pour que l'ensemble tienne en une page.

## Ce qui est délégué

- La rédaction du [README](README.md) et de cette page.
- Les commits sur des branches dédiées, sans demander. Le niveau d'autonomie déclaré est `autonomous-pr`,
  et la relecture se fait sur la pull request.

## Ce qui est gardé

- **Le choix des règles.** Samir a installé sa base de pratiques personnelle, `ai-core`, et choisi
  lui-même les règles à suivre. Il les a choisies **avant d'avoir rencontré la moindre friction** :
  « j'ai assumé de ne pas attendre les frictions pour faire certains choix, je vois à peu près où je veux
  aller ».
- **Le périmètre et les décisions de conception.** Le [CLAUDE.md](CLAUDE.md) l'impose : l'agent propose
  des options et réfute celles qui ne tiennent pas, mais c'est l'humain qui tranche.
- **Les actes de validation.** Merge, approbation et tag restent à l'humain, même après un accord en
  conversation.

## Comment c'est vérifié

- **Le code** n'est accepté qu'avec ses preuves : un test vu en échec avant d'écrire le code, puis
  `dotnet test` et `dotnet format --verify-no-changes`, relancés après la dernière modification. Il n'y a
  pas encore de code.
- **La documentation** est relue par Samir.

## Ce qui n'a pas marché du premier coup

- **L'agent a voulu cadrer à la place de l'humain** (voir le journal, 1).
- **L'agent a écrit sur `main` malgré le régime `pr-only`** (voir le journal, 3).

## Journal

### 2026-10-18

1. **Le premier cadrage est refusé.** Samir transmet le mail de Fimeco, sans aucune consigne. L'agent
   inspecte le dossier, qui est vide, et les outils installés. Il propose ensuite un questionnaire de
   quatre questions (stack, périmètre, règles métier, méthode) dont chacune a une option marquée
   « Recommandé ». Samir le rejette : « non on ne va pas faire comme ça ».
   - *Cause* : l'agent voulait cadrer le travail avant que l'humain ait installé son propre cadre. Et
     recommander une option, c'est déjà orienter des choix que l'exercice évalue justement.
   - *Correction* : Samir annonce qu'il applique `ai-core`, et l'agent s'arrête et attend.
2. **`ai-core` est installé** (commit `1580ce4`, fait par Samir). Le dépôt reçoit un `CLAUDE.md` de six
   règles :
   - délibérer avant de proposer ;
   - travailler en TDD ;
   - livrer en lots relisibles ;
   - vérifier avant d'affirmer ;
   - respecter les limites d'action sur git ;
   - suivre les conventions de langue.

   Il reçoit aussi 15 skills à la demande. Les valeurs déclarées pour ce projet sont :
   - niveau d'autonomie : `autonomous-pr` ;
   - régime de branches : `pr-only` ;
   - langues : français pour la conversation, anglais pour le code et les messages techniques ;
   - preuves : `dotnet test` et `dotnet format --verify-no-changes`.
3. **Le README est écrit sur `main`.** Consigne : « crée juste un read me sur le kata ». L'agent
   l'écrit, signale trois choix faciles à défaire (rédaction en français, stack .NET déduite des preuves
   déclarées, adresse et noms des interlocuteurs omis) et laisse le périmètre « à définir ».
   - *Raté* : l'agent avait bien vu qu'il était sur `main`, mais il n'avait pas lu le régime de branches.
     Ce régime est déclaré dans le skill `git-branch-discipline`, qui ne se charge qu'à la demande, donc
     l'agent ne l'a vu qu'au moment de commiter.
   - *Correction* : le README est passé sur la branche `docs/kata-framing` avant le premier commit.
4. **La règle git et le dépôt se contredisent.** D'après le `CLAUDE.md`, il n'y a « pas encore de remote
   ni de forge, les commits restent locaux ». Or `origin` pointe vers un dépôt GitHub. Dans le doute,
   l'agent n'a rien poussé et a posé la question.
5. **Cette page est déléguée.** Samir confie à l'agent, en autonomie, la tenue de la page sur le travail
   avec les agents.
