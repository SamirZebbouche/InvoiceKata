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
- Le code, lot par lot : le squelette de la solution, la CI, puis chaque cas d'usage en ATDD et en TDD.
  Samir valide la structure et les routes avant, et relit tous les deux commits.
- Les gestes git : commits, push de branche et ouverture des pull requests (niveau `autonomous-pr`). Le
  merge reste à Samir.

## Ce qui est gardé

- **Le cadre.** Samir installe sa base de pratiques personnelle, `ai-core`, dans le dépôt : six règles
  dans le [CLAUDE.md](CLAUDE.md) et quinze skills. Il choisit ces règles d'emblée, sans attendre les
  frictions : « je vois à peu près où je veux aller ».
- **Un cadre qui évolue dans les deux sens.** Le cadre n'est pas figé au départ :
  - **de `ai-core` vers le projet**, quand une friction montre un manque. Après l'obsession des types
    primitifs relevée en relecture, Samir a installé la règle `arch-no-uniform-patterns`, qui existait
    dans sa base mais pas dans le projet ;
  - **du projet vers `ai-core`**, quand le projet invente une pratique qui vaut ailleurs. Les tests par
    mutation, nés ici des casses ciblées de l'agent puis outillés avec Stryker.NET, ont été récoltés par
    Samir dans sa base, sous l'identifiant `test-mutation-testing`, pour servir à ses autres projets.

  Le projet complète aussi ses propres notes : le ruleset de `main`, l'exception de langue des
  scénarios Gherkin, la zone des spikes, et maintenant Stryker parmi les preuves.
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
  instance par émetteur ([ADR-0007](docs/adr/ADR-0007-saas-multi-emetteur.md)). Samir a ensuite
  relevé qu'un même client réel, s'il l'est de deux entreprises, doit avoir deux fiches indépendantes ; le
  code le faisait déjà, l'ADR-0007 en fait une décision explicite. Pour la
  persistance, l'agent proposait une méthode `ToSnapshot` ; Samir a exigé un contrat (une interface qui
  garantit la présence des méthodes dans les classes à persister), puis a soulevé deux risques : un
  objet alourdi, et des objets reconstruits dans un état faux quand le domaine évolue. Il en est sorti des
  fichiers partiels, des invariants revérifiés à la reconstruction, un numéro de version et des fichiers
  de référence ([CDR-0006](docs/cdr/CDR-0006-etat-persistant-des-agregats.md)). Samir a choisi des
  migrations de version en version, à la lecture, et a demandé un ADR pour leur cause légale : un document
  émis n'est jamais réécrit ([ADR-0009](docs/adr/ADR-0009-documents-legaux-jamais-reecrits.md)). Sur les arrondis, Samir
  pensait que tout devait se faire au rendu. L'agent a montré, sources à l'appui, que les montants légaux
  doivent être arrondis au centime dans le domaine, sinon le PDF et le XML se contrediraient. Samir a
  demandé que cette règle soit écrite dans un ADR ([ADR-0008](docs/adr/ADR-0008-arrondis.md)).
  Samir a ensuite introduit un second type de document, le **Code Design Report (CDR)**, pour les
  décisions de conception du code (motifs, modélisation). Samir avait relevé que le modèle de mention
  proposé par l'agent imposait de modifier une structure commune à chaque nouvelle mention, et il
  demandait si un builder aiderait. L'agent a écarté le builder, qui règle la construction et non
  l'extension, au profit du Template Method : une mention, une classe ([docs/cdr](docs/cdr/README.md)).
  En relecture, Samir a aussi écarté les minimal APIs de l'agent (« on appelle des `Map` dans tous les
  sens ») au profit de contrôleurs ([CDR-0005](docs/cdr/CDR-0005-controleurs-plutot-que-minimal-apis.md)).
  Pour la numérotation des devis, l'agent proposait que seul le port bloque un numéro en double. Samir a
  exigé « ceinture et bretelles » : le domaine doit aussi empêcher la collision. Il a demandé si un
  *interlock* conviendrait. L'agent l'a placé dans l'adaptateur, où un verrou existait déjà, et a laissé
  au domaine un calcul pur à partir des numéros déjà pris. Samir a demandé que ce partage soit consigné
  dans un CDR ([CDR-0007](docs/cdr/CDR-0007-numerotation-des-devis.md)).
