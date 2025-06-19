#!/bin/bash

TARGET_SCRIPT="$(pwd)/run.sh" 
SERVICE_NAME="giveaway-bot"

# Проверка существования файла
if [ ! -f "$TARGET_SCRIPT" ]; then
    echo "Файл '$TARGET_SCRIPT' не найден!" >&2
    exit 1
fi

# Даём права на выполнение
chmod +x "$TARGET_SCRIPT"

# Создаём systemd-сервис
cat > "/etc/systemd/system/$SERVICE_NAME.service" <<EOF
[Unit]
Description=Автозапуск giveaway-bot
After=network.target

[Service]
ExecStart=$TARGET_SCRIPT
User=root
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

# Активируем сервис
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl start "$SERVICE_NAME"

# Создание Docker-образа
docker build -t giveaway-bot .

echo "Готово! Сервис '$SERVICE_NAME' создан и добавлен в автозагрузку."
echo "Проверить статус: systemctl status $SERVICE_NAME"