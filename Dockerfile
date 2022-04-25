FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . .
RUN dotnet restore "Vk2Tg/Vk2Tg.csproj"
RUN dotnet build "Vk2Tg/Vk2Tg.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Vk2Tg/Vk2Tg.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Vk2Tg.dll"]
