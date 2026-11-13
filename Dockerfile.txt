# 1. مرحلة البناء والتحضير (Build Stage)
FROM ://microsoft.com AS build-env
WORKDIR /app

# نسخ ملف الـ Solution والمشاريع واستعادتها تلقائيًا
COPY *.sln ./
COPY Ecommerce.Domain/*.csproj ./Ecommerce.Domain/
COPY Ecommerce.Application/*.csproj ./Ecommerce.Application/
COPY Ecommerce.Infrastructure/*.csproj ./Ecommerce.Infrastructure/
COPY Ecommerce.Api/*.csproj ./Ecommerce.Api/
RUN dotnet restore

# نسخ بقية الأكواد وعمل نشر للمشروع كاملاً
COPY . ./
RUN dotnet publish -c Release -o out

# 2. مرحلة التشغيل الخفيفة والمحمية (Runtime Stage)
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
