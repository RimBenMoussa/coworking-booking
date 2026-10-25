# User stories et backlog

Ce fichier est la source de référence du backlog. Chaque story est aussi une *issue* GitHub, rattachée à un *milestone* (semaine) et suivie dans le tableau GitHub Projects.

- **Priorités** (MoSCoW) : Indispensable, Important, Souhaitable.
- **Besoins** : les références F1 à F10 renvoient au cahier des charges.
- **Acteurs** : membre, gestionnaire d'espace, administrateur, équipe d'exploitation.

| Semaine | Stories |
|---|---|
| S2 – API métier (1/2) | US-01, US-02, US-03, US-04, US-05 |
| S3 – API métier (2/2) | US-06, US-07, US-08, US-09 |
| S4 – Frontend React | US-10, US-11, US-12, US-13, US-14 |
| S5 – Conteneurisation et Kubernetes | US-15, US-16 |
| S6 – Monitoring (1/2) | US-17, US-18 |
| S7 – Monitoring (2/2) | US-19, US-20, US-21, US-22 |
| S8 – Alertes | US-23, US-24, US-25 |
| S9 – CI/CD | US-26, US-27 |
| S10 – Validation et rapport | US-28, US-29, US-30 |

Total : 30 stories.

## S2 – API métier (1/2)

### US-01 : Inscription d’un membre

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F1

En tant que **visiteur**, je veux créer un compte avec mon email et un mot de passe, afin de pouvoir réserver des espaces.

**Critères d'acceptation**

- [ ] L’email doit être unique et valide, sinon l’API renvoie une erreur 400 explicite.
- [ ] Le mot de passe est stocké haché, jamais en clair.
- [ ] Un nouveau compte reçoit le rôle « membre » par défaut.

### US-02 : Connexion sécurisée et gestion des rôles

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F1

En tant que **utilisateur**, je veux me connecter et recevoir un jeton JWT, afin de accéder aux fonctions correspondant à mon rôle.

**Critères d'acceptation**

- [ ] Une connexion valide renvoie un JWT avec une durée de validité.
- [ ] Les routes réservées au gestionnaire et à l’admin renvoient 403 pour un membre.
- [ ] Un jeton absent ou expiré renvoie 401.

### US-03 : Catalogue d’espaces et d’équipements

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F2

En tant que **administrateur**, je veux créer, modifier et désactiver des espaces (salle, bureau privé, poste flex) avec leur capacité, leur tarif horaire et leurs équipements, afin de proposer un catalogue à jour aux membres.

**Critères d'acceptation**

- [ ] Un espace a un type, une capacité, un tarif horaire et une liste d’équipements.
- [ ] Un espace désactivé n’apparaît plus dans les recherches mais garde son historique.
- [ ] Seul l’admin peut modifier le catalogue.

### US-04 : Horaires d’ouverture par espace

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F3

En tant que **gestionnaire d’espace**, je veux définir les horaires d’ouverture hebdomadaires de chaque espace, afin de éviter les réservations en dehors des heures d’ouverture.

**Critères d'acceptation**

- [ ] Les horaires se définissent par jour de la semaine (début et fin).
- [ ] Une heure de fin antérieure à l’heure de début est refusée.
- [ ] Une réservation hors horaires est refusée par l’API.

### US-05 : Fermetures exceptionnelles

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F3

En tant que **gestionnaire d’espace**, je veux déclarer une période de fermeture (maintenance, jour férié) pour un espace, afin de bloquer les réservations pendant cette période.

**Critères d'acceptation**

- [ ] Une fermeture a une date de début, une date de fin et un motif.
- [ ] Les créneaux couverts par une fermeture ne sont plus proposés.
- [ ] Créer une fermeture sur une période déjà réservée signale les réservations en conflit.

## S3 – API métier (2/2)

### US-06 : Recherche d’espaces libres

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F4

En tant que **membre**, je veux chercher les espaces libres selon la date, la durée, la capacité et les équipements, afin de trouver rapidement un espace adapté.

**Critères d'acceptation**

- [ ] Un espace est proposé seulement s’il est ouvert, non fermé, assez grand, bien équipé et sans réservation qui chevauche.
- [ ] Le résultat est trié par pertinence (capacité la plus proche, puis tarif).
- [ ] Le calcul est couvert par des tests unitaires (cas limites : créneaux adjacents, minuit, fermetures).

