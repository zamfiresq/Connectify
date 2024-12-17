﻿using Connectify.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Connectify.Models
{
    // PASUL 4: useri si roluri
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService
            <DbContextOptions<ApplicationDbContext>>()))
            {
                // Verificam daca in baza de date exista cel putin un rol insemnand ca a fost rulat codul
                // De aceea facem return pentru a nu insera rolurile inca o data
                // Acesta metoda trebuie sa se execute o singura data
                if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }
                // CREAREA ROLURILOR IN BD
                // daca nu contine roluri, acestea se vor crea
                context.Roles.AddRange(
                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7210", Name = "Admin", NormalizedName = "Admin".ToUpper() },
                

                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7211", Name = "Editor", NormalizedName = "Editor".ToUpper() },
                

                new IdentityRole
                {
                    Id = "2c5e174e-3b0e-446f-86af-483d56fd7212", Name = "User", NormalizedName = "User".ToUpper() }
                

                );

                // o noua instanta pe care o vom utiliza pentru crearea parolelor utilizatorilor
                // parolele sunt de tip hash

                var hasher = new PasswordHasher<ApplicationUser>();
                // admin - id = b0, id_role = 10
                // editor - id = b1, id_role = 11
                // user - id = b2, id_role = 12

                // alexandra (admin) - id = b3, id_role = 10
                // darius (admin) - id = b4, id_role = 10


                // CREAREA USERILOR IN BD
                // Se creeaza cate un user pentru fiecare rol
                context.Users.AddRange(
                new ApplicationUser
                {
                    // PK 
                    // admin
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb0",

                    UserName = "admin@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    NormalizedUserName = "ADMIN@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Admin01pa55!"),
                    FirstName = "Admin",
                    LastName = "Admin"
                },

                new ApplicationUser
                {
                    // PK
                    // editor
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb1",

                    UserName = "editor@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "EDITOR@TEST.COM",
                    Email = "editor@test.com",
                    NormalizedUserName = "EDITOR@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Editor1!"),
                    FirstName = "Editor",
                    LastName = "Si Atat",
                    IsPrivate = false
                },

                new ApplicationUser
                {
                    // PK
                    // user
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb2",

                    UserName = "user@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedUserName = "USER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User1!"),
                    FirstName = "Mihaita",
                    LastName = "Dragan",
                    IsPrivate = false

                },

                new ApplicationUser
                {
                    // PK
                    // alexandra (admin)
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb3",

                    UserName = "zamfi@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "ZAMFI@TEST,COM",
                    Email = "zamfi@test.com",
                    NormalizedUserName = "ZAMFI@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Zamfi1!"),
                    FirstName = "Alexandra",
                    LastName = "Zamfirescu",
                    IsPrivate = false

                },

                new ApplicationUser
                {
                    // PK
                    // darius (admin)
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb4",
                    UserName = "darius@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "DARIUS@TEST.COM",
                    Email = "darius@test.com",
                    NormalizedUserName = "DARIUS@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Darius1!"),
                    FirstName = "Darius",
                    LastName = "Suditu",
                    IsPrivate = false
                });


                // ASOCIEREA USER-ROLE
                context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210", // admin
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0" // admin
                },

                new IdentityUserRole<string>

                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7211", // editor
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb1"  // editor si atat
                },

                new IdentityUserRole<string>

                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212", // user
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2"  // mihaita dragan
                },

                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210", // admin
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb3"  // alexandra zamfirescu
                },

                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210", // admin
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb4"  // darius suditu
                });

                context.SaveChanges();
            }
        }
    }
}
