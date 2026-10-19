# Travailler avec les agents

L'agent principal est Claude Code (modèle Opus 5.5). Samir tient trois rôles : il fixe le cadre, il
tranche les décisions, et il joue le PO pour les règles légales. Le cadre vient de sa base de pratiques
personnelle, installée dans le dépôt : les règles du [CLAUDE.md](CLAUDE.md) (délibérer avant de proposer,
TDD, lots relisibles, vérifier avant d'affirmer, limites d'action sur git, langues) et des skills à la
demande dans [.claude/skills/](.claude/skills/).

Cette page est la synthèse. Le journal tenu au fil du travail est
[TRAVAIL-AVEC-LES-AGENTS-BRUT.md](TRAVAIL-AVEC-LES-AGENTS-BRUT.md), et la revue critique finale
[REVUE-CRITIQUE.md](REVUE-CRITIQUE.md).

## Ce qui a été délégué

- **Le code**, lot par lot, en ATDD et en TDD : le domaine, la persistance, le rendu PDF, l'API, la CI, et
  un front de démonstration.
- **La rédaction** des ADR et des CDR, que Samir ratifie, et de la documentation.
- **La recherche légale** sur les sites officiels (mentions obligatoires, franchise de TVA, client
  particulier), datée et citée dans les ADR.
- **Le spike de rendu**, dans un cadre fixé par Samir : Typst, un document résolu en entrée, un PDF en
  sortie.
- **Les gestes git** jusqu'à l'ouverture de la pull request : commit, push, description. Le merge reste à
  Samir.
- **La revue critique finale**, par plusieurs agents indépendants.

## Ce qui a été gardé

- **Le cadre et l'autonomie.** Samir a choisi les règles d'emblée, et les a ajustées aux frictions :
  une règle de conception installée après un défaut de relecture, la mutation sortie de la boucle rapide
  quand elle l'a ralentie.
- **Chaque décision structurelle.** L'agent propose des options et réfute celles qui ne tiennent pas ;
  Samir tranche. Plusieurs décisions ont changé en délibération :

| Sujet | Proposition de l'agent | Décision de Samir |
|---|---|---|
| Le produit | une instance par émetteur | un SaaS multi-émetteur, l'émetteur dans la route ([ADR-0007](docs/adr/ADR-0007-saas-multi-emetteur.md)) |
| La persistance | une base, faute de transactions avec des fichiers | des fichiers derrière un port, aux conditions que l'agent a posées ([ADR-0006](docs/adr/ADR-0006-persistance-par-port-secondaire.md)) |
| L'état persistant | une méthode `ToSnapshot` | un contrat d'interface, des versions et des migrations à la lecture ([ADR-0009](docs/adr/ADR-0009-documents-legaux-jamais-reecrits.md)) |
| La numérotation | le port seul bloque un doublon | « ceinture et bretelles » : le domaine aussi ([CDR-0007](docs/cdr/CDR-0007-numerotation-des-devis.md)) |
| L'API | des minimal APIs | des contrôleurs ([CDR-0005](docs/cdr/CDR-0005-controleurs-plutot-que-minimal-apis.md)) |
| Le POC | le merger | le jeter ([ADR-0001](docs/adr/ADR-0001-spike-jetable-puis-lots-livrables.md)) |

  L'inverse s'est produit aussi. L'agent a convaincu Samir, sources à l'appui, d'arrondir les montants
  légaux dans le domaine plutôt qu'au rendu ([ADR-0008](docs/adr/ADR-0008-arrondis.md)), et de préférer le
  Template Method à un builder pour les mentions ([CDR-0001](docs/cdr/CDR-0001-mentions-en-template-method.md)).
- **Le périmètre et la roadmap** : l'agent a proposé le découpage en lots, Samir l'a validé, et chaque
  lot laisse un système fonctionnel ([docs/ROADMAP.md](docs/ROADMAP.md)).
- **Les règles légales** : l'agent apporte une recherche sourcée, Samir décide. Par exemple, la mention
  de franchise cite le CIBS plutôt que l'ancien article du CGI, encore toléré.

