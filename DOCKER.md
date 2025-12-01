# Docker Setup

## Alle Services mit Docker Compose starten

```bash
# Alle Services bauen und starten
docker-compose up --build

# Im Hintergrund laufen lassen
docker-compose up -d --build

# Logs anzeigen
docker-compose logs -f

# Bestimmten Service Logs
docker-compose logs -f bff
docker-compose logs -f api
docker-compose logs -f client

# Stoppen
docker-compose down

# Mit Volumes löschen
docker-compose down -v
```

## Services & Ports

| Service | Port (Host → Container) | URL |
|---------|------------------------|-----|
| Keycloak | 8080:8080 | http://localhost:8080 |
| API | 5001:5001 | http://localhost:5001 |
| BFF | 5000:5000 | http://localhost:5000 |
| Frontend | 4200:80 | http://localhost:4200 |

## Netzwerk

Alle Services laufen im gleichen Docker Network `app-network`:
- Services können sich über Service-Namen erreichen
- `api` ist unter `http://api:5001` erreichbar
- `keycloak` ist unter `http://keycloak:8080` erreichbar

## Environment Variables

### API
```yaml
ASPNETCORE_URLS: http://+:5001
Authentication__Authority: http://keycloak:8080/realms/bff-realm
Authentication__Audience: account
Authentication__RequireHttpsMetadata: false
```

### BFF
```yaml
ASPNETCORE_URLS: http://+:5000
OAuth__Authority: http://keycloak:8080/realms/bff-realm
OAuth__ClientId: bff-client
OAuth__ClientSecret: bff-secret
OAuth__RedirectUri: http://localhost:5000/auth/callback
ApiProxy__ApiBaseUrl: http://api:5001
```

## Entwicklung

### Einzelne Services neu bauen

```bash
# Nur BFF neu bauen
docker-compose up -d --build bff

# Nur API neu bauen
docker-compose up -d --build api

# Nur Frontend neu bauen
docker-compose up -d --build client
```

### In laufenden Container wechseln

```bash
# BFF Container
docker-compose exec bff /bin/bash

# API Container
docker-compose exec api /bin/bash

# Frontend Container (nginx)
docker-compose exec client /bin/sh
```

### Volumes prüfen

```bash
# Alle Volumes anzeigen
docker volume ls

# Keycloak Volume inspizieren
docker volume inspect c_sharp-backend-for-frontend_keycloak-data
```

## Troubleshooting

### Problem: Port bereits belegt

```bash
# Ports prüfen
lsof -i :5000  # BFF
lsof -i :5001  # API
lsof -i :8080  # Keycloak
lsof -i :4200  # Frontend

# Prozess beenden
kill -9 <PID>
```

### Problem: Keycloak startet nicht

```bash
# Keycloak Logs prüfen
docker-compose logs keycloak

# Keycloak neu starten
docker-compose restart keycloak

# Keycloak Volume löschen und neu starten
docker-compose down -v
docker-compose up keycloak
```

### Problem: BFF kann nicht zu API connecten

```bash
# Netzwerk prüfen
docker network inspect c_sharp-backend-for-frontend_app-network

# In BFF Container API erreichen testen
docker-compose exec bff curl http://api:5001/api/users
```

### Problem: Frontend kann BFF nicht erreichen

Das Frontend läuft im Browser, nicht im Container!
- Frontend muss `http://localhost:5000` verwenden (nicht `http://bff:5000`)
- In `environment.ts` muss `bffUrl: 'http://localhost:5000'` stehen

### Problem: CORS Fehler

```bash
# BFF CORS Policy prüfen
docker-compose exec bff cat /app/appsettings.json

# API CORS Policy prüfen
docker-compose exec api cat /app/appsettings.json
```

## Production Deployment

### Mit HTTPS

```yaml
services:
  bff:
    environment:
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword
    volumes:
      - ./certs:/https:ro
```

### Mit Environment-spezifischen Configs

```yaml
services:
  bff:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./appsettings.Production.json:/app/appsettings.Production.json:ro
```

### Mit Health Checks

```yaml
services:
  bff:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 40s
```

## Multi-Stage Builds

Alle Dockerfiles verwenden Multi-Stage Builds:

1. **Build Stage**: Kompiliert die Anwendung
2. **Runtime Stage**: Nur Runtime + kompilierte Files

Vorteile:
- ✅ Kleinere Images
- ✅ Keine Build-Tools in Production
- ✅ Bessere Security
- ✅ Schnellere Deployments

### Image Größen

- Frontend: ~50 MB (nginx:alpine)
- API: ~220 MB (aspnet:10.0)
- BFF: ~220 MB (aspnet:10.0)
- Keycloak: ~600 MB (offizielles Image)

## Best Practices

1. ✅ `.dockerignore` verwenden (reduziert Build Context)
2. ✅ Multi-Stage Builds (kleinere Images)
3. ✅ Health Checks definieren
4. ✅ Nicht als root laufen (in Production)
5. ✅ Secrets über Docker Secrets (nicht Environment Variables)
6. ✅ Logging zu stdout/stderr
7. ✅ Keine Secrets im Image
8. ✅ Specific Image Tags (nicht `:latest`)

## Cleanup

```bash
# Alle gestoppten Container entfernen
docker container prune

# Alle ungenutzten Images entfernen
docker image prune -a

# Alle ungenutzten Volumes entfernen
docker volume prune

# Alles auf einmal
docker system prune -a --volumes
```
