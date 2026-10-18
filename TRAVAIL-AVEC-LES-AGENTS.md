# Travailler avec les agents

L'agent (Claude Code, modèle Opus 5.5) tient cette page à la demande de Samir et la met à jour avant
chaque pull request.

## Ce qui est délégué

- La documentation : le [README](README.md), cette page, et la rédaction des ADR, que Samir ratifie.
- La recherche légale (mentions obligatoires des devis et des factures, facturation électronique), puis
  sa confrontation avec le scénario de Samir.
- Les gestes git : commits, push de branche et ouverture des pull requests (niveau `autonomous-pr`). Le
  merge reste à Samir.

## Ce qui est gardé

- **Le cadre.** Samir installe sa base de pratiques personnelle, `ai-core`, dans le dépôt : six règles
  dans le [CLAUDE.md](CLAUDE.md) et quinze skills. Il choisit ces règles d'emblée, sans attendre les
  frictions : « je vois à peu près où je veux aller ».
- **Les décisions de conception.** L'agent propose des options et réfute celles qui ne tiennent pas ;
  Samir tranche.
- **La démarche.** Samir décrit d'abord un cadre large, que l'on conçoit ensemble par ADR. On construit
  ensuite une roadmap découpée en lots, avec un POC d'abord, puis un MVP enrichi petit à petit. La
  contrainte est que chaque PR laisse un système fonctionnel : où que le travail s'arrête, la solution
  est opérationnelle.
- **Le périmètre et l'architecture.** Le périmètre est le back d'un logiciel de devis et de factures. Le
  produit est modulaire : chaque cas pris en charge dépend de l'émetteur et du client. Ce qui n'est pas
  pris en charge est refusé, parce que « le pire serait de fournir une facture fausse »
  ([ADR-0002](docs/adr/ADR-0002-variations-selon-emetteur-et-client.md)). Le POC est un spike jetable
  qui ne teste que le moteur de rendu ([ADR-0001](docs/adr/ADR-0001-spike-jetable-puis-lots-livrables.md)).
  Samir a proposé le motif Strategy ; l'agent l'a affiné en réservant la Strategy au régime de TVA, qui
  est exclusif, et en traitant les mentions légales, qui s'additionnent, comme une liste de règles. Samir
  a écarté la recommandation de l'agent, qui voulait merger le POC. Samir a aussi demandé une chorégraphie
  par événements pour certaines parties, chaque événement étant traité de façon idempotente. L'agent a
  délimité ces parties : le noyau légal reste synchrone, et seules les réactions à un fait acquis sont
  chorégraphiées ([ADR-0003](docs/adr/ADR-0003-reactions-par-evenements-idempotentes.md)). Chaque
  cas d'usage est livré en ATDD couplé au BDD, choix de Samir, avec des scénarios en français
  ([ADR-0004](docs/adr/ADR-0004-atdd-et-bdd-par-cas-d-usage.md)).
- **La roadmap.** L'agent a proposé un découpage en lots, et Samir l'a validé. L'agent a signalé un
  risque : à J-2, toujours aucune ligne de code, alors que l'énoncé demande « quelques heures ». La suite
  doit donc surtout être de l'exécution.
- **Le rôle de PO.** Dans la réalité, les règles légales viendraient d'un PO ou d'un juriste. Ici, Samir
  tient ce rôle : l'agent fournit une recherche sourcée sur les sites officiels, Samir décide.

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
