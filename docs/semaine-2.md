# Semaine 2 : application utilisable de bout en bout

Objectif de la semaine : une application complète qui tourne en local, avec un backend ASP.NET Core, un frontend React et une base PostgreSQL. Docker, la CI, Kubernetes et le monitoring viennent ensuite. Le code a été écrit pour les faciliter : configuration uniquement par variables d'environnement, endpoints de santé, logs structurés.

## 1. Ce qui a été fait

| Domaine | Réalisé |
|---|---|
| Base de données | Modèle EF Core, migration initiale `InitialCreate`, seed des 8 espaces et des 10 tarifs, contraintes `CHECK`, index unique sur le code de réservation |
| API | 20 endpoints REST (publics et gestionnaire) + `/health` et `/ready`, validation avec messages en français, erreurs au format ProblemDetails, Swagger |
| Règles métier | Capacité des postes partagés, aucun chevauchement des cabines, devis pour les salles, place gardée 15 min, passage automatique en « Non présenté », tarification au départ, abonnement mensuel |
| Sécurité | JWT pour le gestionnaire, compte créé depuis la configuration, aucun secret dans le dépôt |
| Concurrence | Verrou PostgreSQL par espace : deux réservations simultanées ne dépassent jamais la capacité |
| Frontend | 6 pages publiques et 7 écrans gestionnaire, responsive (mobile d'abord), en français |
| Tests | 107 tests xUnit, environ 95 % de couverture sur les services |
| DevOps | `/health`, `/ready`, `ConnectionStrings__Default` et autres variables d'environnement, logs `ILogger` des événements métier |

### Évolution par rapport à la semaine 1

Le cadrage initial (branches `docs/user-stories` et `feat/database-model`) décrivait des comptes membres, des horaires d'ouverture par espace et un tarif horaire unique. En semaine 2, le sujet a été précisé sur le modèle d'un coworking réel de Fès, ce qui a changé plusieurs choix :

| Semaine 1 | Semaine 2 | Pourquoi |
|---|---|---|
| Comptes membres avec JWT | Pas de compte client ; seul le gestionnaire se connecte | Le client réserve en une minute avec nom et téléphone, comme à l'accueil |
| Horaires d'ouverture et fermetures | Ouvert 24 h/24, 7 j/7 | Fonctionnement réel de l'espace |
| Tarif horaire par espace | Table de tarifs : court, journée, nuit, heure entamée, devis | Grille tarifaire réelle |
| Contrainte d'exclusion `EXCLUDE USING gist` | Verrou `pg_advisory_xact_lock` par espace | Les postes partagés ont une capacité (17 places) : une contrainte d'exclusion ne sait gérer que « 0 ou 1 réservation » |
| Annulation refusée moins de 2 h avant | Annulation gratuite à tout moment | Règle de l'espace |

Les idées de la semaine 1 qui restent : dates en `timestamptz` (UTC en base), plages semi-ouvertes `[début, fin)`, une réservation annulée ou non présentée libère la place, statuts contrôlés.

## 2. Organisation du code

```
backend/src/CoworkingBooking.Api/
  Controllers/   reçoivent la requête HTTP, appellent un service, renvoient la réponse
  Services/      toutes les règles métier (testées)
  Data/          AppDbContext, seed, création du compte gestionnaire, migrations
  Models/        entités enregistrées en base
  DTOs/          formats des requêtes et réponses JSON
backend/tests/CoworkingBooking.Tests/   un fichier de tests par service
frontend/src/
  pages/         une page par écran (pages/admin pour le gestionnaire)
  components/    mise en page et petits composants communs
  api.ts         client Axios (URL dans VITE_API_URL, jeton JWT ajouté automatiquement)
```

| Service | Rôle |
|---|---|
| `PricingService` | Calcule le prix au départ. Ne touche ni à la base ni à l'horloge : tout est passé en paramètre. |
| `AvailabilityService` | Compte les réservations actives qui chevauchent un créneau ; pose le verrou PostgreSQL. |
| `BookingService` | Côté client : créer, consulter, annuler. |
| `FrontDeskService` | Côté gestionnaire : liste du jour, occupation, check-in, check-out, devis. |
| `NoShowService` + `NoShowBackgroundService` | Passe en « Non présenté » les réservations sans check-in après 15 min ; la tâche de fond l'appelle chaque minute. |
| `SubscriptionService`, `CatalogService`, `ContactService`, `AuthService` | Abonnements, espaces et tarifs, messages, connexion. |

L'heure n'est jamais lue directement : tous les services reçoivent un `TimeProvider`. En production c'est l'horloge système ; dans les tests c'est un `FakeTimeProvider` que l'on règle à la minute près (« il est 09:16 »).

## 3. Règles métier

### Espaces (seed)

| Espace | Type | Capacité | Tarifs |
|---|---|---|---|
| Open Space | Poste partagé | 17 postes | < 6 h : 25 DH · journée : 40 DH · nuit : 40 DH |
| Terrasse panoramique | Poste partagé | 8 places | < 6 h : 30 DH · journée : 50 DH · nuit : 50 DH |
| Phone box Solo 1 et Solo 2 | Cabine | 1 personne | 10 DH / heure entamée |
| Phone box Duo | Cabine | 2 personnes | 15 DH / heure entamée / personne |
| Salles Spark, Nexus, Summit | Salle privée | 4, 8, 12 personnes | sur devis |
| Abonnement mensuel | — | — | 800 DH |

Les tarifs sont en base (table `tariffs`) et modifiables par le gestionnaire.

### Réservation et disponibilité

- Un client donne son nom, un mobile marocain (06/07, `+212` accepté) et l'espace, la date et l'heure. Pour les cabines et les salles, il donne aussi le nombre de personnes et la durée en heures.
- Chaque réservation reçoit un code `CW-XXXXX`. Il fait 5 caractères et n'utilise ni 0, ni O, ni 1, ni I, pour éviter les confusions à l'oral.
- Seules les réservations **Confirmée** et **Arrivée** occupent une place.

| Type d'espace | Règle | Fenêtre d'occupation |
|---|---|---|
| Poste partagé | Refus (409) si les réservations qui se chevauchent atteignent la capacité | 6 h à partir de l'arrivée prévue |
| Cabine | Refus (409) si un créneau chevauche déjà ce créneau | durée choisie (1 à 8 h) |
| Salle privée | Crée une **demande de devis**. Refus (409) si un devis déjà validé chevauche le créneau. Le gestionnaire accepte (avec un montant) ou refuse. | durée choisie (1 à 12 h) |

- Un client arrivé qui dépasse sa fin prévue est considéré présent encore 1 h, pour ne pas revendre sa place pendant qu'il est assis.
- La place est gardée 15 min après l'heure prévue. Ensuite, la tâche de fond passe la réservation en **Non présenté** et la place est libérée. Un check-in tenté après ce délai est refusé.
- Le check-in est possible jusqu'à 30 min avant l'heure prévue.
- L'annulation est gratuite à tout moment, avec le code et le téléphone.

**Statuts :** Confirmée, Devis demandé, Arrivée, Terminée, Annulée, Non présenté, Refusée.

### Tarification au départ (`PricingService`)

Le prix est calculé sur le temps réel entre le check-in et le check-out :

| Espace | Calcul | Exemple |
|---|---|---|
| Poste partagé | Arrivée entre **20:00 et 05:59** → tarif nuit forfaitaire. Sinon : moins de 6 h → tarif court ; 6 h ou plus → tarif journée. | Open Space 09:05 → 15:35 (6 h 30) = 40 DH |
| Cabine Solo | Heures entamées × 10 DH (les secondes sont ignorées, minimum 1 h) | 1 h 05 = 2 h = 20 DH |
| Cabine Duo | Heures entamées × 15 DH × personnes | 1 h 05 à deux = 2 × 15 × 2 = 60 DH |
| Salle | Montant du devis accepté | 350 DH |
| Abonné | 0 DH sur les postes partagés ; cabines et salles payantes | — |

L'abonné est reconnu par son téléphone. Le statut est vérifié à la réservation, puis de nouveau au check-out : un client qui s'abonne en arrivant ne paie donc pas sa journée.

### Abonnement

Le client fait une demande en ligne (nom, téléphone, email, date de début), qui passe au statut **En attente de paiement**. Le gestionnaire l'active quand il encaisse les 800 DH. L'abonnement est valable un mois, bornes incluses (du 2 novembre au 1ᵉʳ décembre). S'il est activé après la date de début prévue, il démarre le jour de l'activation.

### Heure de Fès

Les dates sont stockées en UTC et converties avec le fuseau `Africa/Casablanca` côté serveur. L'API renvoie des heures locales avec leur décalage, par exemple `2026-11-02T09:00:00+01:00`. Le frontend les affiche telles quelles, sans les reconvertir.

C'est volontaire. Pendant les tests, Microsoft Edge 156 situait Casablanca en GMT+0 le 31 octobre 2026, alors que .NET et Node.js indiquaient GMT+1 : les bases de fuseaux horaires des navigateurs ne sont pas toujours d'accord avec celle du serveur. Le serveur est donc la seule référence. `GET /api/spaces` renvoie aussi `localNow`, l'heure actuelle à Fès selon le serveur, qui sert à pré-remplir les formulaires.

> **À vérifier avant la mise en production :** quelle source a raison pour l'heure légale actuelle au Maroc. La base de fuseaux de l'image Docker de la semaine suivante devra être à jour.

## 4. Endpoints principaux

Swagger : http://localhost:5080/swagger (bouton *Authorize* pour coller le jeton).

| Méthode | Route | Rôle |
|---|---|---|
| GET | `/api/spaces` | Espaces, tarifs, prix de l'abonnement, heure serveur |
| GET | `/api/spaces/{id}/availability?date=2026-11-02&time=09:30&durationHours=1` | Places restantes, ou créneaux occupés de la journée |
| POST | `/api/bookings` | Créer une réservation ou une demande de devis → 201 + code |
| GET | `/api/bookings/{code}?phone=06…` | Consulter |
| POST | `/api/bookings/{code}/cancel` | Annuler (`{ "phone": "06…" }`) |
| POST | `/api/subscriptions` | Demande d'abonnement |
| POST | `/api/contact` | Message de contact |
| POST | `/api/auth/login` | Connexion gestionnaire → JWT |
| GET | `/api/admin/bookings?date=…&status=…` | Réservations du jour, filtrables |
| GET | `/api/admin/occupancy` | Occupation actuelle par espace |
| POST | `/api/admin/bookings/{code}/check-in` | Arrivée |
| GET | `/api/admin/bookings/{code}/check-out` | Aperçu : durée et prix à encaisser (rien n'est enregistré) |
| POST | `/api/admin/bookings/{code}/check-out` | Départ : enregistre prix et moyen de paiement (`Especes` ou `Carte`) |
| POST | `/api/admin/quotes/{code}/accept` · `/reject` | Valider (avec montant) ou refuser un devis |
| GET · PUT | `/api/admin/prices` | Lire et modifier les tarifs |
| GET | `/api/admin/subscriptions?status=…` | Liste des abonnements |
| POST | `/api/admin/subscriptions/{id}/activate` | Activer après paiement |
| GET | `/api/admin/messages` | Messages de contact |
| GET | `/health` · `/ready` | Liveness et readiness (PostgreSQL) |

**Codes d'erreur :**
- 400 : donnée invalide, avec le message en français.
- 401 : jeton absent ou invalide.
- 404 : code inconnu, ou mauvais téléphone (on ne révèle pas si le code existe).
- 409 : espace complet, créneau pris, statut incompatible.

Exemple de réponse 409 :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflit",
  "status": 409,
  "detail": "Terrasse panoramique est complet à cette heure (8/8 places réservées)."
}
```

## 5. Tests

```bash
cd backend
dotnet test
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

| Fichier | Ce qui est vérifié |
|---|---|
| `PricingServiceTests` | Tarifs court, journée et 6 h pile ; nuit à 20:00 pile, à 19:59 (non), à 05:59 (oui), à 06:00 (non) ; nuit forfaitaire ; heures entamées (5 min, 1 h pile, 1 h 05, secondes ignorées) ; Duo × 2 ; abonné à 0 DH mais payant en cabine ; salle au devis ; erreurs |
| `AvailabilityTests` | Capacité atteinte → 409 ; dernière place acceptée ; clients arrivés comptés ; réservation annulée, non présentée ou terminée → place libérée ; fenêtre de 6 h ; client qui dépasse ; chevauchement de cabine → 409 ; créneau adjacent accepté ; devis validé bloquant |
| `NoShowTests` | Rien à 09:15 pile, « Non présenté » à 09:15:01 ; autres statuts intacts ; place libérée ensuite ; check-in en retard refusé |
| `BookingCodeTests` | Format `CW-XXXXX`, pas de caractères ambigus, codes aléatoires, code déjà utilisé ignoré |
| `BookingServiceTests` | Validation (téléphone, passé, personnes, durée), abonné reconnu, consultation, annulation |
| `FrontDeskServiceTests` | Scénario de démo complet, check-in trop tôt, check-out sans check-in, devis accepté puis facturé, devis en conflit, occupation, filtres |
| `OtherServicesTests` | Abonnements, tarifs modifiés puis appliqués, contact, connexion JWT |

**Résultat :** 107 tests réussis. La couverture des lignes est d'environ 95 % sur `Services/` et 75 % au global ; les migrations générées sont exclues.

Le verrou de concurrence a été vérifié à la main sur PostgreSQL : 6 requêtes simultanées sur la même cabine et le même créneau donnent 1 réponse 201 et 5 réponses 409.

## 6. Logs pour le monitoring (semaine 5)

Chaque événement métier est journalisé avec `ILogger`, avec des champs nommés réutilisables par Loki ou Prometheus :

- `Réservation créée {Code} : {Space}, arrivée {Arrival}, statut {Status}, abonné {IsSubscriber}`
- `Check-in {Code} : {Space}`
- `Check-out {Code} : {Space}, durée {Minutes} min, montant {Amount} DH ({Detail}), paiement {PaymentMethod}`
- `Réservation {Code} non présentée : {Space}, arrivée prévue {Arrival}`
- Annulation, devis accepté ou refusé, abonnement activé, tarif modifié, échec de connexion.

## 7. Captures

| Écran | Capture |
|---|---|
| Accueil (mobile) | [captures/accueil-mobile.png](captures/accueil-mobile.png) |
| Confirmation avec le code | [captures/confirmation-mobile.png](captures/confirmation-mobile.png) |
| Check-out et prix à encaisser | [captures/check-out-mobile.png](captures/check-out-mobile.png) |
| Tableau du jour du gestionnaire | [captures/tableau-du-jour.png](captures/tableau-du-jour.png) |

Autres captures conseillées pour le rapport :
- le message « complet » à la réservation ;
- Swagger avec le cadenas JWT ;
- la sortie de `dotnet test` ;
- la réponse de `/ready` ;
- l'écran des tarifs.

## 8. Scénario de démonstration

Préparation, avec trois terminaux ouverts :

```bash
docker start coworking-pg
cd backend && dotnet run --project src/CoworkingBooking.Api --launch-profile http
cd frontend && npm run dev
```

1. **Réserver un poste.** Sur http://localhost:5173, cliquer sur « Réserver » sous Open Space. L'heure proposée est le prochain quart d'heure. L'encadré affiche « 17 places libres sur 17 ». Saisir un nom et un mobile (`0612345678`), puis confirmer.
2. **Recevoir le code.** La page de confirmation affiche le code (par exemple `CW-7K3P9`) et les rappels : place gardée 15 min, paiement au départ, annulation gratuite.
3. **Faire le check-in.** Ouvrir http://localhost:5173/admin et se connecter avec `gestionnaire@coworking.local` / `Gestion2026!`. Dans le tableau du jour, la réservation apparaît « Confirmée ». Cliquer sur « Check-in » puis « Check-in (arrivée) ». L'occupation passe à 1/17.
4. **Faire le check-out.** Cliquer sur « Check-out (départ) ». L'écran affiche la durée, le calcul (« Tarif court (moins de 6 h) ») et **25 DH** à encaisser. Choisir Espèces, puis « Encaisser et terminer ».
5. **Montrer un refus quand c'est complet.** Le plus rapide est de remplir la Terrasse (8 places) avec un script, puis de tenter une 9ᵉ réservation dans l'interface, qui affiche « Complet à cette heure » :
   ```bash
   for i in 1 2 3 4 5 6 7 8; do
     curl -s -o /dev/null -w "%{http_code} " -X POST localhost:5080/api/bookings \
       -H 'Content-Type: application/json' \
       -d "{\"spaceId\":2,\"fullName\":\"Client $i\",\"phone\":\"07000000$i$i\",\"date\":\"2026-11-02\",\"time\":\"10:00\"}"
   done
   ```
   (adapter la date à une date future). Pour l'Open Space, il faut faire la même chose avec `spaceId: 1` et 17 réservations.
6. **Lancer les tests.** `cd backend && dotnet test` affiche « Réussi ! 107 tests ». Ouvrir `PricingServiceTests.cs` pour montrer les cas 20:00 pile et 05:59.

Bonus si le temps le permet :
- attendre 15 minutes après une réservation sans check-in : elle passe seule en « Non présenté » ;
- montrer `/health` et `/ready`, puis arrêter le conteneur PostgreSQL : `/ready` passe en `Unhealthy`, ce qui annonce le monitoring.

## 9. Limites connues et suite

- La fenêtre de 6 h des postes partagés est une estimation : un client peut rester plus longtemps. Il est alors compté présent tant qu'il n'a pas fait son check-out, avec une marge d'une heure.
- Pas d'envoi de SMS ou d'email : le client doit noter son code (lien « Voir ou annuler » sur la confirmation).
- Pas de tests d'intégration HTTP ni de tests du frontend pour l'instant. Les règles métier sont couvertes au niveau des services.
- Les semaines suivantes s'appuieront sur ce qui existe déjà :
  - **Docker** : variables d'environnement prêtes, voir `.env.example`.
  - **CI GitHub Actions** : `dotnet test` et `npm run build`.
  - **Kubernetes** : sondes `/health` et `/ready`.
  - **Monitoring** : logs métier déjà en place.
