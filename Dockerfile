FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# نسخ ملفات المشاريع واستعادة الحزم
COPY ["Ecommerce.Api/Ecommerce.Api.csproj", "Ecommerce.Api/"]
COPY ["Ecommerce.Application/Ecommerce.Application.csproj", "Ecommerce.Application/"]
COPY ["Ecommerce.Domain/Ecommerce.Domain.csproj", "Ecommerce.Domain/"]
COPY ["Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj", "Ecommerce.Infrastructure/"]
RUN dotnet restore "Ecommerce.Api/Ecommerce.Api.csproj"

# نسخ بقية الكود وبناء التطبيق
COPY . .
WORKDIR "/src/Ecommerce.Api"
RUN dotnet publish "Ecommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# صورة التشغيل النهائية الخفيفة
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
