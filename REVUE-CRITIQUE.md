# Revue critique d'InvoiceKata

Une revue critique mais pragmatique du logiciel livré, et des dérives du travail avec les agents. Elle a été
demandée par Samir à la fin du projet, le 19 octobre 2026, pour être lue telle quelle.

**Comment elle a été produite.** Plusieurs agents indépendants ont relu chacun une dimension du code :
domaine, couche application, persistance, API, rendu PDF, simplicité. Un autre a catalogué les dérives à
partir du journal, des descriptions de pull request et de l'historique git. La vérification
contradictoire automatique n'a pas abouti : l'agent principal a donc revérifié lui-même chaque constat
retenu ici, sur une instance lancée ou dans le code. Les tests, la conformité légale, la documentation, la
sécurité et l'outillage n'ont pas eu de relecture dédiée : cette revue n'en fait pas le tour.

## Lancer le programme en local

**Prérequis**

- Le SDK .NET 10 (version `10.0.401` ou une version ultérieure de la même branche, fixée par `global.json`).
- [Typst](https://github.com/typst/typst/releases/tag/v0.15.1) 0.15.1, accessible sous le nom `typst`
  (réglage `Rendering:Typst`). Sans lui, tout fonctionne sauf les PDF.

**L'API et sa démo**

```
dotnet run --project src/InvoiceKata.Api
```

- http://localhost:5045/ ouvre un front de démonstration, qui déroule le parcours : émetteur, client,
  devis, lignes, finalisation, PDF du devis, facture, PDF de la facture. Le type d'émetteur (société ou
  entrepreneur individuel) et le type de client (professionnel ou particulier) se choisissent dans les
  formulaires. Le bouton « Recommencer » remet le parcours à zéro.
- Les routes sont décrites dans [docs/API.md](docs/API.md).
- Les données sont des fichiers JSON, un dossier par émetteur, sous `src/InvoiceKata.Api/data` (réglage
  `Persistence:Directory`). Elles survivent au redémarrage.

**Les tests**

```
dotnet test
dotnet format --verify-no-changes
```

523 tests : domaine, application, contrat de persistance, API, rendu, et 39 scénarios d'acceptation en
français. Les tests de rendu et les scénarios marqués `@pdf` appellent Typst.

## Le verdict

Le dépôt montre un vrai savoir-faire : l'immuabilité des documents émis garantie par construction, des
arrondis justes et prouvés, une numérotation sûre, des décisions tracées et une discipline de test réelle.
Mais l'effort est allé à la rigueur interne plutôt qu'à ce qu'un utilisateur rencontre dans la première
minute. Et le volume produit par l'agent (135 commits, 12 ADR, 7 CDR, 523 tests) dépasse ce qu'un énoncé
qui valorise « un résultat partiel assumé » semblait attendre. Ce n'est pas une question de temps passé :
Samir a cadré, tranché et relu, l'agent a produit. C'est une question de proportion, et de volume à relire
pour qui évalue.

## Les points forts

- **Les arrondis** ([ADR-0008](docs/adr/ADR-0008-arrondis.md)) : la TVA est calculée une fois par taux sur
  la base HT du taux, arrondie au centime à mi-chemin vers le haut, et le prix net après remise reste en
  pleine précision. Des cas discriminants font échouer la version naïve (0,03 + 0,03 à 20 % donne 0,01 de
  TVA, et non 0,02).
- **Un document émis ne change jamais** : la finalisation fige le devis en une seule opération, et une
  facture n'a aucune méthode qui la modifie. La règle de l'[ADR-0009](docs/adr/ADR-0009-documents-legaux-jamais-reecrits.md)
  est vraie par construction.
- **La numérotation** : le domaine calcule le numéro suivant à partir des numéros déjà pris, et le port
  refuse un doublon sous son verrou ([CDR-0007](docs/cdr/CDR-0007-numerotation-des-devis.md)).
- **Des abstractions gagnées au bon moment** : la Strategy du régime de TVA n'apparaît qu'avec le second
  régime, et les objets valeur sont réservés aux identifiants qu'on pourrait inverser.
- **Des décisions argumentées** : chaque ADR présente ses options écartées, et plusieurs décisions ont
  changé en délibération (voir [TRAVAIL-AVEC-LES-AGENTS.md](TRAVAIL-AVEC-LES-AGENTS.md)).

## Les constats, par priorité

### 1. Les saisies ne sont pas contrôlées : une faute de frappe donne un document légal numéroté sans PDF

Reproduit sur une instance lancée :

