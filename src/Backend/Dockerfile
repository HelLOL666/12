FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DocArchive.API/DocArchive.API.csproj", "DocArchive.API/"]
COPY ["DocArchive.Application/DocArchive.Application.csproj", "DocArchive.Application/"]
COPY ["DocArchive.Domain/DocArchive.Domain.csproj", "DocArchive.Domain/"]
COPY ["DocArchive.Infrastructure/DocArchive.Infrastructure.csproj", "DocArchive.Infrastructure/"]
RUN dotnet restore "DocArchive.API/DocArchive.API.csproj"
COPY . .
WORKDIR "/src/DocArchive.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/storage /app/logs
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "DocArchive.API.dll"]
