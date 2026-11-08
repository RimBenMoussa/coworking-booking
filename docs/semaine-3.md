# Semaine 3 : Docker et intégration continue

Objectif de la semaine : l'application démarre avec `docker compose up`, et chaque push est testé automatiquement. Le pipeline bloque un test cassé, publie des images Docker vérifiées, et prépare le déploiement Kubernetes de la semaine 4.

## 1. Ce qui a été fait

| Tâche | Réalisé | PR |
|---|---|---|
| Dockerfiles multi-stage (Khadija) | Backend `dotnet/sdk` → `dotnet/aspnet` en non-root ; frontend Vite → Nginx non-root qui relaie `/api` | #35 |
| `docker-compose.yml` (Rim) | PostgreSQL + volume, API, frontend ; `.env` non commité ; healthchecks et ordre de démarrage | #36 |
| GitHub Actions (Khadija) | `dotnet build` + `dotnet test`, puis `npm ci` + lint + build Vite, à chaque push | #37 |
| Qualité et sécurité (Rim) | SonarCloud (C# + TypeScript), scan Trivy des images qui échoue sur une faille critique | #38 |
| Publication (Rim et Khadija) | Push vers GitHub Container Registry, tag = SHA du commit, sans secret à créer | #38 |

**Écart avec la feuille de suivi :** le lint utilise **oxlint**, pas ESLint. Il est en place depuis la semaine 2 (`frontend/.oxlintrc.json`) et joue le même rôle, en plus rapide.

## 2. Les images Docker

### Backend (`backend/Dockerfile`)

```
sdk:10.0   restore (couche en cache) → publish Release
   │ copie de /app uniquement
aspnet:10.0   runtime .NET + l'application, utilisateur "app" (uid 1654), port 8080
```

- **Multi-stage** : le SDK (environ 1 Go, compilateur, NuGet) ne sert qu'à construire. L'image finale ne contient que le runtime : **365 Mo**.
- **Cache des couches** : le `.csproj` est copié et restauré avant le code source. Quand on modifie un fichier `.cs`, Docker réutilise les paquets NuGet déjà téléchargés.
- **Non-root** : `USER $APP_UID`. Si l'API était compromise, l'attaquant n'aurait pas les droits root dans le conteneur.
- **`tzdata`** : fournit le fuseau `Africa/Casablanca` utilisé par `LocalTime.cs` (point ouvert de la semaine 2, vérifié dans l'image).
- **Healthcheck** : `curl /health`.

### Frontend (`frontend/Dockerfile`)

```
node:24-alpine   npm ci → npm run build (dist/)
   │ copie de dist/ uniquement
nginx-unprivileged:1.30-alpine   fichiers statiques + relais /api, utilisateur nginx (uid 101), port 8080
```

- Image finale de **82 Mo** : ni Node, ni `node_modules`.
- `apk upgrade` dans l'image finale : il applique les correctifs Alpine publiés après l'image de base. Sans lui, Trivy trouvait 1 faille critique et 53 hautes, toutes déjà corrigées par Alpine.

### Pourquoi Nginx relaie `/api`

`VITE_API_URL` est **figée dans le JavaScript au moment du build**. Une image construite pour `http://localhost:5080` ne fonctionnerait plus derrière une autre adresse. On construit donc avec une URL vide :

```
navigateur ──> http://localhost:8080/            ──> Nginx : fichiers React
           ──> http://localhost:8080/api/spaces  ──> Nginx ──> backend:8080/api/spaces
```

- **Une seule image** convient à docker compose comme à Kubernetes.
- **Pas de CORS** : le navigateur ne voit qu'un seul domaine.
- L'adresse du backend vient de la variable `BACKEND_URL` (modèle `nginx/default.conf.template`). Sous Kubernetes, on la remplacera par le nom complet du Service (par exemple `http://backend.coworking.svc.cluster.local:8080`), parce que le résolveur DNS de Nginx n'utilise pas les domaines de recherche.

## 3. docker-compose

```
db (postgres:17)        healthcheck pg_isready
  │ service_healthy
backend (API)           healthcheck curl /ready   (= PostgreSQL joignable)
  │ service_healthy
frontend (Nginx)        publié sur localhost:8080
```

- **`.env`** (ignoré par git) contient les secrets. `.env.example` en donne le modèle. Compose les transmet à l'API sous leur nom ASP.NET Core : `JWT_KEY` → `Jwt__Key`, `POSTGRES_PASSWORD` → dans `ConnectionStrings__Default`, etc.
- **`${VAR:?message}`** : si un secret manque, compose refuse de démarrer et affiche le message.
- **Volume `pgdata`** : les données survivent à `docker compose down`. Pour repartir de zéro : `docker compose down -v`.
- **Migrations au démarrage** (`Database__MigrateOnStartup=true`) : les tables et le seed sont créés au premier lancement.
- **La base n'est pas publiée** sur la machine. L'API est publiée sur `127.0.0.1:5080` seulement, pour Swagger.

```bash
cp .env.example .env     # remplir POSTGRES_PASSWORD, JWT_KEY (openssl rand -hex 32), ADMIN_*
docker compose up --build
docker compose ps        # 3 services "healthy"
docker compose logs -f backend
docker compose down      # (-v pour effacer aussi la base)
```

## 4. Le pipeline CI (`.github/workflows/ci.yml`)

Déclenché à **chaque push, sur toutes les branches**.

```
push
 ├─ Backend (.NET build + tests)   restore → build Release → 107 tests + couverture
 ├─ Frontend (lint + build Vite)   npm ci → oxlint → tsc + vite build
 ├─ Qualité (SonarCloud)           analyse C# + TypeScript, couverture
 │
 └─ (si backend ET frontend sont verts)
    ├─ Image backend    build → Trivy ─┐
    └─ Image frontend   build → Trivy ─┴─> sur main seulement : push ghcr.io (SHA + latest)
```

| Choix | Pourquoi |
|---|---|
| `needs: [backend, frontend]` | On ne construit ni ne publie une image dont le code n'est pas testé |
| `concurrency` | Deux pushs rapprochés : le premier pipeline est annulé, pas de file d'attente |
| `permissions: contents: read` | Droits minimaux ; seul le job docker reçoit `packages: write` |
| Résultats de tests en artefact (`if: always()`) | On peut télécharger le `.trx` même quand un test échoue |
| `npm ci` plutôt que `npm install` | Installe exactement les versions du `package-lock.json` |

### Sécurité : Trivy

- Chaque image est scannée **avant** d'être publiée.
- `severity: CRITICAL`, `exit-code: 1`, `ignore-unfixed: true` : **le pipeline échoue s'il reste une faille critique pour laquelle un correctif existe**. Une faille sans correctif ne bloque pas, puisqu'on ne pourrait rien y faire.
- **Action épinglée par SHA** (`aquasecurity/trivy-action@ed142fd…` = v0.36.0). En mars 2026, des attaquants ont réécrit les tags de cette action pour voler les secrets des pipelines ([CVE-2026-33634](https://github.com/aquasecurity/trivy/security/advisories/GHSA-69fq-xp46-6x23)). Un tag peut être déplacé, un SHA de commit non.
- Résultat actuel : **0 faille corrigeable de niveau HIGH ou CRITICAL** sur les deux images.

### Qualité : SonarCloud

- `dotnet-sonarscanner begin` → build et tests → `end`. Le scanner observe la compilation C#, et `sonar.scanner.scanAll=true` ajoute le TypeScript du frontend : un seul projet Sonar pour tout le dépôt.
- La couverture vient de Coverlet au format `opencover`, ajouté dans `coverlet.runsettings`. Les migrations sont exclues.
- **Si le secret `SONAR_TOKEN` est absent**, le job est sauté avec un avertissement, et le pipeline reste vert.

### Publication : GitHub Container Registry

- `ghcr.io/rimbenmoussa/coworking-booking-backend:<sha>` et `ghcr.io/rimbenmoussa/coworking-booking-frontend:<sha>`, plus `latest`.
- **Tag = SHA du commit** : on sait exactement quel code tourne. C'est ce tag que Kubernetes utilisera en semaine 4 (`latest` n'est pas reproductible).
- Push **uniquement depuis `main`**. Les branches de travail sont testées et scannées, mais ne publient rien.
- Authentification avec le `GITHUB_TOKEN` fourni automatiquement par Actions : **aucun secret à créer** pour GHCR.

## 5. Configuration GitHub (une seule fois)

### Secrets

| Secret | Où | Pour |
|---|---|---|
| `SONAR_TOKEN` | Settings → Secrets and variables → Actions | Analyse SonarCloud |
| `GITHUB_TOKEN` | fourni automatiquement | Push GHCR |

Les secrets de l'application (`JWT_KEY`, mots de passe) ne vont pas dans la CI : elle n'en a pas besoin, puisque les tests utilisent EF Core InMemory. Ils sont dans `.env` en local, et seront dans les Secrets Kubernetes en semaine 4.

### SonarCloud

1. https://sonarcloud.io → se connecter avec GitHub → importer `RimBenMoussa/coworking-booking`.
2. Organisation `rimbenmoussa`, clé `RimBenMoussa_coworking-booking`. Si elles sont différentes, créer les **variables** GitHub `SONAR_ORGANIZATION` et `SONAR_PROJECT_KEY`.
3. *Administration → Analysis Method* : **désactiver Automatic Analysis**, puisque l'analyse se fait dans la CI.
4. *My Account → Security* : générer un jeton, puis le créer dans GitHub sous le nom `SONAR_TOKEN`.

### Bloquer la fusion si la CI est rouge

Settings → Rules → Rulesets → **main** → *Require status checks to pass* → ajouter :

- `Backend (.NET build + tests)`
- `Frontend (lint + build Vite)`
- `Image backend (build + Trivy + GHCR)`
- `Image frontend (build + Trivy + GHCR)`

À faire **après** la fusion du workflow dans `main`, sinon les PR créées avant le workflow ne pourraient jamais être fusionnées.

### Rendre les images publiques (optionnel)

Une image GHCR est privée par défaut. Pour que Kubernetes la télécharge sans identifiants : profil GitHub → Packages → l'image → *Package settings* → *Change visibility* → Public.

## 6. Scénario de démonstration

**1. `docker compose up` en direct**

```bash
docker compose down -v               # base vide, pour montrer le premier démarrage
docker compose up --build
```

- Montrer l'ordre de démarrage : `db Healthy` → `backend Healthy` → `frontend Started`.
- Montrer les logs de l'API : la migration est appliquée, puis le compte gestionnaire est créé.
- `docker compose ps` : les 3 services sont `healthy`.
- http://localhost:8080 : faire une réservation, puis se connecter en gestionnaire.
- Bonus : `docker compose exec backend id` → `uid=1654(app)`, l'API ne tourne pas en root.

**2. Un push qui déclenche le pipeline**

Une petite modification sur une branche, puis `git push` : dans l'onglet *Actions*, les jobs démarrent. Montrer le graphe (backend et frontend en parallèle, puis les images), les 107 tests et le tableau Trivy.

**3. Un test cassé qui bloque**

On introduit un bug réaliste : le tarif de nuit commence à 21 h au lieu de 20 h.

```bash
git checkout -b demo/bug-tarif-nuit
# backend/src/CoworkingBooking.Api/Services/PricingService.cs, ligne 39 :
#   return hour >= 20 || hour < 6;   →   return hour > 20 || hour < 6;
git commit -am "fix: tarif de nuit"
git push -u origin demo/bug-tarif-nuit
gh pr create --fill
```

- Le job **Backend** échoue : `ArrivalAtExactly2000_IsNightRate` attendait 40 DH et en reçoit 25.
- Les jobs **Image** ne démarrent même pas (`needs`), donc aucune image cassée n'est publiée.
- Sur la PR, le bouton de fusion est **bloqué** par la règle de protection.
- Corriger, pousser : le pipeline redevient vert et la PR peut être fusionnée. Sinon, fermer la PR.

## 7. Limites et suite

- **SonarCloud** n'est pas bloquant : le *quality gate* est affiché, mais il ne fait pas échouer le pipeline. Pour qu'il bloque, ajouter `/d:sonar.qgate.wait=true` à l'étape `begin`.
- **Avertissement DataProtection** au démarrage de l'API : les clés ne sont pas persistées. Sans conséquence ici, car l'application n'utilise que des jetons JWT, pas de cookies.
- **`ubuntu-latest`** passera à Ubuntu 26. À surveiller si un job casse sans changement de code.
- **Semaine 4 (Kubernetes)** : déployer les images `ghcr.io/...:<sha>`, `BACKEND_URL` avec le nom complet du Service, sondes `livenessProbe` sur `/health` et `readinessProbe` sur `/ready`, secrets dans des Secrets Kubernetes.
