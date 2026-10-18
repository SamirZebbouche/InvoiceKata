# ADR-0006 : La persistance derrière un port secondaire, des fichiers pour le kata

- **Statut** : proposé, ratifié par le merge de la PR qui l'introduit
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Le système est une **API**. Il stocke des clients, des profils d'émetteurs, des devis (modifiables en
brouillon, figés une fois finalisés), des factures (immuables une fois émises, numérotées en continu sans
trou, conservées 10 ans) et des PDF. Le délai du kata est court. À terme, PostgreSQL suffit.

## Décision

1. **La persistance est cachée derrière un port secondaire** (dépôts et unité de travail), défini à partir
   des besoins du domaine. Le domaine ne connaît aucun adaptateur.
2. **Pour le kata, le stockage se fait en fichiers JSON** (System.Text.Json). PostgreSQL viendra à terme.
   L'état persistant en JSON et le document résolu en YAML, destiné au rendu
   ([ADR-0005](ADR-0005-rendu-pdf-par-typst.md)), sont deux contrats distincts.
3. **L'adaptateur fichiers respecte quatre règles :**
   - **Le numéro suivant se déduit des factures stockées** (le plus grand + 1), sans compteur séparé : un
     numéro n'existe que si la facture existe, donc aucun trou n'est possible.
   - **L'écriture est atomique** : un fichier temporaire, puis un renommage. Un crash ne laisse pas de
     fichier à moitié écrit.
   - **Un document figé n'est jamais écrasé** : il est créé en `FileMode.CreateNew`, et l'adaptateur n'a
     aucun chemin de mise à jour pour lui.
   - **Un seul rédacteur à la fois** : l'API est un seul processus, et un verrou interne suffit.
4. **Une seule suite de tests de contrat du port**, rejouée sur chaque adaptateur : mémoire, fichiers,
   puis PostgreSQL. Un adaptateur qui passe la suite est interchangeable.
5. **L'adaptateur en mémoire** sert aux tests d'acceptation ([ADR-0004](ADR-0004-atdd-et-bdd-par-cas-d-usage.md)).
6. **Les PDF sont des fichiers**, identifiés par le document et sa version. On peut toujours les
   régénérer à partir du document figé ([ADR-0003](ADR-0003-reactions-par-evenements-idempotentes.md)).

## Options écartées

| Option | Raison |
|---|---|
| PostgreSQL dès le kata | Il faut monter de l'infrastructure ; des fichiers suffisent tant que l'API est le seul rédacteur. |
| SQLite | Une étape intermédiaire qui ne remplace ni les fichiers pour le kata, ni PostgreSQL à terme. |
| Un compteur de numéros séparé, ou une `SEQUENCE` en base | Le numéro et la facture peuvent diverger, ce qui laisse un trou. En PostgreSQL, ce sera un compteur verrouillé dans la même transaction que l'émission. |
| L'event sourcing | Il colle à l'immuabilité, mais c'est une porte à sens unique coûteuse que rien ne justifie encore. |

## Conséquences

- Passer à PostgreSQL revient à écrire un adaptateur qui passe la suite de contrat.
- Les fichiers ne supportent qu'un seul processus rédacteur. Plusieurs instances de l'API imposeront
  PostgreSQL.
