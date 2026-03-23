FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/DataBaseApi/DataBaseApi/DataBaseApi.csproj DataBaseApi/
RUN dotnet restore DataBaseApi/DataBaseApi.csproj

COPY src/DataBaseApi/DataBaseApi/ DataBaseApi/
RUN dotnet publish DataBaseApi/DataBaseApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Dossier pour les fichiers statiques (images uploadées)
RUN mkdir -p wwwroot/previews

EXPOSE 5078

ENTRYPOINT ["dotnet", "DataBaseApi.dll"]
