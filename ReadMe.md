# The Illustrated Book

## creation commands
dotnet new globaljson --sdk-version 8.0.100
dotnet new web --no-https --output IllustratedBook --framework net8.0
dotnet new sln -o IllustratedBook
dotnet sln IllustratedBook add IllustratedBook

cd IllustratedBook

dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0

# SQLServer Express 2022
Server=localhost\SQLEXPRESS;Database=master;Trusted_Connection=True;

# Seed data
The app will be seeded with one book, written by one user. The book will have one chapter and three pages. The chapter and pages are the book's sections, and they form a self-joining relationship in the database.

# EF migration cmds
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 8.0.0
dotnet ef migrations add Initial
dotnet ef database update