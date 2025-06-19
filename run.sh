#!/bin/bash
sudo docker build -t giveaway-bot .
sudo docker run -d --restart unless-stopped -v $(pwd)/assets:/giveawaybot/assets -v $(pwd)/data:/giveawaybot/data -e TZ=Europe/Moscow giveaway-bot
