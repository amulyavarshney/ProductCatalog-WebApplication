FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY BookCatalog.Gateway/BookCatalog.Gateway.csproj BookCatalog.Gateway/
RUN dotnet restore BookCatalog.Gateway/BookCatalog.Gateway.csproj
COPY BookCatalog.Gateway/ BookCatalog.Gateway/
RUN dotnet publish BookCatalog.Gateway/BookCatalog.Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookCatalog.Gateway.dll"]
