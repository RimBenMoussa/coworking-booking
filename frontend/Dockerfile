# Frontend React : image multi-stage.
# Étape 1 (node) construit les fichiers statiques avec Vite, étape 2 (nginx) les sert.
# Construire depuis le dossier frontend/ :  docker build -t coworking-frontend frontend

# ---------- Étape 1 : build ----------
FROM node:24-alpine AS build
WORKDIR /app

# Dépendances d'abord (couche en cache tant que package*.json ne change pas).
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# URL de l'API intégrée au build. Vide par défaut : le navigateur appelle /api/... sur le même
# domaine que le frontend, et Nginx relaie vers le backend (voir nginx.conf).
# Une seule image fonctionne donc partout (docker compose, Kubernetes) et il n'y a pas de CORS.
ARG VITE_API_URL=""
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ---------- Étape 2 : Nginx ----------
# Variante "unprivileged" : Nginx tourne sans root et écoute sur le port 8080.
# Branche "stable" de Nginx (versions paires) : celle recommandée en production.
FROM nginxinc/nginx-unprivileged:1.30-alpine AS runtime

# Applique les correctifs de sécurité Alpine publiés après l'image de base
# (sinon le scan Trivy de la CI échoue sur des failles déjà corrigées).
# Le temps de la mise à jour on repasse root, puis on revient à l'utilisateur nginx (uid 101).
USER root
RUN apk upgrade --no-cache
USER nginx

# Adresse du backend vue depuis Nginx. "backend" = nom du service dans docker-compose.
# Sous Kubernetes, on la surcharge avec le nom complet du Service (ex. http://backend.coworking.svc.cluster.local:8080).
ENV BACKEND_URL=http://backend:8080
# Demande à l'image de lire le serveur DNS du conteneur et de le mettre dans NGINX_LOCAL_RESOLVERS.
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1

COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --retries=5 \
    CMD wget -q --spider http://127.0.0.1:8080/ || exit 1
