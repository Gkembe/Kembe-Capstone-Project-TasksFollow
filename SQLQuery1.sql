SELECT
    UserName,
    Email,
    CASE
        WHEN PasswordHash IS NULL THEN 'NO PASSWORD'
        ELSE 'PASSWORD EXISTS'
    END AS PasswordStatus
FROM AspNetUsers;