| Saisie | Réponse |
|---|---|
| Un émetteur au SIREN « abc » et à la rue vide | `201` |
| Une ligne sans taux de TVA | `201`, à 0 % de TVA : l'option que l'[ADR-0011](docs/adr/ADR-0011-ei-en-franchise-de-tva.md) dit écarter |
| Une ligne à −1 × 100 € et 33 % de TVA | `201`, et un devis à TVA négative |
| La finalisation du devis, puis l'émission de la facture | `200`, puis `201` : les numéros sont attribués |
| Le PDF du devis | `500` « the issuer's SIREN 'abc' is not nine digits », à chaque lecture |

Aucune route ne corrige un émetteur : ces documents resteront sans PDF. La seule barrière est le
validateur final du rendu, conçu pour attraper un défaut du produit. Il attrape ici une erreur de saisie,
trop tard, et laisse passer ce qui est numériquement lisible (une quantité négative, un taux de 33 %).
C'est l'inverse de la règle que le projet se donne (« validate at the boundaries and fail fast »).

**Le remède**, une à deux heures : un SIREN de 9 chiffres, un n° de TVA de forme valide, des textes non
vides, une quantité positive, un prix positif ou nul, une remise de 0 à 100 %, un taux parmi 20, 10, 5,5,
2,1 et 0, et un champ numérique absent refusé en `400`.

### 2. Des documents de conception décrivent un autre mécanisme que le code

- L'[ADR-0006](docs/adr/ADR-0006-persistance-par-port-secondaire.md) dit qu'un document figé est créé en
  `FileMode.CreateNew`, sans chemin de mise à jour. Le code écrit un fichier temporaire puis le renomme,
  et refuse l'écrasement par une garde applicative sous un verrou. La garantie tient (la suite de contrat
  la vérifie), mais pas par le mécanisme décrit.
- L'[ADR-0003](docs/adr/ADR-0003-reactions-par-evenements-idempotentes.md) décrit un dispatcher qui traite
  les événements « jusqu'à épuisement ». Il fait un seul passage, ce que le CDR-0002 reconnaît, mais pas
  l'ADR.
- L'ADR-0009 dit les montants enregistrés tels qu'émis. Les totaux le sont ; les totaux de ligne sont
  recalculés à la lecture à partir des quantités et des prix.
- `POST …/lines` répond avec un `Location` vers une route qui n'existe pas.

### 3. Deux mentions obligatoires de la facture manquent

Le numéro RCS de l'émetteur, et la date de la vente ou de la prestation, réputée égale à la date de la
facture. Les deux avaient été notés « à reprendre » pendant le lot de la facture, et n'ont jamais été
repris. Le principe du projet est pourtant de ne jamais produire une facture fausse
([ADR-0002](docs/adr/ADR-0002-variations-selon-emetteur-et-client.md)).

### 4. De la sur-construction locale

Rien de faux, mais des motifs appliqués là où ils ne distinguent rien :

- les événements du brouillon ne portent aucune information que leurs gestionnaires lisent : le
  dispatcher habille un simple « réévaluer toutes les règles » ;
- le mécanisme de réactions sert une seule réaction par document ;
- quatre règles de mention sur sept s'appliquent toujours (`AppliesTo => true`) ;
- les deux templates PDF sont identiques à environ 85 % ;
- le document résolu encode en chaînes (`"company"`, `"franchise"`…) des types que le domaine a en enums,
  ce qui oblige les validateurs à vérifier des valeurs inconnues.

### 5. Typst absent n'est pas détecté au démarrage

Sans Typst, l'API démarre, la finalisation répond `200`, et le PDF répond un `500` dont la cause n'est
que dans les journaux.

## Les limites assumées

### Voulues

- **Une IHM minimale.** Le livrable est un back exposé par une API ; le front est une page de
  démonstration sans framework, hors périmètre, qui n'appelle que les routes de l'API.
- **Pas d'authentification ni de multi-utilisateur** : l'émetteur est dans la route, et son identifiant
  est un GUID impossible à deviner ([ADR-0007](docs/adr/ADR-0007-saas-multi-emetteur.md)). Ce n'est pas
  une protection : une vraie mise en service demande une authentification.
- **Des fichiers JSON plutôt qu'une base**, derrière un port prêt à recevoir une base
  ([ADR-0006](docs/adr/ADR-0006-persistance-par-port-secondaire.md)). Le verrou est en mémoire : une seule
  instance de l'API à la fois.
- **Hors roadmap** : les avoirs, les factures d'acompte, Factur-X (la voie est connue,
  [ADR-0005](docs/adr/ADR-0005-rendu-pdf-par-typst.md)) et PostgreSQL.
