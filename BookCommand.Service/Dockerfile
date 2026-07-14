FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY BookCatalog.Contracts/BookCatalog.Contracts.csproj BookCatalog.Contracts/
COPY BookCommand.Service/BookCommand.Service.csproj BookCommand.Service/
RUN dotnet restore BookCommand.Service/BookCommand.Service.csproj
COPY BookCatalog.Contracts/ BookCatalog.Contracts/
COPY BookCommand.Service/ BookCommand.Service/
RUN dotnet publish BookCommand.Service/BookCommand.Service.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookCommand.Service.dll"]
