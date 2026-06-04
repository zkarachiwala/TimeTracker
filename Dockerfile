FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TimeTracker.Web/TimeTracker.Web.csproj", "TimeTracker.Web/"]
COPY ["TimeTracker.Shared/TimeTracker.Shared.csproj", "TimeTracker.Shared/"]
RUN dotnet restore "TimeTracker.Web/TimeTracker.Web.csproj"
COPY . .
WORKDIR "/src/TimeTracker.Web"
RUN dotnet publish "TimeTracker.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TimeTracker.Web.dll"]
