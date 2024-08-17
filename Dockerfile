FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Carsales.csproj", "./"]
RUN dotnet restore "Carsales.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Carsales.csproj" -c $configuration -o /app/builde

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Carsales.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Carsales.dll"]