-- ════════════════════════════════════════════════════════════════════════
--  ScrumFlix — Phase 1, Item 12: Deactivate Development Leftover Accounts
--  Target: Aiven Cloud (defaultdb) — run during Phase 3 DB prep.
--  Safe to run multiple times (idempotent — sets IsActive = 0).
-- ════════════════════════════════════════════════════════════════════════

UPDATE Users SET IsActive = 0 WHERE UserId IN (2, 123);
-- UserId=2   (e1) — Employee-role test account
-- UserId=123 (a2) — Second Admin account, never logged in

-- Verify:
SELECT UserId, UserName, RoleId, IsActive
FROM   Users
WHERE  UserId IN (2, 123);
