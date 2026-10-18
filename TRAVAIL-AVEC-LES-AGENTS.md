# Travailler avec les agents

L'agent (Claude Code, modèle Opus 5.5) tient cette page à la demande de Samir et la met à jour avant
chaque pull request.

## Ce qui est délégué

- La documentation : le [README](README.md), cette page, et la rédaction des ADR, que Samir ratifie.
- La recherche légale (mentions obligatoires des devis et des factures, facturation électronique), puis
  sa confrontation avec le scénario de Samir.
- L'analyse de modèles réels de factures et d'un devis fournis par Samir. Elle a relevé des erreurs dans
  ces modèles : une TVA totale à 0 €, l'indemnité de 40 € réclamée à un particulier, une échéance
  antérieure à la date de la facture.
- Le spike de rendu, de bout en bout : les données, le template Typst, la console .NET et les constats.
  Samir avait fixé le cadre : Typst, un YAML résolu en entrée, un PDF en sortie, un template par type
  d'entreprise.
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
  ([ADR-0004](docs/adr/ADR-0004-atdd-et-bdd-par-cas-d-usage.md)). Pour le contrat entre le domaine et
  le rendu, Samir choisit un validateur final, composé des validateurs de chaque partie, qui refuse tout
  document invalide avant le rendu ([ADR-0005](docs/adr/ADR-0005-rendu-pdf-par-typst.md)). La
  persistance est cachée derrière un port secondaire : des fichiers pour le kata, PostgreSQL à terme.
  L'agent a d'abord écarté les fichiers, faute de transactions. Samir les a proposés, et l'agent a montré
  à quelles conditions ils tiennent : un numéro déduit des factures stockées, des écritures atomiques et
  une suite de contrat commune à tous les adaptateurs
  ([ADR-0006](docs/adr/ADR-0006-persistance-par-port-secondaire.md)). Samir a ensuite fait chorégraphier
  les mentions légales, parce que certaines ne se déclenchent qu'à l'ajout de certaines lignes.
  L'argument de l'agent contre cette idée (« plus personne ne garantit la complétude ») ne tenait plus
  depuis le validateur final. Samir a fixé deux règles : des gestionnaires fondés sur l'état, donc
  idempotents, et un validateur qui ne s'exécute qu'une fois toutes les réactions terminées (amendement
  de l'[ADR-0003](docs/adr/ADR-0003-reactions-par-evenements-idempotentes.md)). Pour l'API, Samir a
  retenu `quotes` et des routes préfixées par l'émetteur, et il a imposé deux points : l'émetteur est configurable, parce que l'éditeur de l'application vend un SaaS
  à plusieurs entreprises, et son identifiant doit être impossible à deviner. L'agent avait supposé une
  instance par émetteur ([ADR-0007](docs/adr/ADR-0007-saas-multi-emetteur.md)).
- **La roadmap.** L'agent a proposé un découpage en lots, et Samir l'a validé. Samir a ensuite validé la
  structure de la solution (projets et sens des dépendances), et l'agent a rédigé le détail PR par PR
  dans [docs/ROADMAP.md](docs/ROADMAP.md), avec les questions à trancher en chemin. L'agent a signalé un
  risque : à J-2, toujours aucune ligne de code, alors que l'énoncé demande « quelques heures ». La suite
  doit donc surtout être de l'exécution.
- **Le rôle de PO.** Dans la réalité, les règles légales viendraient d'un PO ou d'un juriste. Ici, Samir
  tient ce rôle : l'agent fournit une recherche sourcée sur les sites officiels, Samir décide.

## Comment c'est vérifié

- **Le code** : en TDD, chaque test est vu en échec avant d'écrire le code. `dotnet test` et
  `dotnet format --verify-no-changes` sont relancés après la dernière modification. Il n'y a pas encore
  de code.
- **La documentation et les décisions** : Samir les relit, pull request par pull request.
- **Le spike de rendu** : l'agent lit chaque PDF produit et en recalcule les totaux, plutôt que de se
  fier au code de retour.
- **Factur-X** : un validateur indépendant, Mustang, juge le résultat, pas l'agent. Mustang a trouvé un
  bug de l'agent (`50` écrit `5E+1`) et des règles françaises que la recherche n'avait pas relevées
  (mentions codées, adresses électroniques obligatoires).

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
- **« La partie back » mal comprise.** Pour Samir, c'était une API depuis le début. L'agent l'a lu comme
  « le domaine seul », a proposé « aucun point d'entrée » et a même écrit dans l'ADR-0004 qu'il n'y avait
  « pas d'API dans le périmètre ». L'ADR-0004 est amendé, et le README le précise désormais.
- **Une question posée en jargon.** L'agent a demandé s'il fallait « tenter Factur-X » sans expliquer ce
  que c'était. Samir pensait y avoir répondu, et l'agent a cru comprendre que le sujet était écarté. Une
  fois Factur-X expliqué, Samir a demandé de le vérifier dans le spike.
- **Un chemin déformé dans le spike.** Git Bash a converti le chemin passé à Typst en chemin Windows, et
  la première compilation a échoué. L'agent a diagnostiqué la cause, puis l'a contournée.
- **Un spike conclu trop vite.** Samir avait annoncé « POC, discutons ». L'agent a pourtant enchaîné le
  spike, ses conclusions, un ADR, une PR et même la proposition du lot suivant, en tranchant seul le
  contrat YAML entre le domaine et le rendu. Samir l'a arrêté : « tu es parti trop vite du spike ». La
  leçon retenue : un spike se termine par un débrief, où l'agent montre les résultats et liste les choix
  que le spike lui a fait faire. L'ADR vient ensuite. Le débrief a produit deux décisions de Samir : les
  totaux, les mentions et la signature toujours sur la même page, et un devis court pour tester l'autre
  extrême.
