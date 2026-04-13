# ==================== 构建阶段 ====================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 先复制 csproj，利用 Docker 层缓存加速依赖还原
COPY ProjectHub.Api.csproj .
RUN dotnet restore

# 复制全部源码并发布
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ==================== 运行阶段 ====================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# 暴露端口（ASP.NET Core 默认 8080）
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProjectHub.Api.dll"]
