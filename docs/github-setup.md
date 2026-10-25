# Mise en place du dépôt GitHub

Étapes à faire une seule fois, par la personne qui crée le dépôt en ligne. Elles ne peuvent pas se faire en local.

## 1. Créer le dépôt

1. GitHub → **New repository**.
2. Nom : `beauty-booking` (ou autre). Visibilité : **Private** tant que le cahier des charges n'est pas validé.
3. Ne cochez ni README, ni .gitignore, ni licence : ils existent déjà en local.

## 2. Pousser le code local

```bash
cd beauty-booking
git remote add origin https://github.com/<utilisateur>/beauty-booking.git
git push -u origin main
```

## 3. Ajouter la binôme

Settings → **Collaborators** → **Add people** → son pseudo GitHub, avec le rôle *Write*.

## 4. Protéger `main`

Settings → **Branches** → **Add branch ruleset** (ou *Add classic branch protection rule*) sur la branche `main` :

- [x] Require a pull request before merging
  - [x] Require approvals : **1**
  - [x] Dismiss stale pull request approvals when new commits are pushed
- [x] Require status checks to pass before merging (à activer dès que le workflow CI existe)
  - [x] Require branches to be up to date before merging
- [x] Block force pushes
- [x] Restrict deletions

Remarque : sur un dépôt privé avec un compte gratuit, certaines règles de protection ne sont pas disponibles. Dans ce cas, passer le dépôt en public ou s'en tenir à la discipline décrite dans `CONTRIBUTING.md`.

## 5. Régler la fusion

Settings → **General** → *Pull Requests* :

- [x] Allow squash merging (seule option cochée)
- [x] Automatically delete head branches

## 6. Créer le tableau Kanban

1. Onglet **Projects** → **New project** → modèle **Board**.
2. Colonnes : `Backlog`, `À faire (semaine en cours)`, `En cours`, `En revue`, `Terminé`.
3. Ajouter chaque user story comme *issue* et l'associer au projet.
4. Créer des *milestones* `S1` à `S10` pour suivre le planning.

## 7. Vérification

- [ ] Un push direct sur `main` est refusé.
- [ ] Une PR affiche le template.
- [ ] Les deux membres voient le dépôt et le projet.
