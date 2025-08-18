# The Illustrated Book

## global.json
global.json has to be changed from env to env to match the installed .Net SDK. The two versions are "8.0.101" and "8.0.410".

## appSettings
Never check in any sensitive data in appSettings. Keep it all in _appSettings.json, which is ignored by git, and copy the stuff into the appSettings.json file.

## Build
dotnet build

## Run the app
dotnet run

## DB
The app uses a **SQLExpress** database, running locally. The connection string is in the *appsettings* file. Use the SQL Server add-on to inspect the tables.

## Images
Once an image has been generated for a particular page (of a chapter, of a book), a reference to the picture is stored in the db for future ease of retrieval (and savings!). The picture itself (png) is saved in the _Images_ folder.

## Sections
The idea of Sections is to allow books to allow books to be divided into things other than chapters and pages. For instance, Shakespearean plays have acts and scenes. But that can come later. 