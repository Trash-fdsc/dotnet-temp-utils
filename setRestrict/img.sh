#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/img.sh /usr/local/bin/img

# Запускаем программу от имени пользователя 'image' с оставшимися параметрами
sudo -u image /usr/bin/eog "$@"
# sudo -u image /usr/bin/drawing "$@"

