# Réservation de coworking avec monitoring et alertes

Projet DevOps réalisé en binôme (ENSA Fès). Une application de réservation pour un espace de coworking ouvert 24 h/24, inspirée du fonctionnement d'un coworking de Fès : Open Space, terrasse, phone box et salles de réunion. Les clients réservent sans compte et paient au départ selon le temps passé ; le gestionnaire suit l'occupation en direct, fait les check-in / check-out et traite les devis. L'application sera déployée sur Kubernetes et surveillée par Prometheus et Grafana, avec des alertes automatiques.

> **État du projet : semaine 2 (application utilisable de bout en bout).** API ASP.NET Core, frontend React et base PostgreSQL fonctionnent en local. Docker, CI/CD, Kubernetes et le monitoring arrivent les semaines suivantes. Détails : [docs/semaine-2.md](docs/semaine-2.md).

## Problématique

Deux questions guident le projet :

1. Comment permettre une réservation fiable, sans dépasser la capacité d'un espace ni réserver deux fois la même cabine, même quand deux clients réservent au même moment ?
2. Comment savoir qu'une application en production est en bonne santé, sans attendre qu'un client se plaigne ?

## Architecture cible

![Architecture cible](docs/architecture.png)

Version vectorielle : [docs/architecture.svg](docs/architecture.svg)

## Stack

| Couche | Technologie |
|---|---|
| Frontend | React 19 + Vite + TypeScript, React Router, Axios, CSS modules |
| Backend | ASP.NET Core 10 (Web API, Controllers, JWT), Swagger |
| Base de données | PostgreSQL 17, Entity Framework Core 10 + Npgsql (migrations) |
| Tests | xUnit, EF Core InMemory, FakeTimeProvider, Coverlet |
| Conteneurs et orchestration | Docker, Kubernetes (minikube ou kind en local), Helm *(à venir)* |
| Monitoring | Prometheus, Grafana, Alertmanager, Loki *(à venir)* |
| CI/CD | GitHub Actions *(à venir)* |

## Structure du dépôt

```
backend/
  CoworkingBooking.slnx              solution .NET (nouveau format .slnx du SDK 10)
  src/CoworkingBooking.Api/          API : Controllers, Services, Data (DbContext, seed, migrations), Models, DTOs
  tests/CoworkingBooking.Tests/      tests xUnit des services
frontend/                            application React (Vite)
docs/                                cahier des charges, schémas, documentation par semaine
k8s/                                 manifestes Kubernetes ou charts Helm (à venir)
monitoring/                          règles Prometheus, dashboards Grafana (à venir)
.github/                             workflows CI/CD et template de Pull Request
```

## Démarrage en local

### Prérequis

| Outil | Version testée | Vérification |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Outil EF Core | 10.x | `dotnet ef --version` (sinon `dotnet tool install --global dotnet-ef`) |
| Node.js / npm | 24 / 11 | `node -v`, `npm -v` |
| Docker | 28 | `docker --version` (pour PostgreSQL) |

### 1. Lancer PostgreSQL

```bash
docker run -d --name coworking-pg \
  -e POSTGRES_USER=coworking -e POSTGRES_PASSWORD=coworking_dev -e POSTGRES_DB=coworking \
  -p 5432:5432 postgres:17
```

Les fois suivantes : `docker start coworking-pg`.

### 2. Configurer et lancer l'API

```bash
cd backend/src/CoworkingBooking.Api
cp appsettings.Development.example.json appsettings.Development.json
# Remplacer Jwt:Key par une valeur aléatoire, par exemple : openssl rand -hex 32
```

`appsettings.Development.json` est ignoré par git : il ne doit jamais être commité. En dehors du développement, la configuration passe par des variables d'environnement (voir [.env.example](.env.example)) : `ConnectionStrings__Default`, `Jwt__Key`, `Admin__Email`, `Admin__Password`, `Frontend__Url`.

```bash
cd backend
dotnet ef database update -p src/CoworkingBooking.Api     # crée les tables, les espaces et les tarifs
dotnet run --project src/CoworkingBooking.Api --launch-profile http
```

- API : http://localhost:5080
- Swagger : http://localhost:5080/swagger
- Santé : http://localhost:5080/health (le processus répond) et http://localhost:5080/ready (PostgreSQL joignable)

En développement, `Database:MigrateOnStartup` vaut `true` : la migration est aussi appliquée au démarrage de l'API.

### 3. Lancer le frontend

```bash
cd frontend
cp .env.example .env.development.local   # VITE_API_URL=http://localhost:5080
npm install
npm run dev
```

Application : http://localhost:5173

### 4. Lancer les tests

```bash
cd backend
dotnet test                                                        # 107 tests
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"   # avec couverture (TestResults/)

cd ../frontend
npm run build                                                      # vérification TypeScript + build
npm run lint
```

### Comptes de test

| Rôle | Identifiants | Remarque |
|---|---|---|
| Gestionnaire | `gestionnaire@coworking.local` / `Gestion2026!` | Valeurs de `appsettings.Development.example.json`, créées au démarrage de l'API. À changer hors développement. |
| Client | aucun compte | Réservation avec nom + téléphone mobile marocain (06/07…). |

## Planning

| Semaine | Objectif |
|---|---|
| S1 | Cadrage, organisation, dépôt, architecture, modèle de données |
| S2 | Application de bout en bout : API, frontend, PostgreSQL, tests ✅ |
| S3 – S6 | Conteneurisation, CI/CD, Kubernetes, monitoring et alertes, tests de charge, démonstration |

## Contribuer

Voir [CONTRIBUTING.md](CONTRIBUTING.md) pour la stratégie de branches, les conventions de commit et le processus de Pull Request.

## Équipe

- Rim Ben Moussa
- Khadija Alaoui Mrani

Encadrant : Youssef Aounzo
