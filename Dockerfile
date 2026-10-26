FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY StreetSignal.Dashboard.csproj ./
RUN dotnet restore StreetSignal.Dashboard.csproj
COPY Program.cs ./
COPY Models/ Models/
COPY Security/ Security/
COPY Services/ Services/
COPY Pages/ Pages/
COPY wwwroot/ wwwroot/
COPY appsettings.Production.json ./appsettings.json
RUN dotnet publish StreetSignal.Dashboard.csproj -c Release -o /out --no-restore /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet","StreetSignal.Dashboard.dll"]
