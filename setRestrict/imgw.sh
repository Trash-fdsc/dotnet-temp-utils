#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/imgw.sh /usr/local/bin/imgw

# Запускаем программу от имени пользователя 'image' с оставшимися параметрами
sudo -u image /usr/bin/drawing "$@"
# sudo -u image /usr/bin/drawing "$@"

