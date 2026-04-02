```
	cd ..

	dotnet ef migrations add intialFirs -p .\gtas_vpp_be.Migrations\ -s .\gtas_vpp_be -c VPPMigrationDbContext

	dotnet ef database update -p .\gtas_vpp_be.Migrations -s .\gtas_vpp_be -c VPPMigrationDbContext
```
