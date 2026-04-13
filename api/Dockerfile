# ==================== 构建阶段（使用 Docker Hub 镜像）====================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 还原依赖
COPY ProjectHub.Api.csproj .
RUN dotnet restore

# 编译并发布（自包含运行时，减小镜像依赖）
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ==================== 运行阶段（标准镜像，避免 Alpine 兼容性问题）====================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProjectHub.Api.dll"]
