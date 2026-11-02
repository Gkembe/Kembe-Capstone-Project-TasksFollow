/*SELECT u.Email, r.Name AS RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'manager@gmail.com';*/

UPDATE AspNetUsers
SET IsActive = 1
WHERE Email = 'manager@gmail.com';