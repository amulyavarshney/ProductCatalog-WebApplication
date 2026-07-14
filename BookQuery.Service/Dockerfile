FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY BookCatalog.Contracts/BookCatalog.Contracts.csproj BookCatalog.Contracts/
COPY BookQuery.Service/BookQuery.Service.csproj BookQuery.Service/
RUN dotnet restore BookQuery.Service/BookQuery.Service.csproj
COPY BookCatalog.Contracts/ BookCatalog.Contracts/
COPY BookQuery.Service/ BookQuery.Service/
RUN dotnet publish BookQuery.Service/BookQuery.Service.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookQuery.Service.dll"]
