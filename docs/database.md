# Database — UserService

Vejledning til at forbinde til UserService-databasen i clusteret med pgAdmin 4,
oprette en testbruger og køre kontrol-scriptet.

---

## Forbind med pgAdmin 4

### Forudsætning: Tailscale

Clusteret eksponerer Postgres via Tailscale. Installer og log ind på Tailscale,
og sørg for at du er på det rigtige netværk, inden du fortsætter.

### Forbindelsesparametre

| Felt | Værdi |
|---|---|
| Host | `quiz-postgres.tail5a1bc5.ts.net` |
| Port | `5432` |
| Maintenance DB | `postgres` |
| Brugernavn | `postgres` |
| Database | `userservice` |

I pgAdmin: højreklik **Servers → Register → Server**,
udfyld **General** (valgfrit navn, f.eks. "quiz-postgres"),
og **Connection** med ovenstående værdier.

### Hent adgangskoden

Adgangskoden gemmes som en Kubernetes-hemmelighed og må **ikke** skrives i denne fil.

**Linux/macOS (bash):**
```bash
kubectl -n quiz get secret postgresql-auth \
  -o jsonpath="{.data.postgres-password}" | base64 -d
```

**Windows (PowerShell):**
```powershell
kubectl -n quiz get secret postgresql-auth `
  -o jsonpath="{.data.postgres-password}" |
  % { [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_)) }
```

Alternativt: spørg Asger om adgangskoden.

---

## Fallback: kubectl port-forward

Har du ikke Tailscale tilgængeligt, kan du tunnele direkte:

```bash
kubectl -n quiz port-forward svc/postgresql 15432:5432
```

Brug derefter disse forbindelsesparametre i pgAdmin:

| Felt | Værdi |
|---|---|
| Host | `localhost` |
| Port | `15432` |
| Maintenance DB | `postgres` |
| Brugernavn | `postgres` |
| Database | `userservice` |

---

## Opret en testbruger

Inden du kører kontrol-scriptet, bør der være mindst én bruger i databasen.

### Via edge (anbefalet)

```http
POST /api/userservice/users
Content-Type: application/json

{
  "userName": "pgtest_1",
  "password": "testtest123"
}
```

### Via port-forward til UserService

```bash
kubectl -n quiz port-forward svc/userservice 15001:80
```

```http
POST http://localhost:15001/api/users
Content-Type: application/json

{
  "userName": "pgtest_1",
  "password": "testtest123"
}
```

Et vellykket kald returnerer HTTP 201 med brugerens `id`, `userName` og `role`.

---

## Kør check-users.sql i pgAdmin Query Tool

1. Åbn pgAdmin og forbind til `userservice`-databasen (se ovenfor).
2. Vælg databasen **userservice** i træet til venstre.
3. Klik **Tools → Query Tool** (eller tryk `Alt+Shift+Q`).
4. Åbn filen: **File → Open** og naviger til `docs/sql/check-users.sql`.
5. Klik **Execute / Refresh** (F5) for at køre alle forespørgsler.

### Hvad skal hvert resultat vise?

| Forespørgsel | Hvad du ser | Hvad det betyder |
|---|---|---|
| 1 — `__EFMigrationsHistory` | Mindst én række: `20261120122544_InitialCreate` | EF-migrationer er kørt korrekt |
| 2 — `information_schema.columns` | Kolonnerne `Id`, `UserName` (citext), `PasswordHash` (text), `Role` (integer) — ingen `PasswordSalt` | Skemaet matcher domænemodellen; salt er indlejret i hashen |
| 3 — antal pr. rolle | Én eller flere rækker med rolleværdi og antal | Viser brugerfordeling; Role 0 = Player, 1 = Admin |
| 4 — 20 nyeste brugere | Rækker med `Id`, `UserName`, `Role` og `hash_length` (~84) | Bekræfter at brugerne er gemt; selve hashen vises aldrig |
| 5 — dubletcheck | Ingen rækker | citext-indekset forhindrer case-insensitive dubletter |

---

## Vigtig regel

Du må **ikke** ændre data eller skema direkte i databasen.
Skemaændringer sker udelukkende via EF Core-migrationer
(`dotnet ef migrations add ...`), og dataret­telser sker via API'et.
Direkte SQL-INSERT/UPDATE/DELETE kan bryde applikationens tilstand.
