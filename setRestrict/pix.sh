#!/bin/bash

# Запускаем программу от имени пользователя 'image' с оставшимися параметрами
sudo -u image /usr/bin/pix "$@"
# sudo -u image /usr/bin/drawing "$@"

