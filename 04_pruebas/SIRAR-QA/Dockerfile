FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SIRAR.Auditoria.csproj .
RUN dotnet restore SIRAR.Auditoria.csproj
COPY . .
RUN dotnet publish SIRAR.Auditoria.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SIRAR.Auditoria.dll"]
