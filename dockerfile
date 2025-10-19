# 1️⃣ Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Solution ve projeyi kopyala
COPY *.sln ./
COPY BilbakalimAPI/*.csproj BilbakalimAPI/
RUN dotnet restore

# Tüm projeyi kopyala ve publish et
COPY . .
WORKDIR /src/BilBakalimAPI
RUN dotnet publish -c Release -o /app/publish

# 2️⃣ Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Use the same URL/port as in Properties/launchSettings.json for consistency
ENV ASPNETCORE_URLS=http://+:32005
EXPOSE 32005

ENTRYPOINT ["dotnet", "BilbakalimAPI.dll"]
