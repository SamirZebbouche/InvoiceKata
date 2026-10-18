# ADR-0008 : Les arrondis, précision dans le domaine, centimes pour les montants légaux

- **Statut** : accepté le 2026-10-18
- **Date** : 2026-10-18
- **Décideur** : Samir

## Contexte

Le domaine calcule des lignes, des remises, de la TVA et des totaux. Il faut décider où arrondir. Samir
voulait que le domaine reste précis et que l'arrondi soit l'affaire du rendu. Il demandait aussi s'il
existait des règles comptables à respecter.

Les règles trouvées :
- **La TVA se calcule au centime, pour chaque facture.** Selon une réponse ministérielle de 2020, « les
  TVA collectées et déductibles sont calculées au centime près pour chaque facturation ». Seule la
  déclaration est ensuite arrondie à l'euro.
- **La norme européenne de la facture électronique (EN 16931)** impose que la TVA d'un taux soit égale à
  la base de ce taux multipliée par le taux, **arrondie à deux décimales** (règle BR-CO-17), et qu'un
  montant de TVA ait au plus deux décimales (BR-DEC-20). Un prix unitaire peut en avoir davantage.
- **L'article 242 nonies A de l'annexe II au CGI** demande, pour chaque taux, le total hors taxe et la taxe
  correspondante : la TVA se calcule par taux, pas ligne par ligne.

## Décision

1. **Les valeurs saisies restent en pleine précision.** Prix unitaire, quantité, taux de remise, prix net
   après remise (par exemple 29,997 €) : `Money` et `Quantity` les gardent sans arrondi.
2. **Le domaine arrondit au centime les montants légaux, et eux seuls** : le total HT de chaque ligne, la
   TVA de chaque taux, et donc les totaux HT, TVA et TTC, qui sont des sommes de montants déjà arrondis.
3. **La TVA est calculée une fois par taux**, sur le total HT de ce taux, jamais ligne par ligne.
4. **Le mode d'arrondi est l'arrondi commercial, à mi-chemin vers le haut** : 0,125 donne 0,13
   (`MidpointRounding.AwayFromZero`). Ce n'est pas l'arrondi au pair que .NET applique par défaut. Aucun
   texte officiel trouvé n'impose ce mode : c'est l'usage, et c'est un choix assumé.
5. **Le rendu ne fait que mettre en forme.** Il arrondit seulement l'affichage d'une valeur saisie (un
   prix unitaire de 29,997 € affiché 30,00 €), jamais un montant légal, qui lui arrive déjà au centime
   ([ADR-0005](ADR-0005-rendu-pdf-par-typst.md)).

## Options écartées

| Option | Raison |
|---|---|
| Tout garder précis dans le domaine et arrondir au rendu | Le document deviendrait faux à l'œil nu : HT arrondi + TVA arrondie ≠ TTC arrondi, à un centime près. Le PDF et le XML Factur-X arrondiraient chacun de leur côté et pourraient se contredire. Le XML violerait BR-CO-17. |
| Arrondir la TVA ligne par ligne | Contraire à BR-CO-17 et au calcul par taux. Par exemple, deux lignes de 0,03 € à 20 % donneraient 0,02 € de TVA au lieu de 0,01 €. |
| L'arrondi au pair (`Math.Round` par défaut) | Ce n'est pas l'usage commercial : 0,125 donnerait 0,12. |

## Conséquences

- Les tests du domaine fixent ces règles, et ils ont été vus en échec contre une version volontairement
  naïve (arrondi au pair, TVA ligne par ligne).
- Le contrat du document résolu transmet au rendu des montants légaux déjà au centime.

## Sources

- [Question écrite n° 26125 et réponse ministérielle](https://questions.assemblee-nationale.fr/q15/15-26125QE.htm)
- [EN 16931 / Peppol : règle BR-CO-17](https://docs.peppol.eu/tdd/sk/tdd-sk/trn-tdd/rule/BR-CO-17/)
- [EN 16931 / Peppol : BT-117, montant de TVA par catégorie](https://docs.peppol.eu/tdd/sk/tdd-sk/trn-tdd/semantic-model/bt-117/)
- [Service Public Entreprendre : mentions obligatoires d'une facture (F31808)](https://entreprendre.service-public.gouv.fr/vosdroits/F31808)
