-- check-users.sql
-- Tabelnavn "Users" stammer fra DbSet<User> Users i AppDbContext
-- (src/UserService.Repository/Data/AppDbContext.cs).
-- UserConfiguration (Data/Configurations/UserConfiguration.cs) har ingen
-- ToTable()-override, så EF Core bruger DbSet-egenskabsnavnet som tabelnavn.
--
-- Kør alle forespørgsler i pgAdmins Query Tool mod databasen "userservice".
-- Ingen af forespørgslerne ændrer data eller skema.

-- ─────────────────────────────────────────────────────────────────────────────
-- Forespørgsel 1: EF-migrationshistorik
-- Forventet: mindst én række med MigrationId "20261120122544_InitialCreate".
-- Mangler tabellen, er migrationerne ikke kørt.
-- ─────────────────────────────────────────────────────────────────────────────
SELECT
    "MigrationId",
    "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";


-- ─────────────────────────────────────────────────────────────────────────────
-- Forespørgsel 2: kolonner i bruger-tabellen
-- Forventet: Id (integer), UserName (citext), PasswordHash (text), Role (integer).
-- Der må IKKE være en PasswordSalt-kolonne — saltet er indlejret i PasswordHash
-- via ASP.NET Core PasswordHasher<User>.
-- ─────────────────────────────────────────────────────────────────────────────
SELECT
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name   = 'Users'
ORDER BY ordinal_position;


-- ─────────────────────────────────────────────────────────────────────────────
-- Forespørgsel 3: antal brugere i alt og pr. rolle
-- Role gemmes som heltal: 0 = Player, 1 = Admin (se UserRole-enum i Domain).
-- Forventet: alle oprettede brugere fordelt på rolleværdi.
-- ─────────────────────────────────────────────────────────────────────────────
SELECT
    "Role",
    COUNT(*) AS antal
FROM "Users"
GROUP BY ROLLUP("Role")
ORDER BY "Role" NULLS LAST;


-- ─────────────────────────────────────────────────────────────────────────────
-- Forespørgsel 4: de 20 nyeste brugere (uden password-hash)
-- Viser Id, UserName og Role. Selve hash-strengen vises aldrig — kun dens
-- længde som en ekstra fornuftstjek (ASP.NET Core v3-hash er typisk 84 tegn).
-- ─────────────────────────────────────────────────────────────────────────────
SELECT
    "Id",
    "UserName",
    "Role",
    length("PasswordHash") AS hash_length
FROM "Users"
ORDER BY "Id" DESC
LIMIT 20;


-- ─────────────────────────────────────────────────────────────────────────────
-- Forespørgsel 5: dubletcheck på brugernavn (case-insensitivt)
-- Forventet resultat: ingen rækker.
-- UserName er af typen citext med et unikt indeks, så dubletter burde ikke
-- eksistere. Returnerer rækker med count > 1, hvis der alligevel er fejl.
-- ─────────────────────────────────────────────────────────────────────────────
SELECT
    lower("UserName") AS username_lower,
    COUNT(*)          AS antal
FROM "Users"
GROUP BY lower("UserName")
HAVING COUNT(*) > 1
ORDER BY antal DESC;
