#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/img.sh /usr/local/bin/imgw

# Запускаем программу от имени пользователя 'image' с оставшимися параметрами
sudo -u image /usr/bin/pix "$@"
# sudo -u image /usr/bin/drawing "$@"