### US-07 : Réservation d’un espace

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F5

En tant que **membre**, je veux réserver un espace pour une plage horaire, afin de être sûr de disposer de l’espace.

**Critères d'acceptation**

- [ ] La réservation est créée au statut « confirmée » si le créneau est libre.
- [ ] Le prix est calculé à partir du tarif horaire et de la durée.
- [ ] Une réservation dans le passé ou hors horaires est refusée.

### US-08 : Aucune double réservation

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F6

En tant que **système**, je veux garantir qu’un espace ne peut pas être réservé deux fois sur le même créneau, afin de éviter tout conflit, même si deux membres cliquent en même temps.

**Critères d'acceptation**

- [ ] Une contrainte d’exclusion PostgreSQL interdit les plages qui se chevauchent pour un même espace.
- [ ] Un test de concurrence (deux requêtes simultanées) montre qu’une seule réussit.
- [ ] La seconde requête reçoit une erreur 409 claire.

### US-09 : Modification et annulation d’une réservation

- **Type** : backend · **Priorité** : Indispensable · **Besoin** : F5

En tant que **membre**, je veux modifier ou annuler ma réservation, afin de libérer l’espace si mes plans changent.

**Critères d'acceptation**

- [ ] L’annulation est refusée en dessous d’un délai minimum configurable (par exemple 2 heures avant le début).
- [ ] Une modification revérifie la disponibilité du nouveau créneau.
- [ ] Un membre ne peut modifier que ses propres réservations.

## S4 – Frontend React

### US-10 : Pages d’inscription et de connexion

- **Type** : frontend · **Priorité** : Indispensable · **Besoin** : F1

En tant que **visiteur**, je veux m’inscrire et me connecter depuis l’interface web, afin de accéder à l’application sans passer par l’API.

**Critères d'acceptation**

- [ ] Les erreurs de l’API (email déjà utilisé, mot de passe invalide) s’affichent clairement.
- [ ] Le jeton est conservé de façon sécurisée et la session expire proprement.
- [ ] L’interface est utilisable sur mobile.

### US-11 : Parcours de recherche et de réservation

- **Type** : frontend · **Priorité** : Indispensable · **Besoin** : F4, F5

En tant que **membre**, je veux chercher un espace, voir ses détails et le réserver en 4 étapes maximum, afin de réserver sans effort.

**Critères d'acceptation**

- [ ] Les filtres date, durée, capacité et équipements sont disponibles.
- [ ] Le récapitulatif (espace, créneau, prix) s’affiche avant confirmation.
- [ ] Un message de confirmation s’affiche, et un conflit (409) propose d’autres créneaux.

### US-12 : Page « Mes réservations »

- **Type** : frontend · **Priorité** : Indispensable · **Besoin** : F5

En tant que **membre**, je veux voir mes réservations à venir et passées et pouvoir les annuler, afin de garder le contrôle de mon planning.

**Critères d'acceptation**

- [ ] Les réservations à venir et l’historique sont séparés.
- [ ] Le bouton d’annulation est masqué ou désactivé quand le délai minimum est dépassé.
- [ ] La liste se met à jour après une annulation.

### US-13 : Planning du jour et check-in

- **Type** : frontend · **Priorité** : Important · **Besoin** : F8

En tant que **gestionnaire d’espace**, je veux voir le planning du jour et enregistrer l’arrivée des membres, afin de suivre l’occupation réelle et repérer les absences.

**Critères d'acceptation**

- [ ] Le planning du jour liste les réservations par espace et par heure.
- [ ] Un bouton « check-in » passe la réservation à « utilisée ».
- [ ] Les réservations non honorées passent à « non utilisée » après la fin du créneau.

### US-14 : Rappels automatiques avant réservation

- **Type** : backend · **Priorité** : Important · **Besoin** : F7

En tant que **membre**, je veux recevoir un rappel avant ma réservation, afin de ne pas l’oublier.

**Critères d'acceptation**

- [ ] Un job planifié envoie un rappel (email) à un délai configurable avant le début.
- [ ] Chaque rappel est enregistré avec son statut (envoyé ou échoué).
- [ ] Une réservation annulée ne déclenche pas de rappel.

