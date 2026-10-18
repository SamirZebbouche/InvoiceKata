# Travailler avec les agents

L'agent (Claude Code, modèle Opus 5.5) tient cette page à la demande de Samir et la met à jour avant
chaque pull request.

## Ce qui est délégué

- La documentation : le [README](README.md) et cette page.
- Les gestes git : commits, push de branche et ouverture des pull requests (niveau `autonomous-pr`). Le
  merge reste à Samir.

## Ce qui est gardé

- **Le cadre.** Samir installe sa base de pratiques personnelle, `ai-core`, dans le dépôt : six règles
  dans le [CLAUDE.md](CLAUDE.md) et quinze skills. Il choisit ces règles d'emblée, sans attendre les
  frictions : « je vois à peu près où je veux aller ».
- **Les décisions de conception.** L'agent propose des options et réfute celles qui ne tiennent pas ;
  Samir tranche.
- **Le périmètre**, encore à définir.

## Comment c'est vérifié

- **Le code** : en TDD, chaque test est vu en échec avant d'écrire le code. `dotnet test` et
  `dotnet format --verify-no-changes` sont relancés après la dernière modification. Il n'y a pas encore
  de code.
- **La documentation et les décisions** : Samir les relit, pull request par pull request.

## Ce qui n'a pas marché du premier coup

- **Un cadrage proposé par l'agent.** Le mail est transmis sans consigne, et l'agent propose un
  questionnaire de cadrage avec des options « recommandées ». Samir le refuse : le cadre devait venir de
  lui, pas de l'agent.
- **Un README écrit sur `main`, malgré le régime `pr-only`.** Ce régime était déclaré dans un skill qui ne
  se charge qu'à la demande, et l'agent ne l'avait pas encore lu. Le travail a été déplacé sur une branche
  avant le commit.
- **Une règle git périmée.** Elle disait qu'il n'y avait « pas de forge », alors que le dépôt GitHub
  existait. L'agent n'a rien poussé et a posé la question ; la règle a été corrigée.
- **Une première version trop détaillée de cette page.** Samir recadre : la page sert à l'évaluer, elle
  doit être synthétique.
