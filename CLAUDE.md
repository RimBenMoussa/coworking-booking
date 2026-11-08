# CLAUDE.md : contexte du projet coworking-booking

Projet DevOps de binôme (ENSA Fès, Rim Ben Moussa et Khadija Alaoui Mrani, encadrant Youssef Aounzo) : une application de réservation pour un espace de coworking à Fès ouvert 24 h/24, inspirée de Chill & Work. Dépôt GitHub : RimBenMoussa/coworking-booking. Réponds en français. Le code doit rester simple : les deux étudiantes doivent pouvoir l'expliquer au prof.

## Où on en est
- Le projet dure 6 semaines. Les semaines 1 (cadrage) et 2 (application de bout en bout) sont **terminées**.
- Le travail de la semaine 2 a été livré par la PR #34 (`feat/semaine-2` vers `main`). Vérifie si elle est fusionnée avec `gh pr view 34`.
- Il n'y a pas de branche `develop`. On suit GitHub Flow (voir CONTRIBUTING.md) :
  - branches `feat/`, `fix/`, `infra/`, `ci/`, `monitoring/`, `docs/` ;
  - commits `type: description` ;
  - PR vers `main` relue par l'autre membre, puis squash merge.
- Les semaines suivantes portent sur Docker, CI GitHub Actions, Kubernetes, puis le monitoring (Prometheus, Grafana, Loki ; logs exploités en semaine 5), les alertes et les tests de charge. Le planning exact par semaine n'est pas fixé : demande-le.
- Lis d'abord `README.md` et `docs/semaine-2.md`. Ils décrivent tout : règles métier, endpoints, tarification, tests, scénario de démo.