## Comment c'est vérifié

- **Chaque test est vu en échec avant le code.** Un test écrit après coup n'est accepté qu'une fois vu en
  échec sous une casse ciblée du code qu'il protège.
- **Des oracles machine, après chaque changement** : `dotnet test` (523 tests) et
  `dotnet format --verify-no-changes`, relancés par la CI sur chaque pull request.
- **La mutation (Stryker.NET), en CI** : elle mute les fichiers modifiés et signale les tests qui ne
  tiennent rien. Elle a trouvé de vrais trous à presque chaque lot ; le score final est de 96,7 %.
- **La couverture des branches**, en fin de lot : chaque branche non prise des fichiers modifiés est
  testée ou expliquée.
- **Ce qu'aucun code de retour ne montre est regardé** : les PDF sont relus par PdfPig dans les tests, et
  regardés ; le front de démo a été piloté dans un vrai navigateur, captures à l'appui. Dans le spike,
  un validateur indépendant (Mustang) a jugé le Factur-X, et trouvé un bug de l'agent.
- **Samir relit chaque pull request** avant de la fusionner.

## Ce qui n'a pas marché du premier coup

- **L'agent a décidé à la place de Samir.** Il a proposé un cadrage alors que le cadre devait venir de
  Samir, conclu le spike et écrit son ADR sans débrief, et choisi les minimal APIs sans les proposer.
  Samir l'a arrêté chaque fois. La leçon : un spike se termine par un débrief, et une décision
  structurelle est proposée avant d'être construite.
- **Une règle absente du contexte ne protège pas.** Un README a été écrit sur `main`, parce que la règle
  git était dans un skill pas encore chargé. Des types primitifs laissaient un SIREN prendre la place d'un
  n° de TVA, parce que la règle existait dans la base de Samir mais pas dans le projet.
- **Une interprétation non questionnée.** « La partie back » a été lue comme « le domaine seul », au point
  d'écrire « pas d'API dans le périmètre » dans un ADR, amendé ensuite. Une question posée en jargon
  (« tenter Factur-X ? ») a été mal comprise des deux côtés.
- **Des tests insuffisants, rattrapés par les oracles.** Des scénarios d'acceptation tenaient lieu de
  tests unitaires ; les tests unitaires, exigés par Samir, ont aussitôt trouvé une mention légale fausse
  (un capital de 1 000,50 € écrit « 1 001 € »). Des tests ont été écrits après le code, puis vus en échec
  par des casses. La mutation a trouvé des trous à presque chaque lot. Un invariant de totaux supposait
  le régime normal et rendait illisible la facture d'un entrepreneur en franchise : un scénario l'a vu.
- **Des outils mal placés.** La mutation, mise dans la boucle rapide, a pris jusqu'à 25 minutes ; elle
  tourne désormais en CI seulement.
- **Une conséquence vue trop tard.** Au dernier lot, la liste blanche a fini par couvrir toutes les
  paires connues : le refus d'un contexte non pris en charge n'a plus d'exemple réel. L'agent l'a vu après
  avoir construit, pas en délibérant. C'est le cas qui motive le
  [plan d'un agent contradicteur](docs/PLAN-CONTRADICTEUR.md).
- **Le périmètre a débordé.** Le temps de Samir est resté limité : il cadre, tranche et relit, l'agent
  produit. Mais à ce débit, chaque lot semblait possible, et toute la roadmap a été exécutée, quand
  l'énoncé valorise un résultat partiel assumé. Rien n'a posé la question « on continue ou on coupe ? ».
  Cette page elle-même, recadrée tôt (« elle doit être synthétique »), avait regrossi : d'où ce découpage
  entre synthèse et journal.
- **Ce que la revue finale a trouvé** : les saisies ne sont pas contrôlées, deux mentions obligatoires
  manquent sur la facture, et quelques documents décrivent un mécanisme que le code n'a pas (voir
  [REVUE-CRITIQUE.md](REVUE-CRITIQUE.md)).