## S5 – Conteneurisation et Kubernetes

### US-15 : Conteneurisation de l’application

- **Type** : infra · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux des images Docker pour le frontend et l’API, afin de déployer partout de la même façon.

**Critères d'acceptation**

- [ ] Les Dockerfiles utilisent un build multi-étapes et une image finale légère.
- [ ] Un `docker compose up` lance l’application complète en local avec PostgreSQL.
- [ ] Aucun secret n’est écrit dans les images.

### US-16 : Déploiement sur Kubernetes

- **Type** : infra · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux déployer l’application sur un cluster minikube ou kind, afin de disposer d’un environnement proche de la production.

**Critères d'acceptation**

- [ ] Deployments, Services, Ingress, ConfigMap et Secrets sont décrits dans `k8s/`.
- [ ] PostgreSQL tourne en StatefulSet avec un volume persistant (les données survivent à un redémarrage).
- [ ] L’application est accessible via l’Ingress avec des commandes documentées dans le README.

## S6 – Monitoring (1/2)

### US-17 : Exposer les métriques de l’API

- **Type** : monitoring · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux que l’API expose un endpoint /metrics avec des métriques techniques et métier, afin de suivre sa santé et l’activité réelle.

**Critères d'acceptation**

- [ ] Métriques techniques : requêtes, erreurs 5xx et latence en histogramme.
- [ ] Métriques métier : réservations créées, annulées et non utilisées, conflits évités, rappels envoyés et échoués.
- [ ] L’endpoint répond au format Prometheus.

### US-18 : Déployer Prometheus et les exporters

- **Type** : monitoring · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux Prometheus qui collecte l’API, PostgreSQL, les nœuds et les conteneurs, afin de avoir toutes les métriques au même endroit.

**Critères d'acceptation**

- [ ] kube-prometheus-stack est installé via Helm dans le namespace `monitoring`.
- [ ] Les cibles API, postgres_exporter, node_exporter et cAdvisor apparaissent « UP ».
- [ ] La configuration est versionnée dans `monitoring/`.

## S7 – Monitoring (2/2)

### US-19 : Liste d’attente sur un espace complet

- **Type** : backend · **Priorité** : Souhaitable · **Besoin** : F10

En tant que **membre**, je veux m’inscrire sur une liste d’attente quand un espace est complet, afin de être prévenu si un créneau se libère.

**Critères d'acceptation**

- [ ] Un membre peut s’inscrire sur la liste d’attente d’un créneau déjà réservé.
- [ ] Une annulation notifie le premier membre de la liste.
- [ ] La liste est vidée une fois le créneau passé.

### US-20 : Tableaux de bord Grafana

- **Type** : monitoring · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux des tableaux de bord pour la santé de l’app, l’infrastructure et l’activité métier, afin de voir l’état du système d’un coup d’œil.

**Critères d'acceptation**

- [ ] Dashboard « santé » : trafic, erreurs, latence p95, état des pods.
- [ ] Dashboard « infrastructure » : CPU, mémoire, base de données.
- [ ] Dashboard « métier » : réservations, annulations, taux d’occupation. Tous sont exportés en JSON et versionnés.

### US-21 : Logs centralisés avec Loki

- **Type** : monitoring · **Priorité** : Important · **Besoin** : —

En tant que **équipe d’exploitation**, je veux consulter les logs de l’API dans Grafana, afin de comprendre la cause d’une erreur détectée par une alerte.

**Critères d'acceptation**

- [ ] L’API écrit des logs structurés (Serilog).
- [ ] Promtail envoie les logs vers Loki et ils sont consultables dans Grafana.
- [ ] On peut filtrer les logs par niveau et par pod.

### US-22 : Statistiques pour l’administrateur

- **Type** : frontend · **Priorité** : Souhaitable · **Besoin** : F9

En tant que **administrateur**, je veux voir le taux d’occupation par espace, les annulations et les absences, afin de piloter l’activité du coworking.

**Critères d'acceptation**

- [ ] Les statistiques sont filtrables par période.
- [ ] Le taux d’occupation est calculé par espace.
- [ ] Les données proviennent de l’API et non d’un calcul côté navigateur.

