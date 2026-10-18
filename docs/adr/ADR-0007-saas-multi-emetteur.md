# ADR-0007 : Un SaaS multi-émetteur, l'émetteur dans la route

- **Statut** : accepté le 2026-10-18
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

L'éditeur de l'application propose ses solutions à des entreprises clientes : l'application est un SaaS, et une même instance
sert plusieurs **émetteurs**, c'est-à-dire plusieurs entreprises qui facturent. Chaque émetteur a sa
propre configuration (identité légale, type d'entreprise, régime de TVA, séries de numérotation), ses
clients, ses devis et ses factures.

## Décision

1. **L'émetteur est une ressource configurable** : `POST /issuers`.
2. **Chaque requête désigne son émetteur dans la route** : `/issuers/{issuerId}/customers`,
   `/issuers/{issuerId}/quotes`… Un client, un devis ou une facture appartient à un seul émetteur.
3. **Les identifiants ne se devinent pas.** Ce sont des GUID v4 aléatoires (`Guid.NewGuid()`), pas des
   GUID v7, dont l'horodatage révèle la date de création et réduit la part aléatoire. La règle vaut pour
   toutes les ressources.
4. **Une ressource d'un autre émetteur renvoie `404`**, jamais `403`, pour ne pas révéler qu'elle existe.
5. **Les données et la numérotation sont isolées par émetteur.** Chaque émetteur a sa propre séquence de
   numéros sans trou ([ADR-0006](ADR-0006-persistance-par-port-secondaire.md)).

## Options écartées

| Option | Raison |
|---|---|
| L'émetteur dans un en-tête (`X-Issuer-Id`) | C'est implicite : un en-tête oublié devient une erreur de configuration, et les ressources ne disent plus à qui elles appartiennent. |
| Une instance par émetteur, configurée par fichier | Contraire au SaaS multi-émetteur. |
| Des identifiants séquentiels, ou des GUID v7 | Ils sont énumérables ou en partie prévisibles. |

## Conséquences

- Un identifiant imprévisible empêche l'énumération, mais ce n'est pas une autorisation. Le jour où
  l'authentification arrive, elle vérifie qu'un utilisateur a le droit d'agir sur l'`issuerId` de la
  route.
- La liste blanche des contextes ([ADR-0002](ADR-0002-variations-selon-emetteur-et-client.md)) s'appuie
  sur la configuration de l'émetteur.
