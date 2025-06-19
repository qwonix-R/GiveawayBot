# Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Изменяем часовой пояс на UTC+3
ENV TZ=Europe/Moscow
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Копируем и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем исходники и собираем
COPY . .
RUN dotnet publish -c Release -o /giveawaybot/publish

# Запуск
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /giveawaybot

# Создаём папки для файлов и БД
RUN mkdir -p /giveawaybot/assets && \
    mkdir -p /giveawaybot/data

# Копируем собранное приложение
COPY --from=build /giveawaybot/publish .

# Копируем начальные БД
COPY data/admins.db /giveawaybot/data/
COPY data/posts.db /giveawaybot/data/
COPY data/giveaway.db /giveawaybot/data/
COPY data/broads.db /giveawaybot/data/

# Копируем файл config.json
COPY data/config.json /giveawaybot/data/

# Указываем том для данных (VOLUME можно смонтировать при запуске)
VOLUME /giveawaybot/assets
VOLUME /giveawaybot/data

# Запускаем приложение
ENTRYPOINT ["dotnet", "TgBot1.dll"]