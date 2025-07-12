Never place CSS or LESS directly in a component. Place it in the wwwroot/less folder, preferably in one of the existing  files if it's appropriate, and reference it in the component if it's not automatically being referenced. Don't try to compile the LESS to CSS - that happens at build time.

If trying to run the app, never use the "&&" operator as it will only cause an error. Try something like these two commands, one at a time:

cd IllustratedBook
dotnet run