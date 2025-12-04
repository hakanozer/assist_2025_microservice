FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Services/Auth/RestApi.csproj src/Services/Auth/
RUN dotnet restore "src/Services/Auth/RestApi.csproj"
COPY . .
RUN dotnet build "src/Services/Auth/RestApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Auth/RestApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# data klasörü oluştur
RUN mkdir -p /app/data

# Veritabanı dosyasını kopyala
COPY --from=build /src/src/Services/Auth/restApi.db /app/data/restApi.db

# İzinleri ayarla
RUN chmod 666 /app/data/restApi.db && \
    chmod 777 /app/data

EXPOSE 5235
ENTRYPOINT ["dotnet", "RestApi.dll"]