## Stack et structure
- **backend/** :
  - .NET 10, solution `CoworkingBooking.slnx` (nouveau format du SDK 10).
  - `src/CoworkingBooking.Api` : ASP.NET Core Web API avec Controllers, EF Core 10 + Npgsql, noms en snake_case (EFCore.NamingConventions), JWT, Swashbuckle.
  - Dossiers : `Controllers/` (minces), `Services/` (toute la logique métier), `Data/` (AppDbContext, SeedData, AdminSeeder, Migrations/InitialCreate), `Models/`, `DTOs/`.
  - `tests/CoworkingBooking.Tests` : xUnit, EF Core InMemory, FakeTimeProvider ; 107 tests, environ 95 % de couverture sur Services/ ; `coverlet.runsettings` exclut les migrations.
- **frontend/** :
  - React 19 + Vite 8 + TypeScript 7, React Router 7, Axios, CSS modules plus `index.css` global, lint `oxlint`.
  - Fichiers : `src/api.ts` (Axios, URL dans `VITE_API_URL`, jeton JWT en localStorage), `src/useApi.ts`, `src/auth.tsx` + `src/authContext.ts`, `src/format.ts`, `src/pages/`, `src/pages/admin/`.
- **docs/** : `semaine-2.md`, captures dans `docs/captures/`, `architecture.png/.svg`.
- **k8s/, monitoring/, .github/workflows/** : encore vides (`.gitkeep`).

## Règles métier essentielles (déjà implémentées)
- **8 espaces en seed avec tarifs en base** (table `tariffs`, modifiables par le gestionnaire) :
  - Open Space (17 places : 25 DH moins de 6 h, 40 DH la journée, 40 DH la nuit) ;
  - Terrasse panoramique (8 places : 30 / 50 / 50) ;
  - Phone box Solo 1 et Solo 2 (10 DH par heure entamée) ;
  - Phone box Duo (15 DH par heure entamée et par personne) ;
  - salles Spark, Nexus, Summit (4, 8, 12 personnes, sur devis) ;
  - abonnement mensuel 800 DH (tarif sans espace associé).
- **Réservation :**
  - pas de compte client : nom et mobile marocain 06/07 ;
  - code unique `CW-XXXXX` (sans 0, O, 1, I) ;
  - seules les réservations Confirmee et Arrivee occupent une place.
- **Disponibilité :**
  - postes partagés : capacité sur une fenêtre de 6 h ;
  - cabines : aucun chevauchement ;
  - salles : statut DevisDemande, que le gestionnaire accepte (avec un montant) ou refuse ; pas de chevauchement avec un devis accepté.
  - Concurrence gérée par `pg_advisory_xact_lock(spaceId)` dans une transaction (AvailabilityService.LockSpaceAsync).
- **Délais :**
  - place gardée 15 min, puis passage en NonPresente par NoShowBackgroundService, toutes les minutes ;
  - check-in possible jusqu'à 30 min en avance, refusé après 15 min de retard ;
  - un client arrivé qui dépasse sa fenêtre compte encore 1 h (BookingRules.OverstayMargin) ;
  - annulation gratuite avec le code et le téléphone.
- **Prix au check-out (PricingService, fonction pure) :**
  - arrivée entre 20:00 et 05:59 : tarif nuit ;
  - sinon moins de 6 h : tarif court, 6 h ou plus : tarif journée ;
  - cabines : heures entamées (secondes ignorées, minimum 1 h), multipliées par le nombre de personnes pour la Duo ;
  - salle : montant du devis ;
  - abonné : 0 DH sur les postes partagés, abonnement revérifié au check-out.
- **Abonnement :** statut EnAttenteDePaiement, puis activé par le gestionnaire ; valable un mois, bornes incluses ; s'il est activé en retard, il démarre le jour de l'activation.
- **Statuts** (enums C# sans accents, stockés en texte) : Confirmee, DevisDemande, Arrivee, Terminee, Annulee, NonPresente, Refusee. Les libellés accentués sont côté frontend.

## Décisions techniques à respecter
- **Heure :** toujours via `TimeProvider`, jamais `DateTime.Now`. Stockage en UTC (timestamptz, `DateTimeOffset` avec un décalage nul). Conversion avec `Africa/Casablanca` dans `Services/LocalTime.cs`.
- **Le serveur est la seule référence pour l'heure.** L'API renvoie des heures locales avec leur décalage, et `GET /api/spaces` renvoie `localNow`. Le frontend affiche l'heure telle quelle (`wallClock` dans format.ts), sans conversion par le navigateur.
  - Raison : Edge 156 situait Casablanca en GMT+0 le 31/10/2026, alors que .NET et Node donnaient GMT+1.
  - Il reste à vérifier l'heure légale réelle au Maroc, et à s'assurer que la base de fuseaux de l'image Docker est à jour.
- **Erreurs :** `BusinessException` (400, 401, 404, 409) transformée en ProblemDetails par `ProblemDetailsExceptionHandler`. Validation par DataAnnotations avec des messages en français.
- **Configuration uniquement par la config ou des variables d'environnement :**
  - variables : `ConnectionStrings__Default`, `Jwt__Key` (32 caractères minimum, l'API refuse de démarrer sinon), `Admin__Email` / `Admin__Password` (compte gestionnaire créé ou mis à jour au démarrage), `Frontend__Url` (CORS, valeurs séparées par des virgules), `Database__MigrateOnStartup`, `Swagger__Enabled` ;
  - modèle : voir `.env.example` à la racine.
- **Aucun secret dans le dépôt.** `appsettings.Development.json` et `.env*` sont ignorés par git ; seuls `appsettings.Development.example.json`, `.env.example` et `frontend/.env.example` sont commités.
- **Santé :** `/health` (liveness, sans vérification) et `/ready` (DbContext check, tag "ready").
- **Logs ILogger** structurés sur les événements métier (création, check-in, check-out, non-présenté, devis, abonnement, tarifs) : ils serviront au monitoring.

## Lancer le projet en local
- PostgreSQL : `docker start coworking-pg`. Si le conteneur n'existe pas, voir le README (`postgres:17`, utilisateur et base `coworking`, mot de passe `coworking_dev`, port 5432).
- API : `cd backend && dotnet ef database update -p src/CoworkingBooking.Api && dotnet run --project src/CoworkingBooking.Api --launch-profile http`. Elle écoute sur http://localhost:5080 (Swagger sur /swagger).
- Frontend : `cd frontend && npm run dev`, sur http://localhost:5173. `VITE_API_URL` est défini dans `.env.development.local`, copié depuis `.env.example`.
- Gestionnaire de développement : `gestionnaire@coworking.local` / `Gestion2026!`.
- Vérifications avant chaque PR : `dotnet test` (107 tests), `npm run build` et `npm run lint` (0 avertissement attendu).
- **Piège Windows :** si une API ou un Vite tourne encore, `dotnet build` échoue (exe verrouillé) ou Vite ne trouve pas son port. Arrête le processus avant de relancer.

## Points ouverts
- Les branches de la semaine 1 `docs/user-stories` et `feat/database-model` ne sont pas fusionnées : leur modèle (comptes membres, horaires d'ouverture, contrainte d'exclusion) est remplacé, et l'explication est dans docs/semaine-2.md, section 1. Il reste à décider si on les ferme ou si on met à jour les user stories.
- Pour repartir d'une base vide : `docker rm -f coworking-pg`, relancer le conteneur, puis `dotnet ef database update`.
- Limites connues :
  - pas de SMS ni d'email ;
  - pas de tests d'intégration HTTP ni de tests frontend ;
  - la boucle de NoShowBackgroundService n'est pas testée (sa logique `MarkNoShowsAsync` l'est).

## Comment travailler
Avant de coder une nouvelle étape (par exemple Dockerfiles, docker-compose, workflow CI) :
1. Analyse l'état du dépôt.
2. Propose un plan court et attends la validation.
3. Travaille sur une branche dédiée créée depuis `main`, avec des commits petits et clairs.
4. Après chaque grande étape, montre ce qui a été fait et ce qu'il reste.