- **La roadmap.** L'agent a proposé un découpage en lots, et Samir l'a validé. Samir a ensuite validé la
  structure de la solution (projets et sens des dépendances), et l'agent a rédigé le détail PR par PR
  dans [docs/ROADMAP.md](docs/ROADMAP.md), avec les questions à trancher en chemin. L'agent a signalé un
  risque : à J-2, toujours aucune ligne de code, alors que l'énoncé demande « quelques heures ». La suite
  doit donc surtout être de l'exécution.
- **Le rôle de PO.** Dans la réalité, les règles légales viendraient d'un PO ou d'un juriste. Ici, Samir
  tient ce rôle : l'agent fournit une recherche sourcée sur les sites officiels, Samir décide.

## Comment c'est vérifié

- **Le code** : en ATDD et en TDD, chaque test est vu en échec avant d'écrire le code. `dotnet test` et
  `dotnet format --verify-no-changes` sont relancés après la dernière modification, puis par la CI sur
  chaque PR. Quand un simple échec de compilation ne prouverait rien, l'agent casse exprès le code pour
  voir le test échouer pour la bonne raison :
  - un identifiant volontairement généré en GUID v7 a fait échouer le test qui exige un v4 ;
  - le filtre par émetteur, retiré un instant de l'adaptateur, a fait échouer le scénario d'isolation ;
  - un test de `404` passait déjà parce que la route n'existait pas : il exige désormais une réponse
    Problem Details.
  - une version volontairement naïve du calcul (arrondi au pair, TVA arrondie ligne par ligne) a fait
    échouer les deux tests des règles de calcul : 0,12 au lieu de 0,13, et 0,02 au lieu de 0,01.
  - une casse doit être ciblée : retirer `[ApiController]` faisait échouer une dizaine de tests à la
    fois, ce qui ne prouvait rien du test visé. L'agent a recommencé avec une casse précise (un seul champ
    rendu facultatif), qui ne fait échouer que ce test.
  - l'exhaustivité se teste : Samir a relevé que rien ne garantissait la traduction d'une nouvelle
    exception en code HTTP. Un test parcourt désormais toutes les exceptions du produit. L'agent l'a
    vérifié en ajoutant un instant une exception oubliée, qui l'a fait échouer.
  - les tests par mutation automatisent ces casses : Samir a proposé Stryker.NET. Le premier passage a
    trouvé trois vrais trous (des libellés de forme juridique non figés, un cas d'échec non couvert, un
    constructeur de devis vidable sans qu'aucun test du domaine ne le voie). Stryker fait désormais partie
    des preuves, au même titre que `dotnet format`, avec un seuil bloquant de 70 %.
  - la règle du scout, demandée par Samir : les mutants survivants des fichiers touchés sont traités au
    passage. Deux vrais trous sont apparus : l'enregistrement de la mention de taux réduit, et le
    répertoire de données, qu'on pouvait ignorer en silence. Les survivants du démarrage de l'API sont de
    faux positifs, car un hôte de test partagé ne rejoue pas le démarrage sous un mutant : l'agent les a
    tués à la main, puis les a marqués avec leur justification. Une assertion ajoutée sur un message
    d'erreur a aussi révélé une faute (« a Individual »). Le score est passé de 82,6 % à 92 %.
  - l'argument d'un CDR se démontre : avant d'écrire la règle du taux réduit, l'agent a écrit la version
    fondée sur le delta que le CDR-0001 écarte, et le scénario l'a fait échouer au retrait de la première
    ligne à 10 %.
  - un fichier de référence prouve sa valeur : en renommant un champ de l'état d'une ligne, tous les
    tests d'aller-retour passaient encore, et seul le fichier de référence v1 a échoué.
  - un test peut passer pour une mauvaise raison : le test du bloc insécable (totaux, mentions et « Bon pour
    accord » sur une même page) passait avec un bloc sécable, parce qu'avec 60 lignes le bloc tenait de toute
    façon sur la dernière page. Il parcourt désormais plusieurs longueurs de devis, et deux d'entre elles
    cassent le bloc dès qu'il devient sécable. Les PDF des tests sont relus par PdfPig, et l'agent les a aussi
    regardés lui-même.
  - une casse doit aussi compiler : deux casses rendaient du code inaccessible, ce que
    `TreatWarningsAsErrors` refuse. Aucun test n'a tourné, et l'absence de sortie n'était pas une preuve.
    L'agent les a refaites, en vérifiant le build avant de lire les tests.
- **La documentation et les décisions** : Samir les relit, pull request par pull request.
- **Les rapports publics** : à chaque passage sur `main`, la CI publie sur GitHub Pages le rapport des
  scénarios Gherkin et la couverture. Dans chaque PR, le résumé du job donne la synthèse des tests et de la
  couverture. Samir a proposé GitHub Pages, puisque le dépôt est public.
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
- **Des scénarios pris pour des tests unitaires.** Les règles de mention et le dispatcher n'étaient testés
  que par les scénarios Gherkin, et aucun projet ne testait la couche Application. Samir l'a relevé : « la
  présence d'un test d'acceptation ne dispense pas d'un TU ». Le projet `InvoiceKata.Application.Tests` les
  teste désormais, et chaque test a été vu en échec par une casse ciblée. Ces tests ont aussitôt trouvé un
  défaut : la mention légale arrondissait le capital social (1 000,50 € y devenait « 1 001 € »).
- **Un chemin déformé dans le spike.** Git Bash a converti le chemin passé à Typst en chemin Windows, et
  la première compilation a échoué. L'agent a diagnostiqué la cause, puis l'a contournée.
- **Un spike conclu trop vite.** Samir avait annoncé « POC, discutons ». L'agent a pourtant enchaîné le
  spike, ses conclusions, un ADR, une PR et même la proposition du lot suivant, en tranchant seul le
  contrat YAML entre le domaine et le rendu. Samir l'a arrêté : « tu es parti trop vite du spike ». La
  leçon retenue : un spike se termine par un débrief, où l'agent montre les résultats et liste les choix
  que le spike lui a fait faire. L'ADR vient ensuite. Le débrief a produit deux décisions de Samir : les
  totaux, les mentions et la signature toujours sur la même page, et un devis court pour tester l'autre
  extrême.
- **Une obsession des types primitifs.** L'agent avait écrit `Address(string, string, string)` et des
  commandes qui alignaient quatre `string` (dénomination, forme juridique, SIREN, n° de TVA) : un SIREN
  pouvait prendre en silence la place du n° de TVA. Samir l'a relevé en relecture. La règle existait dans
  `ai-core` (`arch-no-uniform-patterns` : un objet valeur seulement si des primitives peuvent être
  inversées ou s'il porte du comportement), mais elle n'était pas installée dans le projet, et l'agent
  ne l'avait donc pas sous les yeux. Samir l'a installée, et l'agent a appliqué la règle dans les deux
  sens : `Siren` et `VatNumber` deviennent des types, `Address` reçoit des propriétés nommées à chaque
  appel, sans un type par champ, et les DTO de l'API restent primitifs. Une règle qu'on n'installe pas
  ne protège pas.
- **Un fichier généré commité.** Le code produit par Reqnroll (`*.feature.cs`) est entré dans un commit.
  L'agent l'a vu en relisant le commit, l'a ajouté au `.gitignore` et a corrigé le commit, qui n'était
  pas encore poussé.
- **Un adaptateur en mémoire trop gentil.** Il stockait des références : une modification non enregistrée
  y « persistait », mais aurait été perdue avec des fichiers. L'agent l'a trouvé en écrivant la suite de
  contrat. La règle « une modification non enregistrée n'est pas conservée » y est entrée, vue en échec
  sur la mémoire avant la correction.
- **Du code de production écrit pour les tests.** Stryker a montré une égalité écrite à la main pour
  comparer des états dans les tests, et des invariants de reconstruction non testés un par un. L'égalité
  a été supprimée, et chaque invariant a maintenant son cas : le score de mutation est remonté de 76,5 %
  à 82,6 %.
- **Un écart à l'ATDD.** Pour le PDF du devis, l'agent a écrit la réaction et ses tests unitaires avant les
  scénarios Gherkin, qui sont donc passés dès leur premier lancement. Il a rattrapé l'écart en faisant
  échouer chaque scénario par une casse ciblée : le cadre « Bon pour accord » retiré, un brouillon rendu,
  un PDF rendu à nouveau à chaque lecture.
- **Un écart à la règle TDD.** Pour le client particulier, l'agent a écrit le contrôleur avant ses tests
  d'API. Il l'a signalé, puis a rattrapé l'écart en faisant échouer chaque test par une casse ciblée.
- **Une DLL périmée.** Après avoir restauré l'adaptateur qu'il avait cassé exprès, l'agent a vu le test
  rester rouge. La copie de sauvegarde avait gardé une date antérieure à la dernière compilation, si bien
  que la compilation incrémentale n'avait pas repris le fichier. Une reconstruction complète a corrigé
  le problème. Depuis, les preuves sont lancées avec `--no-incremental`.
