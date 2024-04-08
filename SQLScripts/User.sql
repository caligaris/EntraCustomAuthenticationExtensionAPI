INSERT INTO CustomUserClaims (AppId, ClaimName, ClaimValue)
VALUES ('<UserPrincipalName>', 'CustomRoles', 'Admin, User');

INSERT INTO CustomUserClaims (AppId, ClaimName, ClaimValue)
VALUES ('<UserPrincipalName>', 'ApiVersion', '1.0.0');

SELECT * FROM CustomUserClaims

