#!/bin/bash
# sudo ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/foliate.sh /usr/local/bin/epub

# Запускаем программу от имени пользователя 'xreader' с оставшимися параметрами
sudo -u foliate XAUTHORITY= /usr/bin/foliate "$@"