- **Deux contextes d'émetteur seulement** : une société au régime normal, un entrepreneur individuel en
  franchise. Ni entrepreneur individuel assujetti à la TVA, ni société en franchise.
- **Pour un particulier** ([ADR-0012](docs/adr/ADR-0012-client-particulier.md)) : ni médiateur de la
  consommation, ni mentions propres à un secteur, ni droit de rétractation, ni garantie légale de
  conformité.

### Échappées

- **Le contrôle des saisies** (constat 1).
- **Les deux mentions obligatoires manquantes** (constat 3).
- **Ni liste, ni modification, ni suppression** d'émetteur, de client ou de devis brouillon : un
  utilisateur qui perd un identifiant ou fait une faute de frappe est bloqué. Ce n'est écrit nulle part
  comme un choix.
- **Des documents en retard sur le code** (constat 2).

## Les dérives du travail avec les agents

Trente dérives ont été cataloguées. La plupart ont été rattrapées : par Samir en relecture, par les tests,
par la mutation en CI. Elles se rangent en familles :

| Famille | Exemples | Ce qui les aurait empêchées |
|---|---|---|
| L'agent décide à la place du propriétaire | un cadrage proposé d'office, un spike conclu sans débrief, les minimal APIs choisies sans être proposées, une finition de démo décidée seule | une porte explicite avant toute décision structurelle, que le cadre prévoit mais que l'agent a franchie |
| Une règle absente du contexte | un README écrit sur `main`, des types primitifs partout, une règle git périmée | les règles critiques dans le `CLAUDE.md`, chargé à chaque session, plutôt que dans des skills chargés à la demande |
| Une interprétation non questionnée | « la partie back » lue comme « le domaine seul », figé dans un ADR ; une question posée en jargon | une question de clarification avant de figer une hypothèse |
| Des tests insuffisants | des scénarios pris pour des tests unitaires ; des tests écrits après le code ; des trous trouvés par la mutation à chaque lot ; une échéance calculée de tête | les oracles machine, qui les ont attrapées : c'est leur rôle |
| Une conséquence non anticipée | l'invariant des totaux qui supposait le régime normal ; le refus d'un contexte non pris en charge devenu inatteignable une fois la liste blanche complète | un contradicteur indépendant au moment de la décision |
| La confidentialité | le nom de l'entreprise destinataire écrit dans le dépôt public : retiré des fichiers, il reste dans deux messages de commit, que la protection de `main` empêche de réécrire | une règle « dépôt public : aucun nom de tiers », et un contrôle avant chaque push |
| Le périmètre | la roadmap entière exécutée, quand l'énoncé valorise un résultat partiel assumé ; une page sur les agents recadrée puis regonflée ; une dette légale jamais reprise | une porte en fin de lot : « l'énoncé demande peu, on continue ou on coupe ? », et un registre de dette relu à chaque lot |

### Un agent contradicteur les aurait-il empêchées ?

Le cadre prévoyait déjà un « réfuteur » : l'agent devait attaquer ses propres propositions avant de les
présenter. Mais c'était le même agent, dans le même contexte que le proposeur, avec les mêmes angles
morts. Le cas le plus net est celui du monde fermé devenu inatteignable : la délibération présentée à
Samir ne l'avait pas vu, parce que personne n'a posé la question « que refuse encore le système après ce
choix ? ».

Un contradicteur séparé, au contexte neuf et chargé de réfuter, aurait attrapé environ un tiers des
dérives : les conséquences de conception, les affirmations des documents que le code ne tient pas, et
surtout le constat 1, qu'un regard extérieur demandant « que produit une faute de frappe de
l'utilisateur ? » aurait vu dès le premier lot. Il n'aurait rien changé aux dérives de processus et
d'outillage : une règle que personne ne charge, le périmètre, la confidentialité. Celles-là demandent
des règles et des portes, pas un agent de plus.

Le plan pour l'ajouter, et son mode d'emploi : [docs/PLAN-CONTRADICTEUR.md](docs/PLAN-CONTRADICTEUR.md).

## Ce qui serait corrigé ensuite, dans l'ordre

1. Le contrôle des saisies, avec des refus en `400`.
2. Le RCS et la date de la prestation sur la facture, ou une décision écrite qui les déclare hors
   périmètre.
3. Les documents de conception remis d'accord avec le code.
4. Une vérification de Typst au démarrage.
5. Les routes de liste et de correction, si le produit devait servir.