## S8 – Alertes

### US-23 : Alertes techniques

- **Type** : monitoring · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux des règles d’alerte sur la disponibilité, les erreurs, la latence, la base et les ressources, afin de être prévenue d’une panne avant les membres.

**Critères d'acceptation**

- [ ] API injoignable (1 min), 5xx supérieurs à 5 % (5 min), p95 supérieur à 1 s (5 min), base inaccessible ou saturée, CPU ou mémoire supérieurs à 85 %.
- [ ] Chaque règle a une gravité et une description indiquant l’action à mener.
- [ ] Les règles sont versionnées dans `monitoring/`.

### US-24 : Alertes métier

- **Type** : monitoring · **Priorité** : Important · **Besoin** : —

En tant que **équipe d’exploitation**, je veux des alertes sur l’activité de réservation, afin de détecter un problème que les métriques techniques ne montrent pas.

**Critères d'acceptation**

- [ ] Aucune réservation créée depuis 2 h en heures ouvrables.
- [ ] Taux d’annulation supérieur à 30 % sur 1 h.
- [ ] Échecs d’envoi de rappels supérieurs à 10 % sur 30 min.

### US-25 : Notifications d’alerte Telegram ou email

- **Type** : monitoring · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux recevoir les alertes sur Telegram ou par email, afin de les voir immédiatement.

**Critères d'acceptation**

- [ ] Alertmanager envoie un message lisible (nom, gravité, description).
- [ ] Un message « résolu » est envoyé quand l’alerte se termine.
- [ ] Les jetons et mots de passe sont dans des Secrets, pas dans Git.

## S9 – CI/CD

### US-26 : Pipeline d’intégration continue

- **Type** : ci-cd · **Priorité** : Indispensable · **Besoin** : —

En tant que **développeur**, je veux que chaque Pull Request compile le code et lance les tests, afin de ne jamais fusionner du code cassé.

**Critères d'acceptation**

- [ ] Un workflow GitHub Actions compile le backend et le frontend et exécute les tests unitaires.
- [ ] Le statut du workflow est exigé pour fusionner sur `main`.
- [ ] Un échec de test bloque la fusion.

### US-27 : Pipeline de déploiement continu

- **Type** : ci-cd · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux qu’un merge sur `main` construise les images et déploie sur le cluster, afin de livrer sans intervention manuelle.

**Critères d'acceptation**

- [ ] Les images sont construites, étiquetées avec le commit et publiées dans un registre.
- [ ] Le déploiement met à jour le cluster et vérifie la santé de l’application (smoke test).
- [ ] Aucun secret n’apparaît dans les logs du pipeline.

## S10 – Validation et rapport

### US-28 : Test de charge « lundi matin »

- **Type** : monitoring · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux simuler avec k6 un pic de réservations simultanées sur les mêmes espaces, afin de valider les performances et ajuster les seuils d’alerte.

**Critères d'acceptation**

- [ ] Le script k6 est versionné dans le dépôt.
- [ ] Les résultats (latence p95, taux d’erreurs) sont relevés et commentés.
- [ ] Aucune double réservation n’est constatée pendant le test.

### US-29 : Scénario de panne et démonstration

- **Type** : docs · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe d’exploitation**, je veux provoquer une panne et montrer la chaîne complète de détection, afin de prouver que le monitoring fonctionne.

**Critères d'acceptation**

- [ ] Le scénario est écrit étape par étape (arrêt de la base ou charge k6).
- [ ] Le dashboard passe au rouge, l’alerte arrive sur Telegram ou par email, puis se résout après rétablissement.
- [ ] La démo est répétée au moins une fois avant la présentation.

### US-30 : Rapport final et présentation

- **Type** : docs · **Priorité** : Indispensable · **Besoin** : —

En tant que **équipe**, je veux rédiger le rapport et préparer la présentation, afin de livrer le projet à l’encadrant.

**Critères d'acceptation**

- [ ] Le rapport couvre le contexte, l’architecture, les choix, les résultats des tests et les limites.
- [ ] Le support de présentation inclut la démonstration.
- [ ] Le README est à jour avec les commandes pour recréer l’environnement.
