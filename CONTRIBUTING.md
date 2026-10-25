# Guide de contribution

Ce guide fixe les règles de travail du binôme. Il reste volontairement simple : deux personnes, dix semaines.

## Stratégie de branches (GitHub Flow)

- `main` contient toujours une version qui fonctionne. **Personne ne pousse directement dessus.**
- Tout travail se fait sur une branche courte créée depuis `main`, puis fusionnée par Pull Request.
- Une branche vit quelques jours au maximum. Une fonctionnalité trop grosse se découpe en plusieurs branches.

### Nommage des branches

`type/description-courte`, en minuscules avec des tirets.

| Préfixe | Usage | Exemple |
|---|---|---|
| `feat/` | nouvelle fonctionnalité | `feat/calcul-disponibilites` |
| `fix/` | correction de bug | `fix/double-reservation` |
| `infra/` | Docker, Kubernetes, Helm | `infra/manifests-postgres` |
| `monitoring/` | Prometheus, Grafana, alertes | `monitoring/alerte-taux-erreurs` |
| `ci/` | pipeline GitHub Actions | `ci/build-et-tests` |
| `docs/` | documentation | `docs/readme-demarrage` |

## Messages de commit

Format : `type: description à l'impératif`, en français ou en anglais, mais de façon cohérente.

```
feat: ajouter le calcul des créneaux disponibles
fix: empêcher deux réservations sur le même créneau
docs: compléter le README
```

Types utilisés : `feat`, `fix`, `docs`, `infra`, `monitoring`, `ci`, `test`, `refactor`, `chore`.

## Pull Requests

1. Pousser la branche et ouvrir une Pull Request vers `main` en remplissant le template.
2. **L'autre membre du binôme relit.** Une PR n'est jamais approuvée par son auteure.
3. Les vérifications automatiques (CI) doivent être vertes.
4. Fusion en **squash merge** pour garder un historique lisible sur `main`.
5. Supprimer la branche après la fusion.

## Règles de protection de `main`

À activer dans GitHub (Settings → Branches → Add rule, ou Rulesets). Le détail pas à pas est dans [docs/github-setup.md](docs/github-setup.md).

- Exiger une Pull Request avant fusion
- Exiger 1 approbation
- Exiger que les vérifications de statut passent (une fois la CI créée)
- Exiger que la branche soit à jour avant fusion
- Bloquer les pushs forcés et la suppression de `main`

## Secrets

Aucun mot de passe, jeton ou clé dans le dépôt. Les secrets vont dans GitHub Secrets (CI/CD) et dans les Secrets Kubernetes (cluster). Un fichier `.env` n'est jamais commité ; on fournit un `.env.example` à la place.

## Répartition des tâches

Le suivi se fait dans le projet GitHub (Kanban) et dans le tracker de l'équipe. Chaque membre travaille à la fois sur l'application et sur l'infrastructure, et les rôles tournent à mi-parcours.
