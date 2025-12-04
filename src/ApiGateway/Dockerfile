FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
RUN dotnet restore "src/ApiGateway/ApiGateway.csproj"

COPY . .
RUN dotnet build "src/ApiGateway/ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8090
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
