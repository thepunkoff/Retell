FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . .
RUN dotnet restore "Retell/Retell.csproj"
RUN dotnet build "Retell/Retell.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Retell/Retell.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Retell.dll"]